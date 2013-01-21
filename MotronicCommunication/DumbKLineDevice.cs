using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Threading;
using System.Timers;

namespace MotronicCommunication
{

    class DumbKLineDevice : IKLineDevice
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


        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        public static extern uint MM_BeginPeriod(uint uMilliseconds);
        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        public static extern uint MM_EndPeriod(uint uMilliseconds);

        private SerialPort _port = new SerialPort();
        private System.Timers.Timer _timer;

        private bool _initIsDone = false;
        private int _ecuaddr = 0x33;
        private int _baudrate = 10400;
        private int _timeout = 0;
        private int _wakeupRetries = 1;
        private bool _echo = false;

        private bool _syncseen = false;
        private bool _kw1seen = false;
        private bool _kw2seen = false;
        private bool _invaddrseen = false;

        private byte _kw1;
        private byte _kw2;

        private CommunicationState _state = CommunicationState.Start;

        public override bool slowInit(string comportnumber, int ecuaddr, int baudrate)
        {
            _ecuaddr = ecuaddr;
            _baudrate = baudrate;

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
                        //AddToLog("Failed to set pins: " + E.Message);
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
                //_ecustate = ECUState.NotInitialized;
                //CastInfoEvent("Failed to initialize KWP71: " + E.Message, 0);
            }
            return false;
        }

        public void stop()
        {
            Cleanup();
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
                //AddToLog("Failed to reset thread high prio: " + E.Message);
            }

        }

        private void sendAt5Baud(int addr)
        {
            //set start bit (line low --> break true)
            _port.BreakState = true;
            Thread.Sleep(BIT_INTERVAL_5B);

            int i = 0;
            for (i = 0; i < BYTE_LENGTH; ++i)
            {
                if ((addr & (1 << i)) == 1)
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

            //set stop bit
            _port.BreakState = false;
            Thread.Sleep(BIT_INTERVAL_5B);

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

                            sendAt5Baud(_ecuaddr);

                            _state = CommunicationState.WaitForKeywords;

                            _port.BaudRate = _baudrate;
                            _timeout = 0;
                            break;
                        case CommunicationState.WaitForKeywords:
                            if (_timeout == 0 || _timeout == 100 || _timeout == 200 || _timeout == 300 || _timeout == 400 || _timeout == 500)
                            {
                                int secs = _timeout / 100;
                                //CastInfoEvent("Waiting for keywords from ECU (" + secs.ToString() + "/5 seconds)", 0);
                            }
                            if (_timeout++ > 500)
                            {
                                //_ecustate = ECUState.NotInitialized;
                                //CastInfoEvent("Timeout waiting for keywords", 0);
                                _state = CommunicationState.Start;
                                _timeout = 0;
                                _wakeupRetries++;
                                if (_wakeupRetries == 6)
                                {
                                    _wakeupRetries = 1;

                                    stop();
                                    //CastInfoEvent("Unable to connect to ECU", 0);
                                    return; // don't restart the timer
                                }
                            }
                            // timeout?
                            break;
                    }
                }
                catch (Exception E)
                {
                    //AddToLog(E.Message);
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
                _echo = true; // ignore the echo byte that will be coming
            }
        }

        private void HandleInitByte(byte b)
        {
            if (!_syncseen)
            {
                if (b == SYNC_BYTE) _syncseen = true;
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
                    //AddToLog("kw1: " + kw1.ToString("X2") + " kw2: " + kw2.ToString("X2"));
                    //AddToLog("Entering idle state");
                }
                else if (!_invaddrseen)
                {
                    _invaddrseen = true;
                    _state = CommunicationState.Idle;
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
                    if (_echo)
                    {
                        //ignore received echo
                        _echo = false;
                        continue;
                    }

                    byte b = Convert.ToByte(rxdata[t]);
                    if (_state == CommunicationState.Start || _state == CommunicationState.WaitForKeywords)
                    {
                            HandleInitByte(b);
                    }
                    else
	                {

	                }

                }
            }
        }
    }
}
