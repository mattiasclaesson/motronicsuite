using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Threading;
using System.Timers;
using System.Diagnostics;
using MicroLibrary;

namespace MotronicCommunication
{

    class DumbKLineDevice
    {
        public enum CommunicationState : int
        {
            Start,
            SendWakeup,
            WaitForKeywords,
            Idle,
            SendCommand,
            WaitForResponse,
            Timeout
        }

        public static int BYTE_LENGTH = 8;
        public static int BIT_INTERVAL_5B = 200;
        public static int SYNC_BYTE = 0x55;
        public static int P4MIN = 5 + 1;
        public static int IDLE_TIMEOUT = 300; // 3 seconds


        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        public static extern uint MM_BeginPeriod(uint uMilliseconds);
        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        public static extern uint MM_EndPeriod(uint uMilliseconds);

        public event ICommunication.StatusChanged onStatusChanged;
        public event ICommunication.ECUInfo onECUInfo;

        private ICommunication.ECUState _ecustate = ICommunication.ECUState.NotInitialized;
 
        private SerialPort _port = new SerialPort();
        private System.Timers.Timer _timer;
        private MicroTimer _microTimer;

        private int _startctr = 0;
        private bool _initIsDone = false;
        private int _ecuaddr = 0x33;
        private int _baudrate = 10400;
        private int _timeout = 0;
        private int _wakeupRetries = 1;
        private int _echo = 0;
        private bool _idlesent = false;

        private bool _syncseen = false;
        private bool _kw1seen = false;
        private bool _kw2seen = false;
        private bool _invaddrseen = false;

        private byte _kw1;
        private byte _kw2;

        private List<byte> _sendMsg;
        private List<byte> _idleMsg;
        private int _sendctr = 0;

        private List<byte> _rcvMsg = new List<byte>();

        private Stopwatch _stopWatch = new Stopwatch();
        private static AutoResetEvent _event = new AutoResetEvent(false);

        private CommunicationState _state = CommunicationState.Start;

        public void setIdleMessage(List<byte> msg)
        {
            _idleMsg = msg;
        }

        public bool send(List<byte> msg)
        {
            if (_state == CommunicationState.SendCommand || _state == CommunicationState.WaitForResponse)
            {
                //already sending something (idle message)
                Console.WriteLine("already sending something (idle message)");
                return false;
            }

            _timeout = 0;
            _sendMsg = msg;
            _sendctr = 0;
            _state = CommunicationState.SendCommand;
            _microTimer.Interval = P4MIN * 1000;
            _microTimer.Enabled = true;

            return true;
        }

        public List<byte> receive()
        {
            _event.WaitOne();

            List<byte> msg = new List<byte>(_rcvMsg);
            _rcvMsg.Clear();

            return msg;
        }

        public bool slowInit(string comportnumber, int ecuaddr, int baudrate)
        {
            clear();

            _ecuaddr = ecuaddr;
            _baudrate = baudrate;

            try
            {
                if (!_initIsDone)
                {

                    _timer = new System.Timers.Timer(10);
                    _timer.Elapsed += new ElapsedEventHandler(_timer_Elapsed);

                    //micro timer is used for precise events such as the 5baud init
                    //sleep is not accurate enough with all computers even with MM period set to 1ms
                    _microTimer = new MicroTimer(BIT_INTERVAL_5B * 1000);
                    _microTimer.MicroTimerElapsed += new MicroLibrary.MicroTimer.MicroTimerElapsedEventHandler(microTimerEvent);

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
                        //AddToLog("Failed to set pins: " + E.Message);
                    }
                    //no need for this anymore
                    //MM_BeginPeriod(1);
                    //Thread.CurrentThread.Priority = ThreadPriority.AboveNormal; // high prio thread
                    _initIsDone = true;
                }
                else
                {
                    //MM_BeginPeriod(1);
                    if (_port.IsOpen) _port.Close();
                    _port.BaudRate = _baudrate;
                    _port.PortName = comportnumber;
                    _port.Open();
                }

                _timer.Enabled = true;
                _microTimer.Enabled = true;
                return true;
            }
            catch (Exception E)
            {
                _ecustate = ICommunication.ECUState.NotInitialized;
                CastInfoEvent("Failed to initialize: " + E.Message, 0);
                Console.WriteLine("Failed to initialize: " + E.Message);
            }
            return false;
        }

        public void stop()
        {
            Cleanup();
        }

        private void clear()
        {
            _ecustate = ICommunication.ECUState.NotInitialized;
 
            _startctr = 0;
            _timeout = 0;
            _wakeupRetries = 1;
            _echo = 0;
            _idlesent = false;

            _kw1 = 0;
            _kw2 = 0;

            _syncseen = false;
            _kw1seen = false;
            _kw2seen = false;
            _invaddrseen = false;

            _sendctr = 0;
            _state = CommunicationState.Start;
        }

        private void Cleanup()
        {
            try
            {
                //MM_EndPeriod(1);
                if (_port.IsOpen) _port.Close();
            }
            catch (Exception E)
            {
                //AddToLog("Failed to reset thread high prio: " + E.Message);
            }

        }

        /*
        private void sendAt5Baud(int addr)
        {
            //set start bit (line low --> break true)
            _port.BreakState = true;
            Thread.Sleep(BIT_INTERVAL_5B);

            int i = 0;
            for (i = 0; i < BYTE_LENGTH; ++i)
            {
                //_stopWatch.Stop();
                //Console.WriteLine("5Baud interval was " + _stopWatch.Elapsed.TotalMilliseconds + " ms");
                //_stopWatch.Reset();
                //_stopWatch.Start();
                if ((addr & (1 << i)) > 0)
                {
                    //set line high
                    _port.BreakState = false;
                }
                else
                {
                    //set line low
                    _port.BreakState = true;
                }
                Thread.Sleep(BIT_INTERVAL_5B);
            }
            //_stopWatch.Reset();

            //set stop bit
            _port.BreakState = false;
            Thread.Sleep(BIT_INTERVAL_5B);


        }
        */

        private void handle5BaudTimer()
        {
            //_stopWatch.Stop();
            //TimeSpan ts = _stopWatch.Elapsed;
            //Console.WriteLine("5baud microtimer interval was " + ts.TotalMilliseconds + " ms");
            //_stopWatch.Reset();
            //_stopWatch.Start();

            if (_startctr == 0)
            {
                _ecustate = ICommunication.ECUState.NotInitialized;
                CastInfoEvent("Sending init/wakeup sequence [" + _wakeupRetries.ToString() + "/5]", 0);
                //start bit
                _port.BreakState = true;
            }
            else if (_startctr == 9)
            {
                //stop bit
                _port.BreakState = false;

            }
            else if (_startctr == 10)
            {
                _stopWatch.Reset();
                _state = CommunicationState.WaitForKeywords;

                _port.BaudRate = _baudrate;
                _timeout = 0;
                _startctr = 0;
                _microTimer.Enabled = false;
            }
            else
            {
                if ((_ecuaddr & (1 << (_startctr - 1))) > 0)
                {
                    //set line high
                    //Console.WriteLine("HIGH");
                    _port.BreakState = false;
                }
                else
                {
                    //set line low
                    //Console.WriteLine("LOW");
                    _port.BreakState = true;
                }
            }
            ++_startctr;
        }

        private void microTimerEvent(object sender, MicroLibrary.MicroTimerEventArgs timerEventArgs)
        {
            if (_port.IsOpen)
            {
                try
                {
                    switch (_state)
                    {
                        case CommunicationState.Start:
                            handle5BaudTimer();
                            break;

                        case CommunicationState.SendCommand:
                            //_stopWatch.Stop();
                            //TimeSpan ts = _stopWatch.Elapsed;
                            //Console.WriteLine("byte interval was " + ts.TotalMilliseconds + " ms");
                            //_stopWatch.Reset();
                            //_stopWatch.Start();

                            byte[] b = new byte[1];
                            b[0] = _sendMsg[_sendctr];
                            ++_echo; // ignore the echo byte that will be coming
                            _port.Write(b, 0, 1);
                            

                            ++_sendctr;
                            if (_sendctr >= _sendMsg.Count)
                            {
                                //_sendMsg.Clear();
                                _timeout = 0;
                                _state = CommunicationState.WaitForResponse;
                                _sendctr = 0;
                                _microTimer.Enabled = false;
                                Console.WriteLine("Finished sending");
                            }

                            break;
                    }
                }
                catch (Exception E)
                {
                    //AddToLog(E.Message);
                    Console.WriteLine(E.Message);
                }
            }
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _timer.Enabled = false;
            if (_port.IsOpen)
            {
                try
                {
                    switch (_state)
                    {
                        case CommunicationState.Start:
                            //_ecustate = ECUState.NotInitialized;
                            //CastInfoEvent("Sending init/wakeup sequence [" + _wakeupRetries.ToString() + "/5]", 0);
                            //_port.BaudRate = 5;

                            //Console.WriteLine("Starting 5baud init!");

                            //sendAt5Baud(_ecuaddr);

                            //_state = CommunicationState.WaitForKeywords;

                            //_port.BaudRate = _baudrate;
                            //_timeout = 0;

                            //not used currently...
                            //handle5BaudTimer()

                            break;
                        case CommunicationState.WaitForKeywords:
                            if (_timeout == 0 || _timeout == 100 || _timeout == 200 || _timeout == 300 || _timeout == 400 || _timeout == 500)
                            {
                                int secs = _timeout / 100;
                                CastInfoEvent("Waiting for keywords from ECU (" + secs.ToString() + "/5 seconds)", 0);
                            }
                            if (_timeout++ > 500)
                            {
                                _ecustate = ICommunication.ECUState.NotInitialized;
                                CastInfoEvent("Timeout waiting for keywords", 0);
                                Console.WriteLine("Timeout waiting for keywords");
                                _state = CommunicationState.Start;
                                _timeout = 0;
                                _wakeupRetries++;
                                if (_wakeupRetries == 6)
                                {
                                    _wakeupRetries = 1;

                                    stop();
                                    CastInfoEvent("Unable to connect to ECU", 0);
                                    return; // don't restart the timer
                                }
                                _microTimer.Enabled = true;
                            }
                            // timeout?
                            break;

                        case CommunicationState.Idle:
                            if (_timeout++ > IDLE_TIMEOUT)
                            {
                                //send the idle message to prevent the connection from closing
                                Console.WriteLine("Send the idle message");
                                _idlesent = true;
                                _timeout = 0;
                                _sendMsg = _idleMsg;
                                _sendctr = 0;
                                _state = CommunicationState.SendCommand;
                                _microTimer.Interval = P4MIN * 1000;
                                _microTimer.Enabled = true;
                            }

                            break;

                        case CommunicationState.WaitForResponse:
                            ++_timeout;
                            if (_timeout == 6)
                            {
                                Console.WriteLine("Receiving finished");
                                _timeout = 0;
                                _state = CommunicationState.Idle;

                                //inform that complete message arrived
                                if (_idlesent == false)
                                {
                                    _event.Set();
                                }
                                else
                                {
                                    _idlesent = false;
                                    _rcvMsg.Clear();
                                }
                            }
                            break;
                    }
                }
                catch (Exception E)
                {
                    //AddToLog(E.Message);
                    Console.WriteLine(E.Message);
                }
            }
            _timer.Enabled = true;
        }

        private void SendAck(byte b2ack, bool invert)
        {
            byte[] b2send = new byte[1];
            b2send[0] = b2ack;
            if (invert) b2send[0] ^= 0xFF;
            if (_port.IsOpen)
            {
                _port.Write(b2send, 0, 1);
                ++_echo; // ignore the echo byte that will be coming
            }
        }

        private void receiveByte(byte b)
        {
            _rcvMsg.Add(b);
            _timeout = 0;
        }

        private void HandleInitByte(byte b)
        {
            if (!_syncseen)
            {
                if (b == SYNC_BYTE)
                {
                    _syncseen = true;
                    Console.WriteLine("Sync byte received!");
                }
                //CastInfoEvent("Synchronization in progress", 0);
            }
            else
            {
                if (!_kw1seen)
                {
                    _kw1seen = true;
                    _kw1 = b;
                }
                else if (!_kw2seen)
                {
                    _kw2seen = true;
                    _kw2 = b;
                    //TODO: need to apply W4 delay (25-50ms) before sending this?
                    SendAck(_kw2, true);
                    Console.WriteLine("Keywords: " + _kw1.ToString("X2") + _kw2.ToString("X2"));
                    //AddToLog("kw1: " + kw1.ToString("X2") + " kw2: " + kw2.ToString("X2"));
                    //AddToLog("Entering idle state");
                }
                else if (!_invaddrseen)
                {
                    _invaddrseen = true;
                    _state = CommunicationState.Idle;
                    _timeout = 0;
                    _ecustate = ICommunication.ECUState.CommunicationRunning;
                    CastInfoEvent("Communication ready", 0);
                    CastECUInfoEvent(3, "M2.10.3"); //a bit of hack but works
                    Console.WriteLine("Inverted address received!");
                }
            }
        }

        private void _port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (e.EventType == SerialData.Chars)
            {
                string rxdata = _port.ReadExisting();
                for (int t = 0; t < rxdata.Length; t++)
                {
                    byte b = Convert.ToByte(rxdata[t]);

                    if (_echo > 0)
                    {
                        //ignore received echo
                        --_echo;
                        continue;
                    }

                    Console.Write(b.ToString("X2") + " ");
                    if (_state == CommunicationState.Start || _state == CommunicationState.WaitForKeywords)
                    {
                        HandleInitByte(b);
                    }
                    else
	                {
                        receiveByte(b);
	                }

                }
            }
        }

        private void CastECUInfoEvent(int idnumber, string info)
        {
            if (onECUInfo != null)
            {
                onECUInfo(this, new ICommunication.ECUInfoEventArgs(info, idnumber));
            }
        }

        private void CastInfoEvent(string information, int percentage)
        {
            if (onStatusChanged != null)
            {
                onStatusChanged(this, new ICommunication.StatusEventArgs(information, percentage, _ecustate));
            }
        }
    }
}
