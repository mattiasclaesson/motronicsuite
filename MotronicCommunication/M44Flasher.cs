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
    public class M44Flasher : IFlasher
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
            _filename = filename;
            _logfileName = Path.Combine(Path.GetDirectoryName(_filename), "commlog.txt");
            AddToLogfile("VerifyChecksum started");
            FileInfo fi = new FileInfo(_filename);
            _length2Send = (int)fi.Length;
            Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
            if (InitializeFlasher(comportnumber))
            {
                CastInfoEvent("Preparing to checksum verification", 0);
                Thread.Sleep(500);
                _state = FlashState.VerifyChecksum;
            }
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
                _switchBankDone = 0;
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
                lock (this)
                {
                    using (StreamWriter sw = new StreamWriter(_logfileName, true))
                    {
                        sw.WriteLine(DateTime.Now.Hour.ToString("D2") + ":" + DateTime.Now.Minute.ToString("D2") + ":" + DateTime.Now.Second.ToString("D2") + "." + DateTime.Now.Millisecond.ToString("D3") + " " + dumpbuf);
                    }
                }
            }
            //Console.WriteLine(dumpbuf); // test
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
                        if (_state == FlashState.SendFlashData || _state == FlashState.SendFlashDataUpper || _state == FlashState.WaitForFinishFirstBank)
                        {
                            // collect upto 38 bytes every time
                            echoBuf[echoCount++] = b;
                            if (echoCount == 38)
                            {
                                echoCount = 0;
                                DumpEchoBuf();
                            }
                        }
                        else AddToLogfile("RX: " + b.ToString("X2") + " " + rx_state.ToString());
                        switch (rx_state)
                        {
                                //02 30 59 30 34 03 6E
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
                                // possible return values:
                                // 0x38     // negative acknowledge
                                // 0x31     // No magic word was received for a command that DOES require it
                                // 0x32     // some result on erase cycle, either fail or bank erased ID
                                // 0x33     // timing error, a 0x3A frame was received while programming bytes in flash
                                // 0x34     // flashing completed, not all bytes received
                                // 0x35     // finished erase sequence
                                // 0x36     // flashing completed
                                // 0x37     // positive acknowledge - Out of range error
                                // 0x39     // start erase sequence
                                // 0x41     // start bank switch
                                // 0x42     // finished bank switch
                                // 0x43     // some sort of verify command response (start)
                                // 0x44     // some sort of verify command response (end good/fail)
                                // 0x45     // some sort of verify command response (end good/fail)
                                // 0x46     // programming voltage range check error

                                if (b == 0x39)
                                {
                                    rxmsgtype = RxMsgType.StartErasingFlash;
                                    AddToLogfile("RX: RxMsgType.StartErasingFlash - 0x39");
                                }
                                else if (b == 0x31)
                                {
                                    rxmsgtype = RxMsgType.NoMagicWordReceived;
                                    AddToLogfile("RX: RxMsgType.NoMagicWordReceived - 0x31");
                                }
                                else if (b == 0x34)
                                {
                                    rxmsgtype = RxMsgType.FinishedFlashing;
                                    AddToLogfile("RX: RxMsgType.FinishedFlashing - 0x34");
                                }
                                else if (b == 0x35)
                                {
                                    rxmsgtype = RxMsgType.FinishedErasingFlash;
                                    AddToLogfile("RX: RxMsgType.FinishedErasingFlash");
                                }
                                else if (b == 0x36)
                                {
                                    rxmsgtype = RxMsgType.FinishedFlashing;
                                    AddToLogfile("RX: RxMsgType.FinishedFlashing - 0x36");
                                }
                                else if (b == 0x37)
                                {
                                    //rxmsgtype = RxMsgType.Acknowledge;
                                    rxmsgtype = RxMsgType.OutOfRangeError;
                                    AddToLogfile("RX: RxMsgType.OutOfRangeError");
                                }
                                else if (b == 0x38)
                                {
                                    rxmsgtype = RxMsgType.NegativeAcknowledge;
                                    AddToLogfile("RX: RxMsgType.NegativeAcknowledge");
                                }
                                else if (b == 0x41)
                                {
                                    rxmsgtype = RxMsgType.StartSwitchingBank;
                                    AddToLogfile("RX: RxMsgType.StartSwitchingBank");
                                }
                                else if (b == 0x42)
                                {
                                    rxmsgtype = RxMsgType.FinishedSwitchingBank;
                                    AddToLogfile("RX: RxMsgType.FinishedSwitchingBank");
                                }
                                else if (b == 0x46)
                                {
                                    rxmsgtype = RxMsgType.ProgrammingVoltageOutOfRange;
                                    AddToLogfile("RX: RxMsgType.ProgrammingVoltageOutOfRange");
                                }
                                else if (b == 0x43)
                                {
                                    rxmsgtype = RxMsgType.StartChecksumVerification;
                                    AddToLogfile("RX: RxMsgType.StartChecksumVerification");
                                }
                                else if (b == 0x44)
                                {
                                    rxmsgtype = RxMsgType.FinishChecksumVerificationFailed;
                                    AddToLogfile("RX: RxMsgType.FinishChecksumVerificationFailed");
                                }
                                else if (b == 0x45)
                                {
                                    rxmsgtype = RxMsgType.FinishChecksumVerificationOK;
                                    AddToLogfile("RX: RxMsgType.FinishChecksumVerificationOK");
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
        private byte[] completeflashbuffer_lower = new byte[0x13000]; // will contain the frames to be send.. need 32 bytes less <GS-19042011>
        private byte[] completeflashbuffer_upper = new byte[0x13000 - 0x130]; // will contain the frames to be send

        private byte[] completeflashbuffer = new byte[0x26000 + 38]; // will contain the frames to be send.. 


        private void DumpTransmitBuffer()
        {
            string filetoWrite = Path.Combine(Path.GetDirectoryName(_filename), "txbuf.txt");
            using (StreamWriter sw = new StreamWriter(filetoWrite, false))
            {
                for (int i = 0; i < 0x0800; i++)
                {
                    string wrbuf = string.Empty;
                    for (int j = 0; j < 38; j++)
                    {
                        wrbuf += completeflashbuffer_lower[i * 38 + j].ToString("X2") + " ";
                    }
                    sw.WriteLine(wrbuf);
                }
                for (int i = 0; i < (completeflashbuffer_upper.Length/38); i++)
                {
                    string wrbuf = string.Empty;
                    for (int j = 0; j < 38; j++)
                    {
                        wrbuf += completeflashbuffer_upper[i * 38 + j].ToString("X2") + " ";
                    }
                    sw.WriteLine(wrbuf);
                }
            }
        }


        private void DumpFullTransmitBuffer()
        {
            string filetoWrite = Path.Combine(Path.GetDirectoryName(_filename), "txbuf.txt");
            using (StreamWriter sw = new StreamWriter(filetoWrite, false))
            {
                for (int i = 0; i < (completeflashbuffer.Length / 38); i++)
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
        private int _switchBankDone = 0;
        private int _waitFirstBankFlashed = 0;
        byte[] allBytes;
        UInt16 fileChecksum = 0;
        string strChecksumToSend = string.Empty;
        byte chksum = 0;
        int _checksumType = 0;

        void _timer_ElapsedOLD(object sender, ElapsedEventArgs e)
        {
            _timer.Enabled = false;
            try
            {
                switch (_state)
                {
                    case FlashState.Idle:   // waiting for a signal to start the flash sequence
                        _communicationTimeout = 0;
                        break;
                        //TODO: This should be started later when flashing is done! <GS-18042011>
                    case FlashState.VerifyChecksum:
                        CastInfoEvent("Starting checksum verification:" + _checksumType.ToString(), 0);
                        _communicationTimeout = 0;
                        allBytes = File.ReadAllBytes(_filename);
                        fileChecksum = 0;

                        switch (_checksumType)
                        {
                            case 0: // normal calculation
                                for (int ci = 0; ci < 0x1FF00; ci++)
                                {
                                    fileChecksum += allBytes[ci];
                                }
                                fileChecksum &= 0xFFFF;

                                strChecksumToSend = "3AFE55443322110000000000000300000000" + fileChecksum.ToString("X4") + "0000000000000000000000000000000000";
                                break;
                            case 1: // assume first bank programmed, and second bank empty (FFs)
                                for (int ci = 0; ci < 0x10000; ci++)
                                {
                                    fileChecksum += allBytes[ci];
                                }
                                for (int ci = 0; ci < 0xFF00; ci++)
                                {
                                    fileChecksum += 0xFF;
                                }
                                fileChecksum &= 0xFFFF;
                                strChecksumToSend = "3AFE55443322110000000000000300000000" + fileChecksum.ToString("X4") + "0000000000000000000000000000000000";
                                break;
                            case 2: // assume entire flash chip empty (FFs)
                                for (int ci = 0; ci < 0x1FF00; ci++)
                                {
                                    fileChecksum += 0xFF;
                                }
                                fileChecksum &= 0xFFFF;
                                strChecksumToSend = "3AFE55443322110000000000000300000000" + fileChecksum.ToString("X4") + "0000000000000000000000000000000000";
                                break;
                            case 3: // assume entire flash chip 00s
                                fileChecksum &= 0xFFFF;
                                strChecksumToSend = "3AFE55443322110000000000000300000000" + fileChecksum.ToString("X4") + "0000000000000000000000000000000000";
                                break;
                            /*case 4: // assume first bank overwritten with second bank (possible?)
                                for (int ci = 0; ci < 0x1FF00; ci++)
                                {
                                    fileChecksum += 0xFF;
                                }
                                fileChecksum &= 0xFFFF;
                                strChecksumToSend = "3AFE55443322110000000000000300000000" + fileChecksum.ToString("X4") + "0000000000000000000000000000000000";
                                break;*/
                        }
                        chksum = calculateChecksumForString(strChecksumToSend);
                        strChecksumToSend += chksum.ToString("X2");
                        CastInfoEvent(strChecksumToSend, 0);
                        WriteBinaryData(strChecksumToSend);
                        _switchBankDone = 0;
                        
                        _state = FlashState.WaitChecksumResults;
                        break;
                    case FlashState.WaitChecksumResults:
                        if (rxmsgtype == RxMsgType.StartChecksumVerification)
                        {
                            rxmsgtype = RxMsgType.Unknown;
                            CastInfoEvent("Verifying checksum", 0);
                            _communicationTimeout = 0;
                        }
                        else if (rxmsgtype == RxMsgType.FinishChecksumVerificationOK)
                        {
                            rxmsgtype = RxMsgType.Unknown;
                            CastInfoEvent("Verified checksum OK", 0);
                            _communicationTimeout = 0;
                            _state = FlashState.Idle;
                        }
                        else if (rxmsgtype == RxMsgType.FinishChecksumVerificationFailed)
                        {
                            rxmsgtype = RxMsgType.Unknown;
                            CastInfoEvent("Verified checksum failed", 0);
                            _communicationTimeout = 0;
                            _switchBankDone++;
                            if (_switchBankDone == 3)
                            {
                                _checksumType++;
                                if (_checksumType == 4) _state = FlashState.Idle;
                                else _state = FlashState.VerifyChecksum; // next try
                                Thread.Sleep(2500); // give it a rest
                            }
                        }
                        break;
                    case FlashState.StartErase:
                        if (_port.IsOpen)
                        {
                            allBytes = File.ReadAllBytes(_filename);
                            int tx_buf_pnt = 0;
                            int _partBufferLength = _length2Send / 2;
                            _fileOffset = 0;
                            // build the complete transmit buffer for lower flash bank
                            while (_fileOffset < _partBufferLength)
                            {
                                uint checksum = 0;
                                completeflashbuffer_lower[tx_buf_pnt ++] = 0x3A;
                                completeflashbuffer_lower[tx_buf_pnt++] = 0x20; // 32 bytes
                                checksum += completeflashbuffer_lower[tx_buf_pnt - 1];
                                byte addresshigh = Convert.ToByte(_fileOffset / 256);
                                byte addresslow = Convert.ToByte((_fileOffset - (256 * addresshigh)));
                                completeflashbuffer_lower[tx_buf_pnt++] = addresshigh;
                                checksum += completeflashbuffer_lower[tx_buf_pnt - 1];
                                completeflashbuffer_lower[tx_buf_pnt++] = addresslow;
                                checksum += completeflashbuffer_lower[tx_buf_pnt - 1];
                                completeflashbuffer_lower[tx_buf_pnt++] = 0x00; // indicate lower bank
                                checksum += completeflashbuffer_lower[tx_buf_pnt - 1];
                                for (int j = 0; j < 32; j++)
                                {
                                    completeflashbuffer_lower[tx_buf_pnt++] = allBytes[_fileOffset + j];
                                    checksum += completeflashbuffer_lower[tx_buf_pnt - 1];
                                }
                                checksum = 0x100 - checksum;
                                completeflashbuffer_lower[tx_buf_pnt++] = Convert.ToByte(checksum & 0x000000FF);
                                _fileOffset += 32;
                            }
                            tx_buf_pnt = 0;
                            // build the complete transmit buffer for upper flash bank
                            while (_fileOffset < _length2Send - 0x100)
                            {
                                uint checksum = 0;
                                completeflashbuffer_upper[tx_buf_pnt++] = 0x3A;
                                completeflashbuffer_upper[tx_buf_pnt++] = 0x20; // 32 bytes
                                checksum += completeflashbuffer_upper[tx_buf_pnt - 1];
                                byte addresshigh = Convert.ToByte((_fileOffset - 0x10000) / 256);
                                byte addresslow = Convert.ToByte(((_fileOffset - 0x10000) - (256 * addresshigh)));
                                completeflashbuffer_upper[tx_buf_pnt++] = addresshigh;
                                checksum += completeflashbuffer_upper[tx_buf_pnt - 1];
                                completeflashbuffer_upper[tx_buf_pnt++] = addresslow;
                                checksum += completeflashbuffer_upper[tx_buf_pnt - 1];
                                completeflashbuffer_upper[tx_buf_pnt++] = 0x00; // indicate upper bank <GS-19042011> was 0x02!
                                checksum += completeflashbuffer_upper[tx_buf_pnt - 1];
                                for (int j = 0; j < 32; j++)
                                {
                                    completeflashbuffer_upper[tx_buf_pnt++] = allBytes[_fileOffset + j];
                                    checksum += completeflashbuffer_upper[tx_buf_pnt - 1];
                                }
                                checksum = 0x100 - checksum;
                                completeflashbuffer_upper[tx_buf_pnt++] = Convert.ToByte(checksum & 0x000000FF);
                                _fileOffset += 32;
                            }
                            DumpTransmitBuffer();
                            _fileOffset = 0;
                            _communicationTimeout = 0;
                            CastInfoEvent("Starting erase", 0);
                            // send the erase command to the ECU
                            //WriteBinaryData("3A30303030303030314646");
                            //Thread.Sleep(2000);
                            WriteBinaryData("3AFF554433221100000000000002000000000000000000000000000000000000000000000000"); // checksum is also 00
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
                        else if (rxmsgtype == RxMsgType.ProgrammingVoltageOutOfRange)
                        {
                            rxmsgtype = RxMsgType.Unknown;
                            CastInfoEvent("Failed to erase flash, voltage out of range", 0);
                            AddToLogfile("Failed to erase flash, voltage out of range");

                            _state = FlashState.EraseError;
                            _communicationTimeout = 0;
                        }
                        else if (rxmsgtype == RxMsgType.Unknown)
                        {
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

                        // send a portion of the flash data from _fileOffset, 32 bytes
                        for (int t = 0; t < completeflashbuffer_lower.Length; t++)
                        {
                            _port.Write(completeflashbuffer_lower, t, 1);
                            if (t % 1024 == 0) CastInfoEvent("Flashing...", ((t * 100) / (completeflashbuffer_lower.Length * 2)));
                        }
                        _state = FlashState.WaitForFinishFirstBank;
                        break;
                    case FlashState.WaitForFinishFirstBank:
                        //TODO: <GS-18042011> Now we first have to wait for 3x MessageRxMsgType.FinishedFlashing - 0x34
                        if (rxmsgtype == RxMsgType.FinishedFlashing)
                        {
                            rxmsgtype = RxMsgType.Unknown;
                            _communicationTimeout = 0;
                            _waitFirstBankFlashed++;
                            if (_waitFirstBankFlashed == 3)
                            {
                                for (int i = 0; i < 250; i++)
                                {
                                    CastInfoEvent("Waiting...", 50);
                                    Thread.Sleep(1);
                                }
                                while (_port.BytesToWrite > 0) CastInfoEvent("Flashing...", 50); // wait it out.. maybe we should wait

                                _state = FlashState.SwitchBank;
                                CastInfoEvent("First bank programmed", 50);
                                AddToLogfile("First bank programmed");
                            }
                        }
                        else if (rxmsgtype == RxMsgType.NegativeAcknowledge)
                        {
                            rxmsgtype = RxMsgType.Unknown;
                            CastInfoEvent("Flashing bank 0 failed", 0);
                            _communicationTimeout = 0;
                            _state = FlashState.FlashingError;
                        }
                        break;
                    case FlashState.SwitchBank:
                        // now switch to upper bank
                                         //8788898A8B8C8D8E8F909192939495969798999A9B9C9D9E9F 
                                         //ACADAEAFB0B1B2B3B4B5B6B7B8B9BABBBCBDBE

                        // UPTO 0x01FF00
                        WriteBinaryData("3AFF554433221100000000000001FF0001010000000000000000000000000000000000000000"); // checksum is 00 again!
                        // UPTO 0x020000
                        //WriteBinaryData("3AFF5544332211000000000000020000010100000000000000000000000000000000000000FE"); // checksum is 00 again!
                        
                    //WriteBinaryData("3AFF554433221100000000000000000001010000000000000000000000000000000000000000"); // checksum is 00 again!
                        // update state to waiting for bankswitch
                        _communicationTimeout = 0;
                        CastInfoEvent("Switching bank", 50);
                        _state = FlashState.WaitBankSwitch;
                        break;
                    case FlashState.WaitBankSwitch:
                        if (rxmsgtype == RxMsgType.StartSwitchingBank)
                        {
                            rxmsgtype = RxMsgType.Unknown;
                            _communicationTimeout = 0;
                        }
                        else if (rxmsgtype == RxMsgType.FinishedSwitchingBank)
                        {
                            rxmsgtype = RxMsgType.Unknown;
                            CastInfoEvent("Bank switched", 50);
                            AddToLogfile("Bank switched");
                            _switchBankDone++;
                            if (_switchBankDone == 3)
                            {
                                _state = FlashState.SendFlashDataUpper; // <GS-19042011> nothing more to do, the extra acks where ghosts
                                echoCount = 0; // force buffer to be empty!
                                _switchBankDone = 0;
                                _communicationTimeout = 0;
                            }
                        }
                        else if (rxmsgtype == RxMsgType.NegativeAcknowledge)
                        {
                            rxmsgtype = RxMsgType.Unknown;
                            CastInfoEvent("Failed to switch bank", 0);
                            _state = FlashState.FlashingError;
                            _communicationTimeout = 0;
                        }
                        else if (rxmsgtype == RxMsgType.Unknown)
                        {
                            _communicationTimeout++;
                            if (_communicationTimeout > 1500)
                            {
                                rxmsgtype = RxMsgType.Unknown;
                                CastInfoEvent("Timeout switching bank", 0);
                                _state = FlashState.FlashingError;
                            }
                        }
                        break;
                    case FlashState.WaitAckForNextBank:
                        //CastInfoEvent("Waiting for ack for next bank", 0);
                        AddToLogfile("State = FlashState.WaitAckForNextBank");
                        if (rxmsgtype == RxMsgType.FinishedFlashing)
                        {
                            rxmsgtype = RxMsgType.Unknown;
                            CastInfoEvent("Ack for next bank", 50);
                            _switchBankDone++;
                            if (_switchBankDone == 3)
                            {
                                _state = FlashState.SendFlashDataUpper;
                                _communicationTimeout = 0;
                            }
                        }
                        else if (rxmsgtype == RxMsgType.Unknown)
                        {
                            _communicationTimeout++;
                            if (_communicationTimeout > 1000) // 10 seconds for testing
                            {
                                rxmsgtype = RxMsgType.Unknown;
                                CastInfoEvent("Timeout waiting for ack for next bank", 0);
                                _state = FlashState.SendFlashDataUpper; // proceed with next bank anyway
                            }
                        }
                        break;
                    case FlashState.SendFlashDataUpper:
                        // TODO: Do we have to wait here again, just like the first bank?
                        Thread.Sleep(2500); // <GS-20042011> testing
                        CastInfoEvent("Start sending upper bank flash data", 50);
                        for (int t = 0; t < completeflashbuffer_upper.Length; t++)
                        {
                            _port.Write(completeflashbuffer_upper, t, 1);
                            if (t % 1024 == 0) CastInfoEvent("Flashing...", 50 + ((t * 100) / (completeflashbuffer_upper.Length * 2)));
                        }
                        CastInfoEvent("Waiting...", 100);
                        for (int i = 0; i < 250; i++)
                        {
                            Thread.Sleep(1);
                        }
                        while (_port.BytesToWrite > 0) CastInfoEvent("Flashing...", 100); // wait it out.. maybe we should wait
                        _switchBankDone = 0;
                        _communicationTimeout = 0;
                        _state = FlashState.WaitEndFlashUpperBank;
                        break;
                    case FlashState.WaitFlashData:
                        if (_fileOffset >= _length2Send)
                        {
                            _state = FlashState.SendEndFlash;
                            //Thread.Sleep(100);
                        }
                        else _state = FlashState.SendFlashData;
                        break;
                    case FlashState.WaitEndFlashUpperBank:
                        //TODO: <GS-19042011> we receive three Acks here ... wait for that first!
                        if (rxmsgtype == RxMsgType.OutOfRangeError)
                        {
                            rxmsgtype = RxMsgType.Unknown;
                            CastInfoEvent("Out of range for bank 1", 100);
                            _switchBankDone++;
                            if (_switchBankDone == 3)
                            {
                                _state = FlashState.FlashingError;
                                _communicationTimeout = 0;
                            }
                        }
                        else if (rxmsgtype == RxMsgType.NegativeAcknowledge)
                        {
                            rxmsgtype = RxMsgType.Unknown;
                            CastInfoEvent("Failed to flash bank 1", 100);
                            _switchBankDone++;
                            if (_switchBankDone == 3)
                            {
                                _state = FlashState.FlashingError;
                                _communicationTimeout = 0;
                            }
                        }
                        else if (rxmsgtype == RxMsgType.FinishedFlashing)
                        {
                            rxmsgtype = RxMsgType.Unknown;
                            CastInfoEvent("Finished flashing bank 1", 100);
                            _switchBankDone++;
                            if (_switchBankDone == 3)
                            {
                                _state = FlashState.SendEndFlash;
                                _communicationTimeout = 0;
                            }
                        }
                        else if (rxmsgtype == RxMsgType.Unknown)
                        {
                            _communicationTimeout++;
                            if (_communicationTimeout > 1000) // 10 seconds for testing
                            {
                                rxmsgtype = RxMsgType.Unknown;
                                CastInfoEvent("Timeout waiting for ack for bank 1", 0);
                                _state = FlashState.SendEndFlash; // done?
                            }
                        }
                        break;
                    case FlashState.SendEndFlash:
                        /*Thread.Sleep(2000);
                        CastInfoEvent("Sending flash end sequence...", 100);
                        rxmsgtype = RxMsgType.Unknown;
                        WriteBinaryData("3A0000000000"); // null command
                        Thread.Sleep(500);
                        WriteBinaryData("3A30303030303030314646");
                        _communicationTimeout = 0;
                        _state = FlashState.WaitEndFlash;*/

                        Thread.Sleep(2000);
                        _communicationTimeout = 0;
                        CastInfoEvent("Starting checksum verification", 100);
                        // file checksum (0-0x20000) should be in location
                                                    //8788898A8B8C8D8E8F909192939495969798999A9B9C9D9E9F 
                        fileChecksum = 0;
                        for (int ci = 0; ci < 0x1FF00; ci++)
                        {
                            fileChecksum += allBytes[ci];
                        }
                        fileChecksum &= 0xFFFF;
                                             //8788898A8B8C8D8E8F909192939495969798999A9B9C9D9E9F 
                        strChecksumToSend = "3AFE55443322110000000000000300000000" + fileChecksum.ToString("X4") + "0000000000000000000000000000000000";
                        chksum = calculateChecksumForString(strChecksumToSend);
                        strChecksumToSend += chksum.ToString("X2");
                        WriteBinaryData(strChecksumToSend);

                                           //8788898A8B8C8D8E8F909192939495969798999A9B9C9D9E9F 
                        //WriteBinaryData("3AFE554433221100000000000003000000000000000000000000000000000000000000000000"); // checksum is also 00
                        _state = FlashState.WaitChecksumResultsAfterFlashing;
                        break;
                    case FlashState.WaitChecksumResultsAfterFlashing:
                        if (rxmsgtype == RxMsgType.StartChecksumVerification)
                        {
                            rxmsgtype = RxMsgType.Unknown;
                            CastInfoEvent("Verifying checksum", 100);
                            _communicationTimeout = 0;
                        }
                        else if (rxmsgtype == RxMsgType.FinishChecksumVerificationOK)
                        {
                            rxmsgtype = RxMsgType.Unknown;
                            CastInfoEvent("Verified checksum OK", 100);
                            _communicationTimeout = 0;
                            _state = FlashState.FlashingDone;
                        }
                        else if (rxmsgtype == RxMsgType.FinishChecksumVerificationFailed)
                        {
                            rxmsgtype = RxMsgType.Unknown;
                            CastInfoEvent("Verified checksum failed", 0);
                            _communicationTimeout = 0;
                            _state = FlashState.FlashingError;
                        }
                        break;
                    case FlashState.WaitEndFlash:
                        if (rxmsgtype == RxMsgType.FinishedFlashing)
                        {
                            rxmsgtype = RxMsgType.Unknown;
                            CastInfoEvent("Finished writing flash", 100);
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
                                //CastInfoEvent("Flashing failed", 0);
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
                    //TODO: This should be started later when flashing is done! <GS-18042011>
                    case FlashState.VerifyChecksum:
                        CastInfoEvent("Starting checksum verification:" + _checksumType.ToString(), 0);
                        _communicationTimeout = 0;
                        allBytes = File.ReadAllBytes(_filename);
                        fileChecksum = 0;

                        switch (_checksumType)
                        {
                            case 0: // normal calculation
                                for (int ci = 0; ci < 0x1FF00; ci++)
                                {
                                    fileChecksum += allBytes[ci];
                                }
                                fileChecksum &= 0xFFFF;

                                strChecksumToSend = "3AFE55443322110000000000000300000000" + fileChecksum.ToString("X4") + "0000000000000000000000000000000000";
                                break;
                            case 1: // assume first bank programmed, and second bank empty (FFs)
                                for (int ci = 0; ci < 0x10000; ci++)
                                {
                                    fileChecksum += allBytes[ci];
                                }
                                for (int ci = 0; ci < 0xFF00; ci++)
                                {
                                    fileChecksum += 0xFF;
                                }
                                fileChecksum &= 0xFFFF;
                                strChecksumToSend = "3AFE55443322110000000000000300000000" + fileChecksum.ToString("X4") + "0000000000000000000000000000000000";
                                break;
                            case 2: // assume entire flash chip empty (FFs)
                                for (int ci = 0; ci < 0x1FF00; ci++)
                                {
                                    fileChecksum += 0xFF;
                                }
                                fileChecksum &= 0xFFFF;
                                strChecksumToSend = "3AFE55443322110000000000000300000000" + fileChecksum.ToString("X4") + "0000000000000000000000000000000000";
                                break;
                            case 3: // assume entire flash chip 00s
                                fileChecksum &= 0xFFFF;
                                strChecksumToSend = "3AFE55443322110000000000000300000000" + fileChecksum.ToString("X4") + "0000000000000000000000000000000000";
                                break;
                            /*case 4: // assume first bank overwritten with second bank (possible?)
                                for (int ci = 0; ci < 0x1FF00; ci++)
                                {
                                    fileChecksum += 0xFF;
                                }
                                fileChecksum &= 0xFFFF;
                                strChecksumToSend = "3AFE55443322110000000000000300000000" + fileChecksum.ToString("X4") + "0000000000000000000000000000000000";
                                break;*/
                        }
                        chksum = calculateChecksumForString(strChecksumToSend);
                        strChecksumToSend += chksum.ToString("X2");
                        CastInfoEvent(strChecksumToSend, 0);
                        WriteBinaryData(strChecksumToSend);
                        _switchBankDone = 0;

                        _state = FlashState.WaitChecksumResults;
                        break;
                    case FlashState.WaitChecksumResults:
                        if (rxmsgtype == RxMsgType.StartChecksumVerification)
                        {
                            rxmsgtype = RxMsgType.Unknown;
                            CastInfoEvent("Verifying checksum", 0);
                            _communicationTimeout = 0;
                        }
                        else if (rxmsgtype == RxMsgType.FinishChecksumVerificationOK)
                        {
                            rxmsgtype = RxMsgType.Unknown;
                            CastInfoEvent("Verified checksum OK", 0);
                            _communicationTimeout = 0;
                            _state = FlashState.Idle;
                        }
                        else if (rxmsgtype == RxMsgType.FinishChecksumVerificationFailed)
                        {
                            rxmsgtype = RxMsgType.Unknown;
                            CastInfoEvent("Verified checksum failed", 0);
                            _communicationTimeout = 0;
                            _switchBankDone++;
                            if (_switchBankDone == 3)
                            {
                                _checksumType++;
                                if (_checksumType == 4) _state = FlashState.Idle;
                                else _state = FlashState.VerifyChecksum; // next try
                                Thread.Sleep(2500); // give it a rest
                            }
                        }
                        break;
                    case FlashState.StartErase:
                        if (_port.IsOpen)
                        {
                            allBytes = File.ReadAllBytes(_filename);
                            int tx_buf_pnt = 0;
                            int _partBufferLength = _length2Send / 2;
                            _fileOffset = 0;
                            // build the complete transmit buffer for lower flash bank
                            while (_fileOffset < _partBufferLength)
                            {
                                uint checksum = 0;
                                completeflashbuffer[tx_buf_pnt++] = 0x3A;
                                completeflashbuffer[tx_buf_pnt++] = 0x20; // 32 bytes
                                checksum += completeflashbuffer[tx_buf_pnt - 1];
                                byte addresshigh = Convert.ToByte(_fileOffset / 256);
                                byte addresslow = Convert.ToByte((_fileOffset - (256 * addresshigh)));
                                completeflashbuffer[tx_buf_pnt++] = addresshigh;
                                checksum += completeflashbuffer[tx_buf_pnt - 1];
                                completeflashbuffer[tx_buf_pnt++] = addresslow;
                                checksum += completeflashbuffer[tx_buf_pnt - 1];
                                completeflashbuffer[tx_buf_pnt++] = 0x00; // indicate lower bank
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

                            // INSERT A SWITCH TO UPPER BANK COMMAND HERE. This DOES NOT GET PROGRAMMED
                            if (true)
                            {
                                uint checksum = 0;
                                completeflashbuffer[tx_buf_pnt++] = 0x3A;
                                completeflashbuffer[tx_buf_pnt++] = 0x20; 
                                checksum += completeflashbuffer[tx_buf_pnt - 1];
                                byte addresshigh = 0; 
                                byte addresslow = 0; 
                                completeflashbuffer[tx_buf_pnt++] = addresshigh;
                                checksum += completeflashbuffer[tx_buf_pnt - 1];
                                completeflashbuffer[tx_buf_pnt++] = addresslow;
                                checksum += completeflashbuffer[tx_buf_pnt - 1];
                                completeflashbuffer[tx_buf_pnt++] = 0x02; // indicate upper bank <GS-20042011> was 0x02
                                checksum += completeflashbuffer[tx_buf_pnt - 1];
                                for (int j = 0; j < 32; j++)
                                {
                                    completeflashbuffer[tx_buf_pnt++] = 0;
                                    checksum += completeflashbuffer[tx_buf_pnt - 1];
                                }
                                checksum = 0x100 - checksum;
                                completeflashbuffer[tx_buf_pnt++] = Convert.ToByte(checksum & 0x000000FF);
                            }
                            //tx_buf_pnt = 0;
                            // build the complete transmit buffer for upper flash bank
                            while (_fileOffset < _length2Send)
                            {
                                uint checksum = 0;
                                completeflashbuffer[tx_buf_pnt++] = 0x3A;
                                completeflashbuffer[tx_buf_pnt++] = 0x20; // 32 bytes
                                checksum += completeflashbuffer[tx_buf_pnt - 1];
                                byte addresshigh = Convert.ToByte((_fileOffset - 0x10000) / 256);
                                byte addresslow = Convert.ToByte(((_fileOffset - 0x10000) - (256 * addresshigh)));
                                completeflashbuffer[tx_buf_pnt++] = addresshigh;
                                checksum += completeflashbuffer[tx_buf_pnt - 1];
                                completeflashbuffer[tx_buf_pnt++] = addresslow;
                                checksum += completeflashbuffer[tx_buf_pnt - 1];
                                completeflashbuffer[tx_buf_pnt++] = 0x00; // indicate upper bank <GS-20042011> was 0x02
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
                            DumpFullTransmitBuffer();
                            _fileOffset = 0;
                            _communicationTimeout = 0;
                            CastInfoEvent("Starting erase", 0);
                            // send the erase command to the ECU
                                             //8788898A8B8C8D8E8F909192939495969798999A9B9C9D9E9F < RX BUF
                                             //ACADAEAFB0B1B2B3B4B5B6B7B8B9BABBBCBDBEBFC0C1C2C3C4 < COPY TO
                            WriteBinaryData("3AFF554433221100000000000002000000000000000000000000000000000000000000000000"); // checksum is also 00
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
                        else if (rxmsgtype == RxMsgType.ProgrammingVoltageOutOfRange)
                        {
                            rxmsgtype = RxMsgType.Unknown;
                            CastInfoEvent("Failed to erase flash, voltage out of range", 0);
                            AddToLogfile("Failed to erase flash, voltage out of range");

                            _state = FlashState.EraseError;
                            _communicationTimeout = 0;
                        }
                        else if (rxmsgtype == RxMsgType.Unknown)
                        {
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

                        // send a portion of the flash data from _fileOffset, 32 bytes
                        for (int t = 0; t < completeflashbuffer.Length; t++)
                        {
                            _port.Write(completeflashbuffer, t, 1);
                            if (t % 1024 == 0) CastInfoEvent("Flashing...", ((t * 100) / (completeflashbuffer.Length)));
                        }
                        _state = FlashState.WaitForFinishFirstBank;
                        break;
                    case FlashState.WaitForFinishFirstBank:
                        //TODO: <GS-18042011> Now we first have to wait for 3x MessageRxMsgType.FinishedFlashing - 0x34
                        if (rxmsgtype == RxMsgType.FinishedFlashing)
                        {
                            rxmsgtype = RxMsgType.Unknown;
                            _communicationTimeout = 0;
                            _waitFirstBankFlashed++;
                            if (_waitFirstBankFlashed == 3)
                            {
                                for (int i = 0; i < 250; i++)
                                {
                                    CastInfoEvent("Waiting...", 50);
                                    Thread.Sleep(1);
                                }
                                while (_port.BytesToWrite > 0) CastInfoEvent("Flashing...", 50); // wait it out.. maybe we should wait

                                _state = FlashState.SendEndFlash;
                                CastInfoEvent("Flash programmed", 50);
                                AddToLogfile("Flash programmed");
                            }
                        }
                        else if (rxmsgtype == RxMsgType.NegativeAcknowledge)
                        {
                            rxmsgtype = RxMsgType.Unknown;
                            CastInfoEvent("Flashing bank 0 failed", 0);
                            _communicationTimeout = 0;
                            _state = FlashState.FlashingError;
                        }
                        else if (rxmsgtype == RxMsgType.Unknown)
                        {
                            _communicationTimeout++;
                            if (_communicationTimeout > 1000) // 10 seconds for testing
                            {
                                rxmsgtype = RxMsgType.Unknown;
                                CastInfoEvent("Timeout waiting for ack for flashing", 0);
                                _state = FlashState.SendEndFlash; // done?
                            }
                        }
                        else
                        {
                            Console.WriteLine(rxmsgtype.ToString("X2") + " received");
                            _communicationTimeout = 0;
                            rxmsgtype = RxMsgType.Unknown;
                        }
                        break;
                    
                    case FlashState.SendEndFlash:
                        /*Thread.Sleep(2000);
                        CastInfoEvent("Sending flash end sequence...", 100);
                        rxmsgtype = RxMsgType.Unknown;
                        WriteBinaryData("3A0000000000"); // null command
                        Thread.Sleep(500);
                        WriteBinaryData("3A30303030303030314646");
                        _communicationTimeout = 0;
                        _state = FlashState.WaitEndFlash;*/

                        Thread.Sleep(2000);
                        _communicationTimeout = 0;
                        CastInfoEvent("Starting checksum verification", 100);
                        // file checksum (0-0x20000) should be in location
                        //8788898A8B8C8D8E8F909192939495969798999A9B9C9D9E9F 
                        fileChecksum = 0;
                        for (int ci = 0; ci < 0x1FF00; ci++)
                        {
                            fileChecksum += allBytes[ci];
                        }
                        fileChecksum &= 0xFFFF;
                        //8788898A8B8C8D8E8F909192939495969798999A9B9C9D9E9F 
                        strChecksumToSend = "3AFE55443322110000000000000300000000" + fileChecksum.ToString("X4") + "0000000000000000000000000000000000";
                        chksum = calculateChecksumForString(strChecksumToSend);
                        strChecksumToSend += chksum.ToString("X2");
                        WriteBinaryData(strChecksumToSend);

                        //8788898A8B8C8D8E8F909192939495969798999A9B9C9D9E9F 
                        //WriteBinaryData("3AFE554433221100000000000003000000000000000000000000000000000000000000000000"); // checksum is also 00
                        _state = FlashState.WaitChecksumResultsAfterFlashing;
                        break;
                    case FlashState.WaitChecksumResultsAfterFlashing:
                        if (rxmsgtype == RxMsgType.StartChecksumVerification)
                        {
                            rxmsgtype = RxMsgType.Unknown;
                            CastInfoEvent("Verifying checksum", 100);
                            _communicationTimeout = 0;
                        }
                        else if (rxmsgtype == RxMsgType.FinishChecksumVerificationOK)
                        {
                            rxmsgtype = RxMsgType.Unknown;
                            CastInfoEvent("Verified checksum OK", 100);
                            _communicationTimeout = 0;
                            _state = FlashState.FlashingDone;
                        }
                        else if (rxmsgtype == RxMsgType.FinishChecksumVerificationFailed)
                        {
                            rxmsgtype = RxMsgType.Unknown;
                            CastInfoEvent("Verified checksum failed", 0);
                            _communicationTimeout = 0;
                            _state = FlashState.FlashingError;
                        }
                        break;
                    case FlashState.WaitEndFlash:
                        if (rxmsgtype == RxMsgType.FinishedFlashing)
                        {
                            rxmsgtype = RxMsgType.Unknown;
                            CastInfoEvent("Finished writing flash", 100);
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
                                //CastInfoEvent("Flashing failed", 0);
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

        private byte calculateChecksumForString(string str2calc)
        {
            uint checksum = 0;
            for (int i = 2; i < str2calc.Length; i += 2)
            {
                byte b = Convert.ToByte(str2calc.Substring(i, 2), 16);
                checksum += b;
            }
            checksum = (checksum & 0x000000FF);
            checksum = 0x100 - checksum;
            checksum = (checksum & 0x000000FF);
            byte bchecksum = Convert.ToByte(checksum);
            return bchecksum;
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
