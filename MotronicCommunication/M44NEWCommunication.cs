using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO.Ports;
using System.Timers;
using System.Threading;
using System.IO;

//****************
//	00* Read ID				F6
//	01* Read RAM			FE
//	02 Write RAM			ED
//	03* Read eeprom			FD
//	04 Actuator activation	09
//	05* Delete DTC			09
//	06* END diagnosis
//	07* Read DTC			FC
//	08* Read ADC Channel	FB
//	09* Acknowledge
//	0A NO Acknowledge
//	10 Read parameter		EC
//	11 Write parameter		EB
//	12 Snapshot request		F4
//
//	17 Actuator activation with feedback	E8
//	18 Login				F0
//	19 EEprom reading request	EF
//	1A EEprom writing request	F9
//	1E Download request			F7
//	1F Download transfer		09
//	20 Safety code transmission	09

//****************

namespace MotronicCommunication
{
    

    public class M44NEWCommunication : ICommunication
    {
        // this should run on KWP71
        public override event ICommunication.DTCInfo onDTCInfo;
        public override event ICommunication.ECUInfo onECUInfo;
        public override event ICommunication.StatusChanged onStatusChanged;
        

        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        public static extern uint MM_BeginPeriod(uint uMilliseconds);
        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        public static extern uint MM_EndPeriod(uint uMilliseconds);
        private SerialPort _port = new SerialPort();
        private System.Timers.Timer _timer;
        private CommunicationState _state = CommunicationState.Start;

        private ECUState _ecustate = ECUState.NotInitialized;
        private byte[] ACK_Buf = new byte[5];
        private bool IsRecv = false;
	    private bool Cmd_Rdy = false;
	    private bool IsConnected = false;
        private bool CmdLock = false;			// flag to avoid writing a command while sending ; this avoids use of complicated mutexes
        private byte LastCmd_CTR = 0;		    // stores the frame counter for last comand sent

        private bool _IsWaitingForResponse = false;

        public override bool IsWaitingForResponse
        {
            get { return _IsWaitingForResponse; }
            set { _IsWaitingForResponse = value; }
        }

        public override bool WriteEprom(string filename, int timeout)
        {
            return false;
        }
        public override void setCANDevice(string adapterType)
        {

        }
        private bool _communicationRunning = false;

        public override bool CommunicationRunning
        {
            get { return _communicationRunning; }
            set { _communicationRunning = value; }
        }

        public override void StartCommunication(string comportnumber, bool HighSpeed)
        {
            Console.WriteLine("StartCommunication: " + comportnumber);
            ACK_Buf[0] = 0x82;
            ACK_Buf[1] = 0x7A;
            ACK_Buf[2] = 0x13;
            ACK_Buf[3] = 0xA1;
            ACK_Buf[4] = 0xB0;	
            // block to be send as default if no other command si ready
            rx_state = 0;
            _ecustate = ECUState.NotInitialized;
            IsRecv = false;
	        Cmd_Rdy = false;
	        IsConnected = false;
            CmdLock = false;			// flag to avoid writing a command while sending ; this avoids use of complicated mutexes
            LastCmd_CTR = 0;		    // stores the frame counter for last comand sent
            _communicationRunning = false;
            _state = CommunicationState.Start;
            _syncSeen = false;
            kw1seen = false;
            kw2seen = false;
            kw1 = 0;
            kw2 = 0;


            _bytesToIgnore = 0;
            Send_ACK = true;			    // when there is nothing else to send
            Echo = true;				    // true because we answered by KW complement
            ECU_IsMaster = true;			// because ECU is master after wake up
            Rx_Ctr = 0;
            Tx_Ctr = 0;
            Bytes_ToRead = 0;
            Block_Ctr = 0;				    // counter for frames 
            IDctr = 0;					    // counter for initial asscii messages
            Cmd_Len = 0;		            // length of frame
            Buf_Ptr = 0;
            Bytes_ToSend = 0;

            if (InitializeCommunication(comportnumber))
            {
                //Do stuff
                _communicationRunning = true;
            }
        }

        private void KWP81_ChkSum_Calc()
        {
            byte i;
            byte Chk = 0;
            for (i = 0; i < Cmd_Buf[0]; i++)
            {
                Chk += Cmd_Buf[i];
            }
            Chk = (byte)((0xFF - Chk) + 1);
            Cmd_Buf[i] = Chk;
        }

        private bool _enableLogging = false;

        public override bool EnableLogging
        {
            get { return _enableLogging; }
            set { _enableLogging = value; }
        }

        private string _logFolder = string.Empty;

        public override string LogFolder
        {
            get { return _logFolder; }
            set { _logFolder = value; }
        }

        private void AddToLog(string item)
        {
            Console.WriteLine(item);
            if (_enableLogging)
            {
                if (Directory.Exists(LogFolder))
                {
                    lock (this)
                    {
                        using (StreamWriter sw = new StreamWriter(Path.Combine(LogFolder, DateTime.Now.ToString("yyyyMMdd") + "-kwplog.txt"), true))
                        {
                            sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + item);
                        }
                    }
                }
            }
            //Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + item);
        }

        public override byte[] readSRAM(int address, int bytestoread, int timeout, out bool success)
        {
            //0827FC7B5200000103;
            //  4BFC7B52000001
            AddToLog("readSRAM: " + address.ToString("X4") + " len: " + bytestoread.ToString());
            success = false;
            byte[] retval = new byte[bytestoread];
            if (!_communicationRunning) return retval;

            if (bytestoread > 0x0d) return retval;

            IsRecv = false;
            byte addrh = Convert.ToByte(address / 256);
            byte addrl = Convert.ToByte(address - (int)addrh * 256);
            int _timeoutMs = 0;

            do
            {
                _timeoutMs++;
                Thread.Sleep(1);
                if (_timeoutMs > timeout) return retval; // we waited for 
            }
            while (CmdLock);				// wait until we can send a command
            Cmd_Buf[0] = 6;
            Cmd_Buf[2] = 0x01;				                // title cmd = SRAM read
            Cmd_Buf[3] = Convert.ToByte(bytestoread);		// number of byte requested 0x0d max
            Cmd_Buf[4] = Convert.ToByte(addrh);				// start addr H
            Cmd_Buf[5] = Convert.ToByte(addrl);			    // start addr L	
            Cmd_Buf[6] = 3;

            Cmd_Rdy = true;
            //AddToLog("Start waiting for next frame");
            if (WaitForNextFrame(timeout))
            {
                //AddToLog("Finished waiting for next frame");
                //04 23 FE C3 00
                //string dbg = "sram: " + CmdRecv[0].ToString("X2") + " " + CmdRecv[1].ToString("X2") + " " + CmdRecv[2].ToString("X2") + " "+ CmdRecv[3].ToString("X2") + " "+ CmdRecv[4].ToString("X2");
                //AddToLog(dbg);
                for (int i = 0; i < bytestoread; i++)
                {
                    retval[i] = CmdRecv[3 + i];
                }
                success = true;
            }
            return retval;
            /*DumpReceiveBuffer("read sram: ", CmdRecv);
            if (CmdRecv[2] != 0xFD)		// if a NACK occurs
            {
                CastInfoEvent("Frame dropped", 0);
            }*/
            
            
        }

        public override void ReadDTCCodes(int timeout)
        {
            //0827FC7B5200000103;
            //  4BFC7B52000001
            AddToLog("Reading DTCs");
            CastInfoEvent("Reading DTCs", 0);
            IsRecv = false;
            int _timeoutMs = 0;

            do
            {
                _timeoutMs++;
                Thread.Sleep(1);
                if (_timeoutMs > timeout) return; // we waited for 
            }
            while (CmdLock);				// wait until we can send a command
            Cmd_Buf[0] = 3;
            Cmd_Buf[2] = 7;					// title cmd = dtc clear rq
            Cmd_Buf[3] = 3;					// end
            Cmd_Rdy = true;					// send the read DTC command to working thread
            if (WaitForNextFrame(timeout))
            {

                int dtcCode = (int)Rx_Buf[3];
                if (dtcCode != 0xFE)
                {
                    int dtcState = (int)Rx_Buf[4];
                    int dtcCondition1 = (int)Rx_Buf[5];
                    int dtcCondition2 = (int)Rx_Buf[6];
                    int dtcCount = (int)Rx_Buf[7];
                    CastDTCInfo(dtcCode, dtcState, dtcCondition1, dtcCondition2, dtcCount);
                    //DumpReceiveBuffer("DTCCodes: ", Rx_Buf);
                }
                else
                {
                    CastDTCInfo(0, 0, 0, 0, 0); // no codes
                }
            }
            CastInfoEvent("DTCs read", 100);
        }


        public override void ClearDTC(int timeout)
        {
            AddToLog("Clearing DTCs");
            CastInfoEvent("Clearing DTCs", 0);
            IsRecv = false;
            int _timeoutMs = 0;

            do
            {
                _timeoutMs++;
                Thread.Sleep(1);
                if (_timeoutMs > timeout) return; // we waited for 
            }
            while (CmdLock);				// wait until we can send a command
            Cmd_Buf[0] = 3;
            Cmd_Buf[2] = 5;					// title cmd = dtc read rq
            Cmd_Buf[3] = 3;					// end
            Cmd_Rdy = true;					// send the read DTC command to working thread
            if (WaitForNextFrame(timeout))
            {//**************************************************HANG point************************
                //AddToLog("DTCs cleared");
                CastInfoEvent("DTCs cleared", 100);
            }
            else
            {
                CastInfoEvent("DTCs NOT cleared", 100);
            }
        }

        private int _readEpromState = 0;

        public void ReadEpromStateMachine(string filename)
        {
            CastInfoEvent("Start reading flash content", 0);
            byte[] filebuffer = new byte[0x20000];
            byte[] DataBuffer = new byte[0x100];
            int Last_EP_addr_L = 0x100;			// last accessible byte in eeprom (from login success frame )
            int bufptr = 0;
            int fileptr = 0;
            int i = 0;
            int bytestoread = 0;
            int h = 0;
            bool fileComplete = false;
            while(!fileComplete)
            {
                CastInfoEvent("", 0);
                switch (_readEpromState)
                {
                    case 0:
                        // next thingie
                        AddToLog("State = 0 " + bufptr.ToString() + " " + h.ToString());

                        IsRecv = false;
                        do
                        {
                            CastInfoEvent("", 0); // <GS-09032011> laatste toegevoegd
                            //AddToLog("Waiting...");
                        }
                        while (CmdLock);				// wait until we can send a command
                        Cmd_Buf[0] = 6;
                        Cmd_Buf[2] = 0x03;				// title cmd = EPROM read
                        Cmd_Buf[3] = Convert.ToByte(bytestoread);		// number of byte requested 0x0d max
                        Cmd_Buf[4] = Convert.ToByte(h);					// start addr H
                        Cmd_Buf[5] = Convert.ToByte(bufptr);			// start addr L	
                        Cmd_Buf[6] = 3;
                        Cmd_Rdy = true;
                        _readEpromState++;
                        break;
                    case 1:
                        // waiting for response
                        //AddToLog("State = 1 " + bufptr.ToString() + " " + h.ToString());
                        if (!_communicationRunning) _readEpromState = 99; // write to file
                        else
                        {
                            WaitForNextFrame(1000);
                            /*if (IsRecv)
                            {
                                AddToLog("State (IsRecv = true) = 1 " + bufptr.ToString() + " " + h.ToString());
                                IsRecv = false;
                                AddToLog(CmdRecv[1].ToString("X2") + " vs " + LastCmd_CTR.ToString("X2"));
                                if(CmdRecv[1] == LastCmd_CTR++) _readEpromState ++;
                                else _readEpromState = 0; // retry
                            }*/
                        }
                        break;
                    case 2:
                        // handle the data
                        AddToLog("State = 2 " + bufptr.ToString() + " " + h.ToString());
                        for (i = 3; i < (bytestoread + 3); i++)	 // strores the data in the buffer
                        {
                            DataBuffer[bufptr] = CmdRecv[i];
                            filebuffer[fileptr] = CmdRecv[i];
                            bufptr++;
                            fileptr++;
                        }
                        // now, increase the address and go again
                        if (bufptr > Last_EP_addr_L)
                        {
                            h++;
                            bufptr = 0;
                            if (h == 0x100) _readEpromState = 99; // write to file
                            else
                            {
                                _readEpromState = 0;
                            }
                        }
                        break;
                    case 99:
                        WriteFile(filename, filebuffer);
                        fileComplete = true;
                        break;
                }
            }

        }

        public override void ReadEprom(string filename, int timeout)
        {
            CastInfoEvent("Start reading flash content", 0);
            byte[] filebuffer = new byte[0x20000];
            byte[] DataBuffer = new byte[0x100];
            int Last_EP_addr_L;			// last accessible byte in eeprom (from login success frame )
            int bufptr = 0;
            int fileptr = 0;
            int i = 0;
            //int ptr = 0;
            int bytestoread = 0;
            int h = 0;
            //int EP_end = 0xFFFF;
            //HANDLE hFile;
            //CString l_strFileName;
            //CString csTemp;

            //TODO: <GS-09032011> signal to user that this will take approximately forever to complete!
            for (h = 0; h <= 0xFF; h++)
            {
                Last_EP_addr_L = 0x100;	// we read by chunks of 256 bytes
                bufptr = 0;
                do
                {
                    //AddToLog("pntr: " + bufptr.ToString());
                    if (Last_EP_addr_L - bufptr > 0x0d)
                    { bytestoread = 0x0d; }
                    else
                    { bytestoread = Last_EP_addr_L - bufptr; }

                    IsRecv = false;

                    int _timeoutMs = 0;

                    do
                    {
                        _timeoutMs++;
                        Thread.Sleep(1);
                        CastInfoEvent("", 0);
                        if (_timeoutMs > timeout) return; // we waited for 
                    }
                    while (CmdLock);				// wait until we can send a command

                    Cmd_Buf[0] = 6;
                    Cmd_Buf[2] = 0x03;				// title cmd = EPROM read
                    Cmd_Buf[3] = Convert.ToByte(bytestoread);		// number of byte requested 0x0d max
                    Cmd_Buf[4] = Convert.ToByte(h);					// start addr H
                    Cmd_Buf[5] = Convert.ToByte(bufptr);			// start addr L	
                    Cmd_Buf[6] = 3;

                    // sending 
                    AddToLog("Sending flash read command: " + Cmd_Buf[3].ToString("X2") + " " + Cmd_Buf[4].ToString("X2") + " " + Cmd_Buf[5].ToString("X2"));

                    Cmd_Rdy = true;

                    WaitForNextFrame(timeout);         // <GS-22032011> this is our problem!
                    if (CmdRecv[2] != 0xFD)		// if a NACK occurs
                    {
                        CastInfoEvent("Frame dropped", 0);
                        AddToLog("Frame dropped: " + CmdRecv[2].ToString("X2"));
                        break;
                    }
                    

                    for (i = 3; i < (bytestoread + 3); i++)	 // strores the data in the buffer
                    {
                        DataBuffer[bufptr] = CmdRecv[i];
                        filebuffer[fileptr] = CmdRecv[i];
                        bufptr++;
                        fileptr++;
                        //Thread.Sleep(0);						// allows update of th UI
                    }
                    //Thread.Sleep(1);
                    CastInfoEvent("", 0);
                    if (bufptr > Last_EP_addr_L)
                    { break; }
                }
                while (bufptr < Last_EP_addr_L);
                int percentage = h * 100 / 255;
                CastInfoEvent("Reading flash... (" + percentage.ToString() + "%)", percentage);
            }
            WriteFile(filename, filebuffer);

            CastInfoEvent("Done reading flash", 100);
        }

        private void WriteFile(string filename, byte[] data)
        {
            FileStream fs = new FileStream(filename, FileMode.OpenOrCreate);
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                bw.Write(data);      
            }
            fs.Close();
            fs.Dispose();
        }


        private void DumpReceiveBuffer(string InfoString, byte[] Rx_Buf)
        {
            string result = InfoString;
            int msgLen = Rx_Buf[0];
            for (int i = 1; i < msgLen; i++)
            {
                result += Rx_Buf[i].ToString("X2");
            }
            //CastInfoEvent(result, 0);
            AddToLog(result);
        }

        /// <summary>
        /// DIRTY SOLUTION!
        /// </summary>
        private bool WaitForNextFrame(int timeout)
        {
            _IsWaitingForResponse = true;
            int _timeoutMs = 0;
            do
            {
                do
                {
                    Thread.Sleep(1);
                    CastInfoEvent("", 0);
                    if (!_communicationRunning)
                    {
                        IsWaitingForResponse = false;
                        return false;
                    }
                    if (_timeoutMs++ > timeout)
                    {
                        _IsWaitingForResponse = false;
                        return false;
                    }
                } while (!IsRecv);				// wait until the ECU answers in Rx_Buf 
                IsRecv = false;
                CastInfoEvent("", 0);
                //AddToLog("Needed: " + LastCmd_CTR.ToString("X2") + " received: " + CmdRecv[1].ToString("X2"));
            } while (CmdRecv[1] != LastCmd_CTR+1); // this was ++ in stead of +1... 
            _IsWaitingForResponse = false;
            return true;
        }

        private void WriteBinaryData(string hexdata)
        {
            try
            {
                byte[] data2Send = new byte[hexdata.Length / 2];
                int idx = 0;
                for (int t = 0; t < hexdata.Length; t += 2)
                {
                    data2Send[idx++] = Convert.ToByte(Convert.ToInt32(hexdata.Substring(t, 2), 16));
                }
                _port.Write(data2Send, 0, data2Send.Length);
                // sleep to wait until the data is transmitted.
                // 9600 baud / 10 bits = 960 characters per second = 10 ms per byte
                //Thread.Sleep(10 * data2Send.Length);
                while (_port.BytesToWrite > 0) Thread.Sleep(0);
            }
            catch (Exception E)
            {
                CastInfoEvent("Failed to send hexdata " + hexdata + " : " + E.Message, 0);
            }
        }

        public override void StopCommunication()
        {
            Cleanup();
            _communicationRunning = false;
        }

        private void Cleanup()
        {
            try
            {
                MM_EndPeriod(1);
                if (_port.IsOpen) _port.Close();
            }
            catch (Exception E)
            {
                AddToLog("Failed to reset thread high prio: " + E.Message);
            }

        }

        private void CastECUInfoEvent(int idnumber, string info)
        {
            if(onECUInfo != null)
            {
                onECUInfo(this, new ECUInfoEventArgs(info, idnumber));
            }
        }

        private void CastDTCInfo(int dtcCode, int dtcState, int dtcCondition1, int dtcCondition2, int dtcCounter)
        {
            if (onDTCInfo != null)
            {
                onDTCInfo(this, new DTCEventArgs(dtcCode, dtcState, dtcCondition1, dtcCondition2, dtcCounter));
            }
        }

        private void CastInfoEvent(string information, int percentage)
        {
            if (onStatusChanged != null)
            {
                onStatusChanged(this, new StatusEventArgs(information, percentage, _ecustate));
            }
        }

        private bool _initIsDone = false;

        private bool InitializeCommunication(string comportnumber)
        {
            Console.WriteLine("InitializeCommunication: " + comportnumber);
            try
            {
                if (!_initIsDone)
                {
                    
                    _timer = new System.Timers.Timer(10);
                    _timer.Elapsed += new ElapsedEventHandler(_timer_Elapsed);
                    if (_port.IsOpen) _port.Close();
                    _port.Encoding = Encoding.GetEncoding("ISO-8859-1");
                    _port.BaudRate = 5;
                    _port.PortName = comportnumber;
                    _port.ReceivedBytesThreshold = 1;
                    _port.DataReceived += new SerialDataReceivedEventHandler(_port_DataReceived);
                    _port.Open();
                    try
                    {
                        _port.Handshake = Handshake.None;
                        _port.RtsEnable = true;
                        _port.BreakState = false;
                        _port.DtrEnable = true;
                    }
                    catch (Exception E)
                    {
                        AddToLog("Failed to set pins: " + E.Message);
                    }
                    MM_BeginPeriod(1);
                    Thread.CurrentThread.Priority = ThreadPriority.AboveNormal; // high prio thread
                    _initIsDone = true;
                }
                else
                {
                    MM_BeginPeriod(1);
                    if (_port.IsOpen) _port.Close();
                    _port.BaudRate = 5;
                    _port.PortName = comportnumber;
                    _port.Open();
                }
                _timer.Enabled = true;
                return true;
            }
            catch (Exception E)
            {
                _ecustate = ECUState.NotInitialized;
                CastInfoEvent("Failed to initialize KWP81: " + E.Message, 0);
            }
            return false;
        }

        private int _timeout = 0;

        void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _timer.Enabled = false;
            if (_port.IsOpen)
            {
                try
                {
                    switch (_state)
                    {
                        case CommunicationState.Start:
                            _ecustate = ECUState.NotInitialized;
                            CastInfoEvent("Sending init/wakeup sequence", 0);
                            //_port.BaudRate = 5;
                            // we need to send 0x10 at 5 baud
                            // for kwp81 this is 0x7A

                            _port.BreakState = true;    // 0    startbit
                            Thread.Sleep(200);
                            _port.BreakState = true;    // 0    bit 7
                            Thread.Sleep(200);
                            _port.BreakState = false;    // 1    bit 6
                            Thread.Sleep(200);
                            _port.BreakState = true;    // 0    bit 5
                            Thread.Sleep(200);
                            _port.BreakState = false;    // 1    bit 4
                            Thread.Sleep(200);
                            _port.BreakState = false;   // 1    bit 3
                            Thread.Sleep(200);
                            _port.BreakState = false;    // 1    bit 2
                            Thread.Sleep(200);
                            _port.BreakState = false;    // 1    bit 1
                            Thread.Sleep(200);
                            _port.BreakState = true;    // 0    bit 0
                            Thread.Sleep(200);
                            _port.BreakState = false;   // 1    stop bit or average bit?
                            Thread.Sleep(200);

                            /*_port.BreakState = true;    // 0
                            Thread.Sleep(200);
                            _port.BreakState = true;    // 0
                            Thread.Sleep(200);
                            _port.BreakState = true;    // 0
                            Thread.Sleep(200);
                            _port.BreakState = true;    // 0
                            Thread.Sleep(200);
                            _port.BreakState = true;    // 0
                            Thread.Sleep(200);
                            _port.BreakState = false;   // 1
                            Thread.Sleep(200);
                            _port.BreakState = true;    // 0 0 0 
                            Thread.Sleep(600);
                            _port.BreakState = false;   // 1
                            Thread.Sleep(200);*/
                            //Thread.Sleep(2000);
                            //while (_port.BytesToWrite > 0) Thread.Sleep(0);
                            _state = CommunicationState.WaitForKeywords;
                            
                            _port.BaudRate = 10400; // new protocol = 10k4 baud
                            _timeout = 0;
                            break;
                        case CommunicationState.WaitForKeywords:
                            if (_timeout++ > 500)
                            {
                                _ecustate = ECUState.NotInitialized;
                                CastInfoEvent("Timeout waiting for keywords", 0);
                                _state = CommunicationState.Start;
                                _timeout = 0;
                            }
                            //Console.WriteLine("waiting for kw: " + _timeout.ToString());
                            // timeout?
                            break;
                        case CommunicationState.Idle:
                            //CastInfoEvent("In idle state waiting for messages", 0);
                            // doin' nuthing
                            break;
                        case CommunicationState.SendCommand:
                            break;
                    }
                }
                catch (Exception E)
                {
                    AddToLog(E.Message);
                }
            }
            _timer.Enabled = true;
        }

        private int rx_state = 0;

        private bool _syncSeen = false;
        private bool kw1seen = false;
        private bool kw2seen = false;
        private bool kw2complseen = false;
        private byte kw1 = 0;
        private byte kw2 = 0;

        private void SendAck(byte b2ack, bool invert)
        {
            byte[] b2send = new byte[1];
            b2send[0] = b2ack;
            if(invert) b2send[0] ^= 0xFF;
            if (_port.IsOpen)
            {
                _port.Write(b2send, 0, 1);
                //AddToLog("TX: " + b2send[0].ToString("X2") + " " + invert.ToString());
                Echo = true; // ignore the echo byte that will be coming
            }
        }

        private int _bytesToIgnore = 0;
        private bool Send_ACK = true;			    // when there is nothing else to send
        private bool Echo = true;				    // true because we answered by KW complement
        private bool ECU_IsMaster = true;			// because ECU is master after wake up
        private byte[] Cmd_Buf = new byte[150];		// command entered by user
        private byte[] Rx_Buf = new byte[150];	    // receive buffer
        private byte[] CmdRecv = new byte[150];	    // exchange between threads

        private int Rx_Ctr = 0;
        private int Tx_Ctr = 0;
        private int Bytes_ToRead = 0;
        private byte pData;
        private byte Block_Ctr = 0;				    // counter for frames 
        private int IDctr = 0;					    // counter for initial asscii messages
        private int Cmd_Len = 0;		            // length of frame
        private int Buf_Ptr = 0;
        private byte[] Tx_Buf = new byte[50];       // transmit buffer
        private int Bytes_ToSend = 0;
        private byte ChkSum = 0;

        private void Hex_to_ASCII_response(byte[] Array)
        {
            string Cmd_Msg = string.Empty;
            byte Msg_Len = 0;
            byte ctr = 0;
            byte ch, l, h = 0;
            Msg_Len = (byte)(Array[0] - 0x80 + 3);
            // Msg_Len ++;

            for (ctr = 0; ctr < Msg_Len; ctr++)
            {
                ch = Array[ctr];
                l = (byte)((ch & 0x0F) + 0x30);
                if (l > 0x39)
                {
                    l = (byte)(l + 7);
                }

                h = (byte)(((ch & 0xF0) / 16) + 0x30);
                if (h > 0x39)
                {
                    h = (byte)(h + 7);
                }

                Cmd_Msg += h.ToString("X2");
                Cmd_Msg += l.ToString("X2");
                Cmd_Msg += " ";
            }
            
            Console.WriteLine(Cmd_Msg);
        }

        private void HandleRunningCommunicationByte(byte b)
        {
            //AddToLog("RXRUN: " + b.ToString("X2") + " " + ECU_IsMaster.ToString());

            lock (this)
            {
                if (Exp_Ans)
                {
                    ChkSum = 0;				// frame checksum
                    Rx_Ctr = 0;					// ready for another block
                    do
                    {
                        // deal with the incoming byte ( store it )
                        Rx_Buf[Rx_Ctr] = b;
                        ChkSum += b;
                        Rx_Ctr++;
                    }
                    while (Rx_Ctr != Rx_Buf[0] - 0x80 + 3); // all chars are received?


                    Exp_Ans = false;
                    Console.WriteLine("Whole frame received");
                    Hex_to_ASCII_response(Rx_Buf);

                    IsRecv = true;			// Let other sub routine know that something has been received.
                    // TODO: <GS-25052011> what to do with this, check volvodiag code
                }
                else
                {
                    Thread.Sleep(200);
                    if (Cmd_Rdy)
                    {
                        Send_ACK = false;
                    }

                    switch (Send_ACK)
                    {

                        case true:						// no user command so keepalive

                            Len = (byte)(ACK_Buf[0] - 0x80 + 3);	// size of cmd + chksum
                            for (i = 0; i < Len; i++)
                            {
                                pData = ACK_Buf[i];
                                SendAck(pData, false);
                            }

                            for (i = 0; i < Len; i++)		// expect the same amount of echoes
                            {
                                /*cData = */_port.ReadByte();
                                //m_Serial.Read(&cData, 1);	// read the echoes
                            }
                            Exp_Ans = true;					//Echo received. Expecting answer to command
                            break;
                        case false:							// a user command has been issued

                            Len = (byte)(Cmd_Buf[0] - 0x80 + 3);	// size of cmd + chksum
                            for (i = 0; i < Len; i++)
                            {
                                pData = Cmd_Buf[i];
                                SendAck(pData, false);
                            }

                            for (i = 0; i < Len; i++)		// expect the same amount of echoes
                            {
                                //m_Serial.Read(&cData, 1);	// read the echoes
                                _port.ReadByte();
                            }

                            Cmd_Rdy = false;
                            Exp_Ans = true;					//Echo received. Expecting answer to command
                            Send_ACK = true;			// next cmd will be an ACK
                            break;
                    }
                }
            }
        }

        public static string ReverseString(string s)
        {
            char[] arr = s.ToCharArray();
            Array.Reverse(arr);
            return new string(arr);
        }

        private byte Len = 0;
        private byte i = 0;
        private bool Exp_Ans = false;

        private void HandleInitByte(byte b)
        {
            

            /*if (_ecustate == ECUState.CommunicationRunning)
            {
                SendAck(b);
            }*/
            switch (rx_state)
            {
                case 0:
                    if (_state == CommunicationState.Start || _state == CommunicationState.WaitForKeywords)
                    {
                        if (!_syncSeen)
                        {
                            if (b == 0x55) _syncSeen = true;
                            _ecustate = ECUState.Initialized;
                            CastInfoEvent("Synchronization in progress", 0);
                        }
                        else
                        {
                            if (!kw1seen)
                            {
                                kw1seen = true;
                                kw1 = b;
                            }
                            else if (!kw2seen)
                            {
                                kw2seen = true;
                                kw2 = b;
                                SendAck(kw2, true);
                                AddToLog("kw1: " + kw1.ToString("X2") + " kw2: " + kw2.ToString("X2"));
                            }
                            else if (!kw2complseen)
                            {
                                kw2complseen = true;
                                _state = CommunicationState.Idle;
                                AddToLog("Entering idle state");
                                _nextByteStartRunningState = true;
                                pData = (byte)(0xFF - b);
                                Console.WriteLine("ECUID: " + b.ToString("X2"));
                                Cmd_Buf[0] = 0x83;
                                Cmd_Buf[1] = 0x7A;
                                Cmd_Buf[2] = 0x13;
                                Cmd_Buf[3] = 0xB9;
                                Cmd_Buf[4] = 0xF0;
                                Cmd_Buf[5] = 0xB9;
                                Cmd_Rdy = true;		    // get ID immediately after starting communication
                                Thread.Sleep(250);
                                Len = (byte)(Cmd_Buf[0] - 0x80 + 3);	// size of cmd + chksum , zero based index
                                
                                for (i = 0; i < Len; i++)
                                {
                                    pData = Cmd_Buf[i];
                                    SendAck(pData, false);
                                }
                                Echo = true;
                                Cmd_Rdy = false;

                                for (i = 0; i < Len; i++)
                                {
                                    SendAck(b, false);
                                }
                                Cmd_Rdy = false;
                                Exp_Ans = true;					//Echo received. Expecting answer to command

                                Send_ACK = true;			// next cmd will be an ACK
                                IsConnected = true;
                            }
                        }
                    }
                    break;
            }
        }

        private bool _nextByteStartRunningState = false;

        void _port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (e.EventType == SerialData.Chars)
            {
                string rxdata = _port.ReadExisting();
                for (int t = 0; t < rxdata.Length; t++)
                {
                    byte b = Convert.ToByte(rxdata[t]);
                    if (_ecustate == ECUState.CommunicationRunning)
                    {
                        //AddToLog("RX: " + b.ToString("X2"));
                        HandleRunningCommunicationByte(b);
                    }
                    else
                    {
                        AddToLog("RXINIT: " + b.ToString("X2"));
                        HandleInitByte(b);
                        if (_nextByteStartRunningState)
                        {
                            _nextByteStartRunningState = false;
                            _ecustate = ECUState.CommunicationRunning;
                            CastInfoEvent("Communication ready", 0);
                        }
                    }
                }
            }
        }

    }
}
