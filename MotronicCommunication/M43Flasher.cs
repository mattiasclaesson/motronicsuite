using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO.Ports;
using System.IO;
using System.Timers;
using System.Threading;

namespace MotronicCommunication
{
    

    public class M43Flasher : IFlasher
    {
        public override event IFlasher.StatusChanged onStatusChanged;


        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        public static extern uint MM_BeginPeriod(uint uMilliseconds);
        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        public static extern uint MM_EndPeriod(uint uMilliseconds);
        private SerialPort _port = new SerialPort();
        private System.Timers.Timer _timer;
        private FlashState _state = FlashState.Idle;
        private string _filename = string.Empty;
        private int _length2Send = 0;
        //private bool _read_data = true;
        private string _logfileName = string.Empty;
//        private bool _logAllData = false;
        private bool _echo = false;
        /// <summary>
        /// COMPORTNUMBER e.g. "COM1" 
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="comportnumber"></param>
        public override void FlashFile(string filename, string comportnumber)
        {
            _filename = filename;
            _logfileName = Path.Combine(Path.GetDirectoryName(_filename), "commlog.txt");
            FileInfo fi = new FileInfo(_filename);
            _length2Send = (int)fi.Length;
            Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
            if (InitializeFlasher(comportnumber))
            {
                CastInfoEvent("Preparing to erase flash", 0);
                Thread.Sleep(2000);
                _state = FlashState.StartErase;
            }
        }

        public override void VerifyChecksum(string filename, string comportnumber)
        {
        }
       
        private void CleanupFlasher()
        {
            try
            {
                MM_EndPeriod(1);
                if (_port.IsOpen) _port.Close();
            }
            catch (Exception E)
            {
                Console.WriteLine("Failed to reset thread high prio: " + E.Message);
            }
            
        }

        private void CastInfoEvent(string information, int percentage)
        {
            if (onStatusChanged != null)
            {
                onStatusChanged(this, new StatusEventArgs(information, percentage));
            }
        }

        private bool InitializeFlasher(string comportnumber)
        {
            
            try
            {
                _eraseFlashDone = 0;
                _retries = 0;
                _communicationTimeout = 0;
                _timer = new System.Timers.Timer(10);
                _timer.Elapsed += new ElapsedEventHandler(_timer_Elapsed);
                _timer.Enabled = true;
                _port.Encoding = Encoding.GetEncoding("ISO-8859-1");
                _port.BaudRate = 9600;
                
               
                _port.PortName = comportnumber;
                _port.ReceivedBytesThreshold = 1;
                _port.DataReceived += new SerialDataReceivedEventHandler(_port_DataReceived);
                _port.Open();
                try
                {
                    _port.Handshake = Handshake.None;
                    //_port.RtsEnable = false; // true
                    //_port.BreakState = false;
                    //_port.DtrEnable = false; // true
                }
                catch (Exception E)
                {
                    Console.WriteLine("Failed to set pins: " + E.Message);
                }
                MM_BeginPeriod(1);
                if (File.Exists(_logfileName)) File.Delete(_logfileName); // create a new logfile
                return true;
            }
            catch (Exception E)
            {
                CastInfoEvent("Failed to initialize flasher: " + E.Message, 0);
            }
            return false;
        }


        private void DumpEchoBuf()
        {
            string dumpbuf = "RX: ";
            foreach (byte b in echoBuf)
            {
                dumpbuf += b.ToString("X2") + " ";
            }
            AddToLogfile(dumpbuf);
        }

        private void AddToLogfile(string dumpbuf)
        {
            if (_logfileName != string.Empty)
            {
                using (StreamWriter sw = new StreamWriter(_logfileName, true))
                {
                    sw.WriteLine(DateTime.Now.Hour.ToString("D2") + ":" + DateTime.Now.Minute.ToString("D2") + ":" + DateTime.Now.Second.ToString("D2") + "." + DateTime.Now.Millisecond.ToString("D3") + " " + dumpbuf);
                }
            }
        }

        private bool _dataReceived = false;
        private int rx_state = 0;
        private RxMsgType rxmsgtype = RxMsgType.Unknown;
        private byte[] echoBuf = new byte[38];
        private int echoCount = 0;
        void _port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (e.EventType == SerialData.Chars)
            {
                string rxdata = _port.ReadExisting();
                for (int t = 0; t < rxdata.Length; t++)
                {
                    _dataReceived = true;
                    //if (_echo) _echo = false; // ignore echo characters
                    //else
                    {

                        byte b = Convert.ToByte(rxdata[t]);
                        if (_state == FlashState.SendFlashData)
                        {
                            // collect upto 38 bytes every time
                            echoBuf[echoCount++] = b;
                            if (echoCount == 38)
                            {
                                echoCount = 0;
                                DumpEchoBuf();
                            }
                        }
                        else AddToLogfile("RX: " + b.ToString("X2"));
                        switch (rx_state)
                        {
                            case 0:
                                // waiting for 0x02
                                if (b == 0x02) rx_state++;
                                break;
                            case 1:
                                if (b == 0x30 || b == 0x31) rx_state++;
                                else rx_state = 0;
                                break;
                            case 2:
                                if (b == 0x59) rx_state++;
                                else rx_state = 0;
                                break;
                            case 3:
                                if (b == 0x30) rx_state++;
                                else rx_state = 0;
                                break;
                            case 4:
                                if (b == 0x39)
                                {
                                    rxmsgtype = RxMsgType.StartErasingFlash;
                                    AddToLogfile("RX: RxMsgType.StartErasingFlash");
                                }
                                else if (b == 0x34)
                                {
                                    rxmsgtype = RxMsgType.FinishedFlashing;
                                    AddToLogfile("RX: RxMsgType.FinishedFlashing");
                                }
                                else if (b == 0x35)
                                {
                                    rxmsgtype = RxMsgType.FinishedErasingFlash;
                                    AddToLogfile("RX: RxMsgType.FinishedErasingFlash");
                                }
                                else if (b == 0x37)
                                {
                                    rxmsgtype = RxMsgType.Acknowledge;
                                    AddToLogfile("RX: RxMsgType.Acknowledge");
                                }
                                else if (b == 0x38)
                                {
                                    rxmsgtype = RxMsgType.NegativeAcknowledge;
                                    AddToLogfile("RX: RxMsgType.NegativeAcknowledge");
                                }
                                else
                                {
                                    AddToLogfile("RX MsgTypeID: " + b.ToString("X2"));
                                }
                                rx_state = 0;
                                break;
                        }
                    }
                }
            }
        }

        private int _communicationTimeout = 0;
        private int _fileOffset = 0;
        private byte[] completeflashbuffer = new byte[0x13000]; // will contain the frame to be send

        private void DumpTransmitBuffer()
        {
            string filetoWrite = Path.Combine(Path.GetDirectoryName(_filename), "txbuf.txt");
            using (StreamWriter sw = new StreamWriter(filetoWrite, false))
            {
                for (int i = 0; i < 0x800; i++)
                {
                    string wrbuf = string.Empty;
                    for (int j = 0; j < 38; j++)
                    {
                        wrbuf += completeflashbuffer[i * 38 + j].ToString("X2") + " ";
                    }
                    sw.WriteLine(wrbuf);
                }
            }
        }

        private int _retries = 0;
        private int _eraseFlashDone = 0;

        void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _timer.Enabled = false;
            try
            {
                switch (_state)
                {
                    case FlashState.Idle:   // waiting for a signal to start the flash sequence
                        _communicationTimeout = 0;
                        break;
                    case FlashState.StartErase:
                        if (_port.IsOpen)
                        {
                            byte[] allBytes = File.ReadAllBytes(_filename);
                            int tx_buf_pnt = 0;
                            _fileOffset = 0;
                            // build the complete transmit buffer
                            
                            while (_fileOffset < _length2Send)
                            {
                                uint checksum = 0;
                                completeflashbuffer[tx_buf_pnt ++] = 0x3A;
                                completeflashbuffer[tx_buf_pnt++] = 0x20; // 32 bytes
                                checksum += completeflashbuffer[tx_buf_pnt-1];
                                byte addresshigh = Convert.ToByte(_fileOffset / 256);
                                byte addresslow = Convert.ToByte((_fileOffset - (256 * addresshigh)));
                                completeflashbuffer[tx_buf_pnt++] = addresshigh;
                                checksum += completeflashbuffer[tx_buf_pnt - 1];
                                completeflashbuffer[tx_buf_pnt++] = addresslow;
                                checksum += completeflashbuffer[tx_buf_pnt - 1];
                                completeflashbuffer[tx_buf_pnt++] = 0x00;
                                checksum += completeflashbuffer[tx_buf_pnt - 1];
                                for (int j = 0; j < 32; j++)
                                {
                                    completeflashbuffer[tx_buf_pnt++] = allBytes[_fileOffset + j];
                                    checksum += completeflashbuffer[tx_buf_pnt - 1];
                                }
                                checksum = 0x100 - checksum;
                                completeflashbuffer[tx_buf_pnt++] = Convert.ToByte(checksum & 0x000000FF);
                                _fileOffset += 32;
                            }
                            DumpTransmitBuffer();
                            _fileOffset = 0;
                            _communicationTimeout = 0;
                            CastInfoEvent("Starting erase", 0);
                            // send the erase command to the ECU
                            WriteBinaryData("3A30303030303030314646");
                            Thread.Sleep(2000);
                            WriteBinaryData("3A011122334455");
                            _state = FlashState.WaitEraseComplete;
                        }
                        break;
                    case FlashState.WaitEraseComplete:
                        // waiting for reception of 
                        // 02 30 59 30 35 03 6F
                        // 02 31 59 30 35 03 6E 
                        // 02 31 59 30 35 03 6E 
                        if (rxmsgtype == RxMsgType.StartErasingFlash)
                        {
                            rxmsgtype = RxMsgType.Unknown;
                            CastInfoEvent("Erasing flash", 0);
                            _communicationTimeout = 0;
                        }
                        else if (rxmsgtype == RxMsgType.FinishedErasingFlash)
                        {
                            rxmsgtype = RxMsgType.Unknown;
                            CastInfoEvent("Finished erasing flash", 0);
                            _eraseFlashDone++;
                            if (_eraseFlashDone == 3)
                            {
                                _state = FlashState.StartFlashing;
                                _communicationTimeout = 0;
                            }
                        }
                        else if (rxmsgtype == RxMsgType.NegativeAcknowledge)
                        {
                            rxmsgtype = RxMsgType.Unknown;
                            CastInfoEvent("Failed to erase flash", 0);
                            _state = FlashState.EraseError;
                            _communicationTimeout = 0;
                        }
                        else if (rxmsgtype == RxMsgType.Unknown)
                        {
                            //TODO: Implement timeout here
                            _communicationTimeout++;
                            if (_communicationTimeout > 1500)
                            {
                                rxmsgtype = RxMsgType.Unknown;
                                CastInfoEvent("Timeout erasing flash", 0);
                                _state = FlashState.EraseError;
                            }
                        }
                        break;
                    case FlashState.StartFlashing:
                        _retries = 0;
                        Thread.Sleep(2500);
                        CastInfoEvent("Start sending flash data", 0);
                        _fileOffset = 0;
                        _state = FlashState.SendFlashData;
                        break;
                    case FlashState.SendFlashData:

                        // the flasher takes about 155 seconds to complete the flash sending
                        // this means 0x10000 bytes in 155 seconds = 423 bytes per second
                        // since we transmit 32 bytes at a time that means we have 423/32 packets to 
                        // send per second which comes down to ~13 packets per second
                        // every packet consists of 38 bytes of data (32 flash bytes) which means
                        // ~ 500 bytes per second transfer. Bitrate = 9k6 which translates roughly to 
                        // 1000 bytes per second, so there should be NO delay between the packets if
                        // we count the echo string into the transmission speed
                        
                        // send a portion of the flash data from _fileOffset, 32 bytes
//                        _read_data = false;
                        for (int t = 0; t < completeflashbuffer.Length; t++)
                        {
                            _port.Write(completeflashbuffer, t, 1);
                            //_echo = true;
                            //Thread.Sleep(1);
                            if( t % 1024 == 0) CastInfoEvent("Flashing...", ((t * 100) / completeflashbuffer.Length));
                            // wait it out here?
                        }
                        for (int i = 0; i < 250; i++)
                        {
                            CastInfoEvent("Waiting...", 100);
                            Thread.Sleep(1);
                        }
                        
//                        _read_data = true;

                        /*while (_fileOffset < _length2Send)
                        {
                            byte[] data2send = readdatafromfile(_filename, _fileOffset, 32);
                            byte[] command2send = new byte[38];
                            command2send[0] = 0x3A;
                            command2send[1] = 0x20;
                            byte addresshigh = Convert.ToByte(_fileOffset / 256);
                            byte addresslow = Convert.ToByte((_fileOffset - (256 * addresshigh)));
                            //AddToLog("Address: " + curaddress.ToString("X4") + " hi: " + addresshigh.ToString("X2") + " lo: " + addresslow.ToString("X2"));
                            command2send[2] = addresshigh;
                            command2send[3] = addresslow;
                            command2send[4] = 0x00;
                            for (int j = 0; j < 32; j++)
                            {
                                command2send[5 + j] = data2send[j];
                            }
                            command2send[37] = CalcChecksum(command2send);
                            _fileOffset += 32;
                            _port.Write(command2send, 0, command2send.Length);
                            //Thread.Sleep( command2send.Length);
                            while (_port.BytesToWrite > 0) Thread.Sleep(0);
                            Thread.Sleep(75); // sleep 75 ms to get to ~13 packets per second
                            CastInfoEvent("Flashing...", ((_fileOffset * 100) / _length2Send));
                        }*/
                        //_state = FlashState.WaitFlashData;

                        // todo: use a stopwatch to hold on for a few moments ?
                        while (_port.BytesToWrite > 0) CastInfoEvent("Flashing...", 100); // wait it out.. maybe we should wait
                        // until no more characters are received from the port?
                        _state = FlashState.SendEndFlash;
                        
                        //_port.Write(txString);
                        // wait until buffer = empty?
                        
                        break;
                    case FlashState.WaitFlashData:
                        if (_fileOffset >= _length2Send)
                        {
                            _state = FlashState.SendEndFlash;
                            //Thread.Sleep(100);
                        }
                        else _state = FlashState.SendFlashData;
                        break;
                    case FlashState.SendEndFlash:
                        Thread.Sleep(2000);
                        CastInfoEvent("Sending flash end sequence...", 100);
//                        _logAllData = true;
                        rxmsgtype = RxMsgType.Unknown;
                        WriteBinaryData("3A0000000000");
                        Thread.Sleep(500);
                        WriteBinaryData("3A30303030303030314646");
                        //WriteBinaryData("3A00000000003A30303030303030314646");
                        _communicationTimeout = 0;
                        _state = FlashState.WaitEndFlash;
                        break;
                    case FlashState.WaitEndFlash:
                        if (rxmsgtype == RxMsgType.FinishedFlashing)
                        {
                            rxmsgtype = RxMsgType.Unknown;
                            CastInfoEvent("Finished writing flash", 0);
                            _state = FlashState.FlashingDone;
                            _communicationTimeout = 0;
                        }
                        else if (rxmsgtype == RxMsgType.NegativeAcknowledge)
                        {
                            rxmsgtype = RxMsgType.Unknown;
                            _communicationTimeout = 0;
                            _retries++;
                            if (_retries < 3)
                            {
                                CastInfoEvent("Retry end flash sequence: " + _retries.ToString(), 0);
                                _state = FlashState.SendEndFlash;
                            }
                            else
                            {
                                CastInfoEvent("Flashing failed", 0);
                                _state = FlashState.FlashingError;
                            }
                        }
                        else if (rxmsgtype == RxMsgType.Unknown)
                        {
                            //TODO: Implement timeout here
                            _communicationTimeout++;
                            if (_communicationTimeout > 500)
                            {
                                rxmsgtype = RxMsgType.Unknown;
                                CastInfoEvent("Timeout writing flash", 0);
                                _state = FlashState.FlashingError;
                            }
                        }
                        else 
                        {
                            AddToLogfile("Ignoring message: " + rxmsgtype.ToString());
                            rxmsgtype = RxMsgType.Unknown;
                        }
                        break;
                    case FlashState.FlashingDone:
                        CastInfoEvent("Flashing completed", 100);
                        CleanupFlasher();
                        _state = FlashState.Idle;
                        break;
                    case FlashState.EraseError:
                        CastInfoEvent("Erase failed", 0);
                        CleanupFlasher();
                        _state = FlashState.Idle;
                        break;
                    case FlashState.FlashingError:
                        CastInfoEvent("Flashing failed", 0);
                        CleanupFlasher();
                        _state = FlashState.Idle;
                        break;
                }
            }
            catch (Exception E)
            {
                CastInfoEvent("Internal error: " + E.Message, 0);
            }
            _timer.Enabled = true;
        }

        private byte CalcChecksum(byte[] seq)
        {
            int checksum = 0;
            for (int t = 1; t < seq.Length; t++)
            {
                checksum += seq[t];
            }
            checksum = 0x100 - checksum;
            return Convert.ToByte(checksum & 0x00000000FF);
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

        private byte[] readdatafromfile(string filename, int address, int length)
        {
            byte[] retval = new byte[length];
            FileStream fsi1 = File.OpenRead(filename);
            while (address > fsi1.Length) address -= (int)fsi1.Length;
            BinaryReader br1 = new BinaryReader(fsi1);
            fsi1.Position = address;
            string temp = string.Empty;
            for (int i = 0; i < length; i++)
            {
                retval.SetValue(br1.ReadByte(), i);
            }
            fsi1.Flush();
            br1.Close();
            fsi1.Close();
            fsi1.Dispose();
            return retval;
        }
    }
}
