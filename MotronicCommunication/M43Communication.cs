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

/* TODO: 
 * We need to read bytes in a seperate thread for more robust communication. Otherwise the 
 * communication will stall when we load the CPU or switch window focus.
 * */

namespace MotronicCommunication
{
    public class M43Communication : ICommunication
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
        private byte[] ACK_Buf = new byte[4];
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
            ACK_Buf[0] = 0x03;
            ACK_Buf[1] = 0x00;
            ACK_Buf[2] = 0x09;
            ACK_Buf[3] = 0x03;	// block to be send as default if no other command si ready
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

            _wakeupRetries = 1;

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
            success = false;
            //0827FC7B5200000103;
            //  4BFC7B52000001
            AddToLog("readSRAM: " + address.ToString("X4") + " len: " + bytestoread.ToString());
            
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
            //Console.WriteLine("readSRAM (1): " + _timeoutMs.ToString());
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
                if (_timeoutMs > timeout) return;
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
                if (_timeoutMs > timeout) return;
            }
            while (CmdLock);				// wait until we can send a command

            Cmd_Buf[0] = 3;
            Cmd_Buf[2] = 5;					// title cmd = dtc read rq
            Cmd_Buf[3] = 3;					// end
            Cmd_Rdy = true;					// send the read DTC command to working thread
            if (WaitForNextFrame(timeout))
            {
                //**************************************************HANG point************************
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
            byte[] filebuffer = new byte[0x10000];
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
            byte[] filebuffer = new byte[0x10000];
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
                        if (_timeoutMs > timeout) return;
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
            int _timeoutMs = 0;
            _IsWaitingForResponse = true;
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
                AddToLog("Needed: " + LastCmd_CTR.ToString("X2") + " received: " + CmdRecv[1].ToString("X2"));
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
                CastInfoEvent("Failed to initialize KWP71: " + E.Message, 0);
            }
            return false;
        }

        private int _timeout = 0;
        private int _wakeupRetries = 1;

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
                            CastInfoEvent("Sending init/wakeup sequence [" + _wakeupRetries.ToString() + "/5]", 0);
                            //_port.BaudRate = 5;
                            // we need to send 0x10 at 5 baud
                            //_port.Write("\x10");
                            _port.BreakState = true;
                            Thread.Sleep(200);
                            _port.BreakState = true;
                            Thread.Sleep(200);
                            _port.BreakState = true;
                            Thread.Sleep(200);
                            _port.BreakState = true;
                            Thread.Sleep(200);
                            _port.BreakState = true;
                            Thread.Sleep(200);
                            _port.BreakState = false;
                            Thread.Sleep(200);
                            _port.BreakState = true;
                            Thread.Sleep(600);
                            _port.BreakState = false;
                            Thread.Sleep(200);
                            //Thread.Sleep(2000);
                            //while (_port.BytesToWrite > 0) Thread.Sleep(0);
                            _state = CommunicationState.WaitForKeywords;
                            
                            _port.BaudRate = 12700;
                            _timeout = 0;
                            break;
                        case CommunicationState.WaitForKeywords:
                            if (_timeout == 0 || _timeout == 100 || _timeout == 200 || _timeout == 300 || _timeout == 400 || _timeout == 500)
                            {
                                int secs = _timeout / 100;
                                CastInfoEvent("Waiting for keywords from ECU (" + secs.ToString() + "/5 seconds)", 0);
                            }
                            if (_timeout++ > 500)
                            {
                                _ecustate = ECUState.NotInitialized;
                                CastInfoEvent("Timeout waiting for keywords", 0);
                                _state = CommunicationState.Start;
                                _timeout = 0;
                                _wakeupRetries++;
                                if (_wakeupRetries == 6)
                                {
                                    _wakeupRetries = 1;
                                    
                                    StopCommunication();
                                    CastInfoEvent("Unable to connect to ECU", 0);
                                    return; // don't restart the timer
                                }
                            }
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

        private void HandleRunningCommunicationByte(byte b)
        {
            
            //AddToLog("RXRUN: " + b.ToString("X2") + " " + ECU_IsMaster.ToString());
            lock (this) // <GS-23052011> test lock
            {
                if (ECU_IsMaster)
                {
                    if (!Echo)
                    {
                        Rx_Buf[Rx_Ctr] = b;		    //data in local frame buffer
                        Rx_Ctr++;
                        if (Rx_Ctr > 0xFF) Rx_Ctr = 0;
                        Bytes_ToRead = Rx_Buf[0];	//number of bytes to expect
                        Bytes_ToRead++;
                        //AddToLog("Bytes to read: " + Bytes_ToRead.ToString("X2"));
                        if (Bytes_ToRead != Rx_Ctr)	// if end of frame , no compliment
                        {
                            SendAck(b, true); //? of false?
                        }
                        else						// we have 240ms to prepare a frame to send
                        {
                            ECU_IsMaster = false;	// let's be a slave
                            Rx_Ctr = 0;
                            Tx_Ctr = 0;				// donc on repart à 0 dans le buff de reception
                            Echo = false;			// we didn't answer to the end of frame byte
                            Block_Ctr = Rx_Buf[1];
                            Block_Ctr++;
                            //if (Block_Ctr > 0xFF) Block_Ctr = 0;
                            ACK_Buf[1] = Block_Ctr; // frame counter
                            IsRecv = false;
                            for (int i = 0; i < 20; i++)
                            {
                                CmdRecv[i] = Rx_Buf[i];		// data for exchange with main thread
                            }

                            string completecommand;
                            CmdRecv[Rx_Buf[0]] = 0;	 // string delimiter or else...
                            completecommand = Encoding.GetEncoding("ISO-8859-1").GetString(CmdRecv);
                            DumpReceiveBuffer("rx cmd: ", CmdRecv);
                            if (CmdRecv[2] != 0xF6)
                            {
                                IsRecv = true;				// inform the other thread there is something to process
                                //AddToLog("Received frame");
                                AddToLog("Rx cmd id: " + CmdRecv[1].ToString("X2"));
                            }
                            else							// this is ascii ID , just to be displayed
                            {							// not very smart to do it here but I am lazy
                                string ID;
                                CmdRecv[Rx_Buf[0]] = 0;	 // string delimiter or else...
                                DumpReceiveBuffer("rx ECU ID: ", CmdRecv);
                                //AddToLog("****");
                                ID = Encoding.ASCII.GetString(CmdRecv);
                                AddToLog("ID0: " + ID);
                                ID = ID.Substring(0, 13);
                                AddToLog("ID1: " + ID);
                                //AddToLog("****");
                                //ID = ReverseString(ID);
                                //AddToLog("ID2: " + ID);
                                //AddToLog("****");
                                ID = ID.Substring(3, 10);
                                //AddToLog("ID3: " + ID);
                                //AddToLog("****");
                                //ID.MakeReverse(); //TODO: <GS-09032011> Make reverse
                                //ID = ID.Left(10); //TODO: <GS-09032011> SubString
                                //AddToLog("ECU ID : " + ID);
                                //AddToLog("****");
                                ID = ID.Replace("\x3F", "");
                                ID = ID.Replace("\x00", " ");
                                AddToLog("Final ID: " + ID);
                                CastECUInfoEvent(IDctr, ID);
                                IDctr++;
                                if ((IDctr == 3) && (!IsConnected)) // connection is really alive after 
                                {								// receiving the 3 ASCII ACU ID
                                    IsConnected = true;
                                }
                            }

                            // *********** here we insert the user command if there is one ******************** 
                            // *********** the process of it is atomic as Cmd_Rdy is just a flag **************

                            if (Cmd_Rdy)					//is there one ?
                            {
                                AddToLog("Sending frame");
                                CmdLock = true;				// lock the buffer during copy
                                Cmd_Rdy = false;
                                Send_ACK = false;			// send user command	
                                Cmd_Len = Cmd_Buf[0];		// len of frame +1 for the end 0x03
                                Cmd_Len++;
                                for (Buf_Ptr = 0; Buf_Ptr < Cmd_Len; Buf_Ptr++)  // copies the user frame into buffer tx
                                {
                                    Tx_Buf[Buf_Ptr] = Cmd_Buf[Buf_Ptr];
                                }
                                Tx_Buf[1] = Block_Ctr;		// frame counter for next block+
                                LastCmd_CTR = Block_Ctr;
                                AddToLog("LastCmdCtr = " + LastCmd_CTR.ToString("X2"));
                                // after the command we just sent
                                // to process answer among ACK block
                                CmdLock = false;			// release buffer for further commands 

                            }
                        }
                    }
                    else
                    {
                        Echo = false;	// echo has been ignored
                    }

                }
                if (!ECU_IsMaster)
                {
                    if (Echo == false)
                    {
                        switch (Send_ACK)
                        {

                            case true:						// no user command so keepalive
                                // with an ACK frame
                                Bytes_ToSend = ACK_Buf[0];
                                Bytes_ToSend++;
                                pData = ACK_Buf[Tx_Ctr];
                                Tx_Ctr++;
                                SendAck(pData, false);
                                //m_Serial.Write(&pData, 1);
                                //Echo = true;

                                if (Bytes_ToSend == Tx_Ctr)	 // end of frame?
                                {
                                    ECU_IsMaster = true;		// ECU goes back master
                                    Tx_Ctr = 0;					//
                                    Rx_Ctr = 0;					// 
                                }
                                break;
                            case false:							// a user command has been issued

                                Bytes_ToSend = Tx_Buf[0];			// number of bytes to send
                                Bytes_ToSend++;
                                pData = Tx_Buf[Tx_Ctr];			// next char
                                Tx_Ctr++;
                                SendAck(pData, false);
                                //m_Serial.Write(&pData, 1);
                                //Echo = TRUE;

                                if (Bytes_ToSend == Tx_Ctr)		// is it block end ?
                                {
                                    ECU_IsMaster = true;		// switch PC to slave
                                    Tx_Ctr = 0;					//
                                    Rx_Ctr = 0;
                                    Send_ACK = true;			// next cmd will be an ACK
                                }
                                break;
                        }

                    }
                    else
                    {
                        Echo = false;		// that was just echo
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
                            //CastInfoEvent("Synchronization in progress", 0);
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
                                _state = CommunicationState.Idle;
                                SendAck(kw2, true);
                                AddToLog("kw1: " + kw1.ToString("X2") + " kw2: " + kw2.ToString("X2"));
                                AddToLog("Entering idle state");
                                _nextByteStartRunningState = true;
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
                        if (_nextByteStartRunningState)
                        {
                            _nextByteStartRunningState = false;
                            _ecustate = ECUState.CommunicationRunning;
                            CastInfoEvent("Communication ready", 0);
                        }
                        else
                        {
                            HandleInitByte(b);
                        }
                    }
                }
            }
        }

    }
}
