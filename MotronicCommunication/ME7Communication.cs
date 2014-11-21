/*
A/C pressure;A6 10 01 01 ;*13.54-176;kPa;
Battery voltage;A6 10 0A 01 ;*0.07;V;
Engine speed;A6 10 1D 01 ;*40;rpm;
Throttle angle (from ETM);A6 10 58 01 ;*100/256;%;
Mass air flow;A6 10 AE 01 ;*0.1;kg/h;
Intake air temperature;A6 10 CE 01 ;X*0.75-48;C;
Coolant temperature;A6 10 D8 01 ;*0.75-48;C;
Engine coolant temperature;A6 10 DD 01 ;*0.75-48;C;
Vehicle speed;A6 11 40 01 ;*1.25;km/h;
Ignition angle;A6 10 36 01 ;*191.25/255;BTDC;
Turbo control valve dutycycle;A6 10 37 01 ;*100/256;%;
Boost pressure sensor, V;A6 12 9C 01 ;*5.0/1024;V;
Boost pressure;A6 12 9D 01 ;*10;hPa;
Accelerator pedal, PWM (via ETM);A6 12 A1 01 ;*100/65535;%;
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using MotronicCommunication.CAN;
using System.IO;
using MotronicCommunication.KWP;
using System.Threading;

namespace MotronicCommunication
{
    public enum ActivityType : int
    {
        StartUploadingBootloader,
        UploadingBootloader,
        FinishedUploadingBootloader,
        StartFlashing,
        UploadingFlash,
        FinishedFlashing,
        StartErasingFlash,
        ErasingFlash,
        FinishedErasingFlash,
        DownloadingSRAM,
        ConvertingFile,
        StartDownloadingFlash,
        DownloadingFlash,
        FinishedDownloadingFlash,
        StartDownloadingFooter,
        DownloadingFooter,
        FinishedDownloadingFooter
    }

    public class ME7Communication : ICommunication
    {
        private System.Timers.Timer tmr = new System.Timers.Timer(3000);
        private System.Timers.Timer tmrSpeed = new System.Timers.Timer(1000);
        private bool _stallKeepAlive = false;

        public bool StallKeepAlive
        {
            get { return _stallKeepAlive; }
            set { _stallKeepAlive = value; }
        }

        private bool m_EnableCanLog = false;

        public bool EnableCanLog
        {
            get { return m_EnableCanLog; }
            set
            {
                m_EnableCanLog = value;
                if (canUsbDevice != null)
                {
                    canUsbDevice.EnableCanLog = m_EnableCanLog;
                }
            }
        }

        ICANDevice canUsbDevice;
        public delegate void WriteProgress(object sender, WriteProgressEventArgs e);
        public event ME7Communication.WriteProgress onWriteProgress;

        public delegate void ReadProgress(object sender, ReadProgressEventArgs e);
        public event ME7Communication.ReadProgress onReadProgress;

        public delegate void BytesTransmitted(object sender, WriteProgressEventArgs e);
        public event ME7Communication.BytesTransmitted onBytesTransmitted;

        public delegate void CanInfo(object sender, CanInfoEventArgs e);
        public event ME7Communication.CanInfo onCanInfo;
        public delegate void CanCount(object sender, CanCountEventArgs e);
        public event ME7Communication.CanCount onCanCount;
        public delegate void CanBusLoad(object sender, CanbusLoadEventArgs e);
        public event ME7Communication.CanBusLoad onCanBusLoad;
        


        // implements functions for canbus access for Trionic 8
        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        public static extern uint MM_BeginPeriod(uint uMilliseconds);
        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        public static extern uint MM_EndPeriod(uint uMilliseconds);

        //private ICANDevice m_canDevice = null;
        private CANListener m_canListener;

        private void CastProgressWriteEvent(float percentage)
        {
            if (onWriteProgress != null)
            {
                onWriteProgress(this, new WriteProgressEventArgs(percentage));
            }
        }

        private void CastProgressReadEvent(float percentage)
        {
            if (onReadProgress != null)
            {
                onReadProgress(this, new ReadProgressEventArgs(percentage));
            }
        }

        private void CastBytesTransmitted(int bytestransmitted)
        {

            if (onBytesTransmitted != null)
            {
                onBytesTransmitted(this, new WriteProgressEventArgs(bytestransmitted));
            }
        }

        private void CastBusLoadEvent(float busload)
        {
            if (onCanBusLoad != null)
            {
                onCanBusLoad(this, new CanbusLoadEventArgs(busload));
            }

        }

        private void CastInfoEvent(string info, ActivityType type)
        {
            //Console.WriteLine(info);
            if (onCanInfo != null)
            {
                onCanInfo(this, new CanInfoEventArgs(info, type));
            }
            //onECUInfo(this, new ECUInfoEventArgs(info, 0));
            //if(onStatusChanged != null) onStatusChanged(this, new StatusEventArgs(info, 0, ECUState.CommunicationRunning));
        }

        private void CastCounterEvent(int rxcount, int txcount, int errcount)
        {
            //Console.WriteLine(info);
            if (onCanCount != null)
            {
                onCanCount(this, new CanCountEventArgs(rxcount, txcount, errcount));
            }
        }

        public ME7Communication()
        {
            tmr.Elapsed += new System.Timers.ElapsedEventHandler(tmr_Elapsed);
            tmrSpeed.Elapsed += new System.Timers.ElapsedEventHandler(tmrSpeed_Elapsed);
        }

        void tmrSpeed_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // bits per second
            float busload = canUsbDevice.BitsPerSecond * 100 / 250000;
            canUsbDevice.BitsPerSecond = 0;
            CastBusLoadEvent(busload);
        }

        private void SendKeepAlive()
        {
            CANMessage msg = new CANMessage(0x7E0, 0, 2);//<GS-18052011> ELM327 support requires the length byte
            msg.setFlags(LAWICEL.CANMSG_EXTENDED);
            ulong cmd = 0x0000000000003E01; // always 2 bytes
            msg.setData(cmd);
            //Console.WriteLine("KA sent");
            m_canListener.setupWaitMessage(0x7E8);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage response = new CANMessage();
            response = m_canListener.waitMessage(1000);
            //Console.WriteLine("received KA: " + response.getCanData(1).ToString("X2"));
        }

        private void AddToCanTrace(string line)
        {
            Console.WriteLine(line);
            if (m_EnableCanLog)
            {
                DateTime dtnow = DateTime.Now;
                lock (this)
                {
                    try
                    {
                        using (StreamWriter sw = new StreamWriter(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\dataTrace.txt", true))
                        {
                            sw.WriteLine(dtnow.ToString("dd/MM/yyyy HH:mm:ss.fff") + " - " + line);
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            }
        }

        void tmr_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (canUsbDevice.isOpen())
            {
                // send keep alive
                if (!_stallKeepAlive)
                {
                    AddToCanTrace("Send KA based on timer");
                    SendKeepAlive();
                }
            }
        }

        private string m_forcedComport = string.Empty;

        public string ForcedComport
        {
            get { return m_forcedComport; }
            set { m_forcedComport = value; }
        }


        public override bool WriteEprom(string filename, int timeout)
        {
            return ProgramECU(filename);
        }

        public bool ProgramECU(string filename)
        {
            bool retval = false;
            if (canUsbDevice.isOpen())
            {
                EnterProgrammingMode();
                Thread.Sleep(1000);
                if (SendSBL())
                {
                    Thread.Sleep(1000);
                    // start secondary bootloader
                    StartSBL();
                    Thread.Sleep(1000);
                    // erase flash
                    // send blocks of 0x4000 bytes at a time 
                    if (EraseFlash())
                    {
                        Thread.Sleep(1000);
                        // upload the new file
                        SendFlashFile(filename);
                        Thread.Sleep(1000);
                        LeaveProgrammingMode();
                        if (onStatusChanged != null) onStatusChanged(this, new StatusEventArgs("ECU Flashed", 100, ECUState.CommunicationRunning));
                    }
                    else
                    {
                        SendFlashFile(filename);
                        Thread.Sleep(1000);
                        LeaveProgrammingMode();
                        if (onStatusChanged != null) onStatusChanged(this, new StatusEventArgs("ECU Flashed", 100, ECUState.CommunicationRunning));
                    }
                }
            }
            return retval;
        }

        public void Reset()
        {
            if (canUsbDevice.isOpen())
            {
                CANMessage msg;
                CANMessage response;

                //Set start address
                //S FFFFE 7A 9C 00 00 80 00 00 00
                //R 21 7A 9C 00 00 80 00 AA AA
                msg = new CANMessage(0x000FFFFE, LAWICEL.CANMSG_EXTENDED, 8);
                msg.setFlags(LAWICEL.CANMSG_EXTENDED);
                msg.setData(0x000000000000C87A);
                m_canListener.setupWaitMessage(0x000021);
                canUsbDevice.sendMessage(msg);
                Thread.Sleep(1);
                response = new CANMessage();
                response = m_canListener.waitMessage(500);
                if (response.getID() == 0x000021)
                {
                    CastInfoEvent(response.getID().ToString("X6") + " " + response.getData().ToString("X16"), ActivityType.ConvertingFile);
                }
                CastInfoEvent("ECU is reset", ActivityType.ConvertingFile);
            }
        }

        private byte getCanData(ulong m_data, uint a_index)
        {
            return (byte)(m_data >> (int)(a_index * 8));
        }

        public byte[] ReadFlash(int startAddress, int endAddress, float percentage)
        {
            byte[] _data = new byte[endAddress - startAddress];
            //float percentage = 0;
            int retryCount = 0;
            ulong data;
            // assumes SBL is already running!
            // 0x88 
            // 0x90
            // 0xC0
            // 0xB0
            if (canUsbDevice.isOpen())
            {
                //EnterProgrammingMode();
                CANMessage msg;
                CANMessage response;
                //Set start address
                //S FFFFE 7A 9C 00 00 80 00 00 00
                //R 21 7A 9C 00 00 80 00 AA AA

                //send 7A 9C 00 00 80 00 00 00
                //send 7A B4 00 00 80 01 00 00
                bool _ok = true;
                for (int address = startAddress; address < endAddress; address++)
                {
                    _ok = true;
                    msg = new CANMessage(0x000FFFFE, LAWICEL.CANMSG_EXTENDED, 8);
                    msg.setFlags(LAWICEL.CANMSG_EXTENDED);
                    ulong cmd = 0x0000000000009C7A;
                    ulong addressHigh = (uint)address & 0x0000000000FF0000;
                    addressHigh /= 0x10000;
                    ulong addressMiddle = (uint)address & 0x000000000000FF00;
                    addressMiddle /= 0x100;
                    ulong addressLow = (uint)address & 0x00000000000000FF;
                    cmd |= (addressLow * 0x10000000000);
                    cmd |= (addressMiddle * 0x100000000);
                    cmd |= (addressHigh * 0x1000000);
                    msg.setData(cmd);
                    m_canListener.setupWaitMessage(0x000021);
                    canUsbDevice.sendMessage(msg);
                    //Thread.Sleep(0);
                    response = new CANMessage();
                    response = m_canListener.waitMessage(200);
                    data = response.getData();
                    if (response.getID() == 0x000021 && getCanData(data, 1) == 0x9C)
                    {
                        //CastInfoEvent(response.getID().ToString("X6") + " " + response.getData().ToString("X16"), ActivityType.DownloadingFlash);
                       // percentage = (float)(address * 100) / _data.Length;
                        CastInfoEvent(percentage.ToString("F2") + " % done", ActivityType.DownloadingFlash);
                        if (onStatusChanged != null) onStatusChanged(this, new StatusEventArgs("Reading flash...", Convert.ToInt32(percentage), ECUState.CommunicationRunning));
                    }
                    else
                    {
                        _ok = false;
                    }
                    if (_ok)
                    {
                        // now start reading (f.e. 0x100 bytes to try)
                        msg = new CANMessage(0x000FFFFE, LAWICEL.CANMSG_EXTENDED, 8);
                        msg.setFlags(LAWICEL.CANMSG_EXTENDED);
                        cmd = 0x000000000000B47A;
                        int address2Read = address + 1;
                        addressHigh = (uint)address2Read & 0x0000000000FF0000;
                        addressHigh /= 0x10000;
                        addressMiddle = (uint)address2Read & 0x000000000000FF00;
                        addressMiddle /= 0x100;
                        addressLow = (uint)address2Read & 0x00000000000000FF;
                        cmd |= (addressLow * 0x10000000000);
                        cmd |= (addressMiddle * 0x100000000);
                        cmd |= (addressHigh * 0x1000000);
                        msg.setData(cmd);
                        m_canListener.setupWaitMessage(0x000021);
                        canUsbDevice.sendMessage(msg);
                        //Thread.Sleep(0);
                        response = new CANMessage();
                        response = m_canListener.waitMessage(200);
                        data = response.getData();
                        if (response.getID() == 0x000021 && getCanData(data, 1) == 0xB1)
                        {
                            _data[address - startAddress] = getCanData(data, 2);
                            //percentage = (float)(address * 100) / _data.Length;
                            CastInfoEvent(percentage.ToString("F2") + " % done", ActivityType.DownloadingFlash);
                            if (onStatusChanged != null) onStatusChanged(this, new StatusEventArgs("Reading flash...", Convert.ToInt32(percentage), ECUState.CommunicationRunning));
                        }
                        else
                        {
                            _ok = false;
                        }
                    }
                    if (!_ok)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            m_canListener.setupWaitMessage(0x000021);
                            //canUsbDevice.sendMessage(msg);
                            Thread.Sleep(1);
                            response = new CANMessage();
                            response = m_canListener.waitMessage(10);
                        }
                        retryCount++;
                        CastInfoEvent("Retrying address: " + address.ToString("X8"), ActivityType.DownloadingFlash);
                        address--; // redo this address
                        canUsbDevice.Flush();
                        if (!canUsbDevice.isOpen() || retryCount == 50)
                        {
                            return _data;
                        }
                        
                    }
                    else
                    {
                        retryCount = 0;
                    }
                    //float percentage2 = (float)(address * 100) / _data.Length;
                    //CastProgressReadEvent(percentage);
                }
                

            }
            return _data;


        }

        public void LeaveProgrammingMode()
        {
            //CastInfoEvent("Need to leave programming mode here", ActivityType.ConvertingFile);
            Reset();
        }

        public bool EraseFlash()
        {
            bool erased = false;
            if (canUsbDevice.isOpen())
            {
                CANMessage msg;
                CANMessage response;

                //Set start address
                //S FFFFE 7A 9C 00 00 80 00 00 00
                //R 21 7A 9C 00 00 80 00 AA AA
                msg = new CANMessage(0x000FFFFE, LAWICEL.CANMSG_EXTENDED, 8);
                msg.setFlags(LAWICEL.CANMSG_EXTENDED);
                msg.setData(0x0000008000009C7A);
                m_canListener.setupWaitMessage(0x000021);
                canUsbDevice.sendMessage(msg);
                Thread.Sleep(1);
                response = new CANMessage();
                response = m_canListener.waitMessage(500);
                if (response.getID() == 0x000021)
                {
                    CastInfoEvent(response.getID().ToString("X6") + " " + response.getData().ToString("X16"), ActivityType.ConvertingFile);
                }

                CastInfoEvent("Erasing flash...", ActivityType.ErasingFlash);
                if (onStatusChanged != null) onStatusChanged(this, new StatusEventArgs("Erasing flash...", 0, ECUState.CommunicationRunning));

                //Send delete command
                //S FFFFE 7A F8 00 00 00 00 00 00
                //R 21 7A F9 01 00 80 00 AA AA
                //R 21 7A F9 00 00 80 00 AA AA
                
                msg = new CANMessage(0x000FFFFE, LAWICEL.CANMSG_EXTENDED, 8);
                msg.setFlags(LAWICEL.CANMSG_EXTENDED);
                msg.setData(0x000000000000F87A);
                m_canListener.setupWaitMessage(0x000021);
                canUsbDevice.sendMessage(msg);
                Thread.Sleep(1);
                response = new CANMessage();
                
                response = m_canListener.waitMessage(500);
                if (response.getID() == 0x000021)
                {
                    CastInfoEvent(response.getID().ToString("X6") + " " + response.getData().ToString("X16"), ActivityType.ConvertingFile);
                    if (response.getCanData(2) == 0x00 && response.getCanData(1) == 0xF9) erased = true;
                }
                int waitcount = 0;
                while (!erased && waitcount++ < 30)
                {
                    m_canListener.setupWaitMessage(0x000021);
                    response = m_canListener.waitMessage(500);
                    if (response.getID() == 0x000021)
                    {
                        CastInfoEvent(response.getID().ToString("X6") + " " + response.getData().ToString("X16"), ActivityType.ConvertingFile);
                        if (response.getCanData(2) == 0x00 && response.getCanData(1) == 0xF9) erased = true;
                    }
                }
                if (erased)
                {
                    CastInfoEvent("Flash erased", ActivityType.FinishedErasingFlash);
                    if (onStatusChanged != null) onStatusChanged(this, new StatusEventArgs("Erased flash...", 0, ECUState.CommunicationRunning));

                }
                else
                {
                    CastInfoEvent("Failed to erase flash", ActivityType.ConvertingFile);
                    if (onStatusChanged != null) onStatusChanged(this, new StatusEventArgs("Failed to erase flash", 0, ECUState.CommunicationRunning));

                }
            }
            return erased;
        }

        public void SendFlashFile(string filename)
        {
            
            if (canUsbDevice.isOpen())
            {
                CANMessage msg;
                CANMessage response;
                CastInfoEvent("Start sending flash file", ActivityType.StartFlashing);
                if (onStatusChanged != null) onStatusChanged(this, new StatusEventArgs("Flashing...", 0, ECUState.CommunicationRunning));

                //Set start address
                //S FFFFE 7A 9C 00 00 80 00 00 00
                //R 21 7A 9C 00 00 80 00 AA AA
                msg = new CANMessage(0x000FFFFE, LAWICEL.CANMSG_EXTENDED, 8);
                msg.setFlags(LAWICEL.CANMSG_EXTENDED);
                //                      HHHH HHMM MMMM MMLL                                          
                //            0x7A 0x9C 0x00 0x31 0xC0 0x00 0x00 0x00

                //            0x00 0x00 0x00 0xF0 0x0F 0x00 0x9C 0x7A
                msg.setData(0x0000008000009C7A);
                
                m_canListener.setupWaitMessage(0x000021);
                canUsbDevice.sendMessage(msg);
                Thread.Sleep(50);
                response = new CANMessage();
                response = m_canListener.waitMessage(500);
                if (response.getID() == 0x000021)
                {
                    CastInfoEvent(response.getID().ToString("X6") + " " + response.getData().ToString("X16"), ActivityType.ConvertingFile);
                }
                // 6 bytes per message
                FileInfo fi = new FileInfo(filename);
                FileStream fs = new FileStream(filename, FileMode.Open);
                BinaryReader br = new BinaryReader(fs);
                fs.Position = 0x8000;

                // first send 0x7000 bytes (upto 0xe000)
                int numberOfMessages = (int)(0x6000) / 6;
                CastInfoEvent("Need to send " + numberOfMessages.ToString() + " messages for part I", ActivityType.UploadingFlash);
                
                for (int t = 0; t < numberOfMessages; t++)
                {
                    msg = new CANMessage(0x000FFFFE, LAWICEL.CANMSG_EXTENDED, 8);
                    msg.setFlags(LAWICEL.CANMSG_EXTENDED);
                    msg.setData(0x000000000000AE7A);
                    msg.setCanData(br.ReadByte(), 2);
                    msg.setCanData(br.ReadByte(), 3);
                    msg.setCanData(br.ReadByte(), 4);
                    msg.setCanData(br.ReadByte(), 5);
                    msg.setCanData(br.ReadByte(), 6);
                    msg.setCanData(br.ReadByte(), 7);

                    m_canListener.setupWaitMessage(0x000021);
                    canUsbDevice.sendMessage(msg);
                    response = new CANMessage();
                    response = m_canListener.waitMessage(1);
                    if (response.getID() == 0x000021)
                    {
                        if (response.getCanData(1) == 0xA9 && response.getCanData(2) == 0x02)
                        {
                            // failed programming
                            CastInfoEvent("Programming failed", ActivityType.UploadingFlash);
                            break;
                        }
                        CastInfoEvent(response.getID().ToString("X6") + " " + response.getData().ToString("X16"), ActivityType.ConvertingFile);
                    }
                    CastProgressWriteEvent(t * 100 / numberOfMessages);
                }
                int rest = (int)(0x6000) % 6;
                if (rest > 0)
                {
                   // CastInfoEvent("Sending the rest: " + rest.ToString(), ActivityType.UploadingFlash);
                    byte value = (byte)(0xA8 + Convert.ToByte(rest));
                    // send the remainder
                    msg = new CANMessage(0x000FFFFE, LAWICEL.CANMSG_EXTENDED, 8);
                    msg.setFlags(LAWICEL.CANMSG_EXTENDED);
                    msg.setData(0x0000000000007A);
                    msg.setCanData(value, 1);
                    if (rest > 0) msg.setCanData(br.ReadByte(), 2);
                    if (rest > 1) msg.setCanData(br.ReadByte(), 3);
                    if (rest > 2) msg.setCanData(br.ReadByte(), 4);
                    if (rest > 3) msg.setCanData(br.ReadByte(), 5);
                    if (rest > 4) msg.setCanData(br.ReadByte(), 6);
                    if (rest > 5) msg.setCanData(br.ReadByte(), 7);
                    m_canListener.setupWaitMessage(0x000021);
                    canUsbDevice.sendMessage(msg);
                    Thread.Sleep(1);
                    response = new CANMessage();
                }
                int nrBlocksToSend = 0;
                if (fi.Length == 0x100000)
                {
                    // 1 MB file
                    // blocks of 0x1000 bytes -> total = 100 blocks
                    // but we already send upto 0x10000 so 0x0F0000 left = 0xF0 blocks
                    nrBlocksToSend = 0xF0;

                }
                else if (fi.Length == 0x080000)
                {
                    // 512 kB file
                    // 0x70000 left, so 0x70 blocks
                    nrBlocksToSend = 0x70;
                }
                fs.Position = 0x010000;
                for (int blockNumber = 0; blockNumber < nrBlocksToSend; blockNumber++)
                {
                    CastInfoEvent("Sending blocknr: " + blockNumber.ToString() + "/" + nrBlocksToSend.ToString(), ActivityType.UploadingFlash);
                    int startAddress = 0x00010000 + (blockNumber * 0x1000);
                    msg = new CANMessage(0x000FFFFE, LAWICEL.CANMSG_EXTENDED, 8);
                    msg.setFlags(LAWICEL.CANMSG_EXTENDED);
                    ulong cmd = 0x0000000000009C7A;
                    ulong addressHigh = (uint)startAddress & 0x0000000000FF0000;
                    addressHigh /= 0x10000;
                    ulong addressMiddle = (uint)startAddress & 0x000000000000FF00;
                    addressMiddle /= 0x100;
                    ulong addressLow = (uint)startAddress & 0x00000000000000FF;
                    cmd |= (addressLow * 0x10000000000);
                    cmd |= (addressMiddle * 0x100000000);
                    cmd |= (addressHigh * 0x1000000);
                    msg.setData(cmd);
                    m_canListener.setupWaitMessage(0x000021);
                    canUsbDevice.sendMessage(msg);
                    Thread.Sleep(50);
                    response = new CANMessage();
                    response = m_canListener.waitMessage(500);
                    if (response.getID() == 0x000021)
                    {
                        //CastInfoEvent(response.getID().ToString("X6") + " " + response.getData().ToString("X16"), ActivityType.ConvertingFile);
                    }
                    for (int t = 0; t < 0x2AA; t++)
                    {
                        msg = new CANMessage(0x000FFFFE, LAWICEL.CANMSG_EXTENDED, 8);
                        msg.setFlags(LAWICEL.CANMSG_EXTENDED);
                        msg.setData(0x000000000000AE7A);
                        msg.setCanData(br.ReadByte(), 2);
                        msg.setCanData(br.ReadByte(), 3);
                        msg.setCanData(br.ReadByte(), 4);
                        msg.setCanData(br.ReadByte(), 5);
                        msg.setCanData(br.ReadByte(), 6);
                        msg.setCanData(br.ReadByte(), 7);

                        m_canListener.setupWaitMessage(0x000021);
                        canUsbDevice.sendMessage(msg);
                        response = new CANMessage();
                        response = m_canListener.waitMessage(1);
                        if (response.getID() == 0x000021)
                        {
                            if (response.getCanData(1) == 0xA9 && response.getCanData(2) == 0x02)
                            {
                                // failed programming
                                CastInfoEvent("Programming failed", ActivityType.UploadingFlash);
                                break;
                            }
                            CastInfoEvent(response.getID().ToString("X6") + " " + response.getData().ToString("X16"), ActivityType.ConvertingFile);
                        }
                    }
                    // the rest
                    rest = 4;
                    //CastInfoEvent("Sending the rest: " + rest.ToString(), ActivityType.UploadingFlash);
                    byte value = (byte)(0xA8 + Convert.ToByte(rest));
                    // send the remainder
                    msg = new CANMessage(0x000FFFFE, LAWICEL.CANMSG_EXTENDED, 8);
                    msg.setFlags(LAWICEL.CANMSG_EXTENDED);
                    msg.setData(0x0000000000007A);
                    msg.setCanData(value, 1);
                    if (rest > 0) msg.setCanData(br.ReadByte(), 2);
                    if (rest > 1) msg.setCanData(br.ReadByte(), 3);
                    if (rest > 2) msg.setCanData(br.ReadByte(), 4);
                    if (rest > 3) msg.setCanData(br.ReadByte(), 5);
                    if (rest > 4) msg.setCanData(br.ReadByte(), 6);
                    if (rest > 5) msg.setCanData(br.ReadByte(), 7);
                    m_canListener.setupWaitMessage(0x000021);
                    canUsbDevice.sendMessage(msg);
                    Thread.Sleep(1);
                    response = new CANMessage();
                    CastProgressWriteEvent(blockNumber * 100 / nrBlocksToSend);
                    if (onStatusChanged != null) onStatusChanged(this, new StatusEventArgs("Flashing...", blockNumber * 100 / nrBlocksToSend, ECUState.CommunicationRunning));
                }

                /*
                int startAddress = 0x00010000;
                // set start address to 0x10000 (we skipped 0xE000 - 0xFFFF)
                msg = new CANMessage(0x000FFFFE, LAWICEL.CANMSG_EXTENDED, 8);
                msg.setFlags(LAWICEL.CANMSG_EXTENDED);
                msg.setData(0x0000000001009C7A);
                m_canListener.setupWaitMessage(0x000021);
                canUsbDevice.sendMessage(msg);
                Thread.Sleep(50);
                response = new CANMessage();
                response = m_canListener.waitMessage(500);
                if (response.getID() == 0x000021)
                {
                    CastInfoEvent(response.getID().ToString("X6") + " " + response.getData().ToString("X16"), ActivityType.ConvertingFile);
                }
                int lastSyncAddress = 0;
                numberOfMessages = (int)(fi.Length - 0x10000) / 6;
                CastInfoEvent("Need to send " + numberOfMessages.ToString() + " messages for part II", ActivityType.UploadingFlash);
                for (int t = 0; t < numberOfMessages; t++)
                {
                    msg = new CANMessage(0x000FFFFE, LAWICEL.CANMSG_EXTENDED, 8);
                    msg.setFlags(LAWICEL.CANMSG_EXTENDED);
                    msg.setData(0x000000000000AE7A);
                    msg.setCanData(br.ReadByte(), 2);
                    msg.setCanData(br.ReadByte(), 3);
                    msg.setCanData(br.ReadByte(), 4);
                    msg.setCanData(br.ReadByte(), 5);
                    msg.setCanData(br.ReadByte(), 6);
                    msg.setCanData(br.ReadByte(), 7);

                    m_canListener.setupWaitMessage(0x000021);
                    canUsbDevice.sendMessage(msg);
                    response = new CANMessage();
                    response = m_canListener.waitMessage(1);
                    if (response.getID() == 0x000021)
                    {
                        if (response.getCanData(1) == 0xA9 && response.getCanData(2) == 0x02)
                        {
                            // failed programming
                            CastInfoEvent("Programming failed", ActivityType.UploadingFlash);
                            break;
                        }
                        CastInfoEvent(response.getID().ToString("X6") + " " + response.getData().ToString("X16"), ActivityType.ConvertingFile);
                    }
                    // send address every 0x8000 bytes
                    startAddress += 6;
                    
                    if(startAddress % 0x1000 == 0)
                    {
                        //lastSyncAddress = startAddress / 0x1000;
                        msg = new CANMessage(0x000FFFFE, LAWICEL.CANMSG_EXTENDED, 8);
                        msg.setFlags(LAWICEL.CANMSG_EXTENDED);
                        ulong cmd = 0x0000000000009C7A; 
                        ulong addressHigh = (uint)startAddress & 0x0000000000FF0000;
                        addressHigh /= 0x10000;
                        ulong addressMiddle = (uint)startAddress & 0x000000000000FF00;
                        addressMiddle /= 0x100;
                        ulong addressLow = (uint)startAddress & 0x00000000000000FF;
                        cmd |= (addressLow * 0x10000000000);
                        cmd |= (addressMiddle * 0x100000000);
                        cmd |= (addressHigh * 0x1000000);
                        msg.setData(cmd);
                        m_canListener.setupWaitMessage(0x000021);
                        canUsbDevice.sendMessage(msg);
                       
                        response = new CANMessage();
                        response = m_canListener.waitMessage(500);
                        if (response.getID() == 0x000021)
                        {
                            CastInfoEvent(response.getID().ToString("X6") + " " + response.getData().ToString("X16"), ActivityType.ConvertingFile);
                        }
                        Thread.Sleep(50);
                    }
                    CastProgressWriteEvent(t * 100 / numberOfMessages);
                }*/
                br.Close();
                fs.Close();
                fs.Dispose();
                CastInfoEvent("Finished sending new flashfile", ActivityType.FinishedFlashing);
                if (onStatusChanged != null) onStatusChanged(this, new StatusEventArgs("Flashing done", 100, ECUState.CommunicationRunning));

            }
        }

        public void StartSBL()
        {
            //Set start address
            //S FFFFE 7A 9C 00 31 C0 00 00 00
            //R 21 7A 9C 00 31 C0 00 AA AA
            if (canUsbDevice.isOpen())
            {
                CANMessage msg;
                CANMessage response;

                // set start address
                //S FFFFE 7A 9C 00 31 C0 00 00 00
                //R 21 7A 9C 00 31 C0 00 AA AA
                msg = new CANMessage(0x000FFFFE, LAWICEL.CANMSG_EXTENDED, 8);
                msg.setFlags(LAWICEL.CANMSG_EXTENDED);
                msg.setData(0x000000C031009C7A);
                m_canListener.setupWaitMessage(0x000021);
                canUsbDevice.sendMessage(msg);
                Thread.Sleep(1);
                response = new CANMessage();
                response = m_canListener.waitMessage(500);
                if (response.getID() == 0x000021)
                {
                    CastInfoEvent(response.getID().ToString("X6") + " " + response.getData().ToString("X16"), ActivityType.ConvertingFile);
                }
                //Jump to start address
                //S FFFFE 7A A0 00 00 00 00 00 00
                //R 21 7A A0 00 31 C0 00 AA AA
                msg = new CANMessage(0x000FFFFE, LAWICEL.CANMSG_EXTENDED, 8);
                msg.setFlags(LAWICEL.CANMSG_EXTENDED);
                msg.setData(0x000000000000A07A);
                m_canListener.setupWaitMessage(0x000021);
                canUsbDevice.sendMessage(msg);
                Thread.Sleep(1);
                response = new CANMessage();
                response = m_canListener.waitMessage(500);
                if (response.getID() == 0x000021)
                {
                    CastInfoEvent(response.getID().ToString("X6") + " " + response.getData().ToString("X16"), ActivityType.ConvertingFile);
                    CastInfoEvent("Secondary bootloader is now running...", ActivityType.ConvertingFile);
                }
            }

           

        }

        public bool SendSBL()
        {
            bool retval = true;
            if (canUsbDevice.isOpen())
            {
                CANMessage msg;
                CANMessage response;

                // set start address
                //S FFFFE 7A 9C 00 31 C0 00 00 00
                //R 21 7A 9C 00 31 C0 00 AA AA
                msg = new CANMessage(0x000FFFFE, LAWICEL.CANMSG_EXTENDED, 8);
                msg.setFlags(LAWICEL.CANMSG_EXTENDED);
                msg.setData(0x000000C031009C7A);
                m_canListener.setupWaitMessage(0x000021);
                canUsbDevice.sendMessage(msg);
                Thread.Sleep(10);
                response = new CANMessage();
                /*response = m_canListener.waitMessage(500);
                if (response.getID() == 0x000021)
                {
                    CastInfoEvent(response.getID().ToString("X6") + " " + response.getData().ToString("X16"), ActivityType.ConvertingFile);
                }*/

                CastInfoEvent("Start sending Secondary bootloader", ActivityType.ConvertingFile);
                if (onStatusChanged != null) onStatusChanged(this, new StatusEventArgs("Uploading bootloader", 0, ECUState.CommunicationRunning));

                ME7SBL sbl = new ME7SBL();
                byte[] SBLBytes = sbl.getSBL();
                //5788 bytes
                int idx = 0;
                for (int t = 0; t < 964; t++)
                {


                    msg = new CANMessage(0x000FFFFE, LAWICEL.CANMSG_EXTENDED, 8);
                    msg.setFlags(LAWICEL.CANMSG_EXTENDED);
                    msg.setData(0x000000000000AE7A);
                    msg.setCanData(SBLBytes[idx++], 2);
                    msg.setCanData(SBLBytes[idx++], 3);
                    msg.setCanData(SBLBytes[idx++], 4);
                    msg.setCanData(SBLBytes[idx++], 5);
                    msg.setCanData(SBLBytes[idx++], 6);
                    msg.setCanData(SBLBytes[idx++], 7);

                    m_canListener.setupWaitMessage(0x000021);
                    canUsbDevice.sendMessage(msg);
                    //Thread.Sleep(1);
                    response = new CANMessage();
                    response = m_canListener.waitMessage(1);
                    if (response.getID() == 0x000021 && response.getCanData(1) == 0xA9 && response.getCanData(2) == 0x02)
                    {
                        // failed programming
                        CastInfoEvent(response.getID().ToString("X6") + " " + response.getData().ToString("X16"), ActivityType.ConvertingFile);
                        CastInfoEvent("Uploading SBL failed", ActivityType.UploadingFlash);
                        retval = false;
                        break;
                    }
                }
                msg = new CANMessage(0x000FFFFE, LAWICEL.CANMSG_EXTENDED, 8);
                msg.setFlags(LAWICEL.CANMSG_EXTENDED);
                msg.setData(0x0000000000AC7A);
                msg.setCanData(SBLBytes[idx++], 2);
                msg.setCanData(SBLBytes[idx++], 3);
                msg.setCanData(SBLBytes[idx++], 4);
                msg.setCanData(SBLBytes[idx++], 5);
                msg.setCanData(0x00, 6);
                msg.setCanData(0x00, 7);
                m_canListener.setupWaitMessage(0x000021);
                canUsbDevice.sendMessage(msg);
                Thread.Sleep(1);
                response = new CANMessage();
                response = m_canListener.waitMessage(500);
                if (response.getID() == 0x000021)
                {
                    CastInfoEvent(response.getID().ToString("X6") + " " + response.getData().ToString("X16"), ActivityType.ConvertingFile);
                }
                CastInfoEvent("Finished sending Secondary bootloader", ActivityType.ConvertingFile);
                if (onStatusChanged != null) onStatusChanged(this, new StatusEventArgs("Bootloader uploaded", 0, ECUState.CommunicationRunning));

            }
            else
            {
                retval = false;
            }
            return retval;
        }

        public bool SendSBLDownload()
        {
            bool retval = true;
            if (canUsbDevice.isOpen())
            {
                CANMessage msg;
                CANMessage response;

                // set start address
                //S FFFFE 7A 9C 00 31 C0 00 00 00
                //R 21 7A 9C 00 31 C0 00 AA AA
                msg = new CANMessage(0x000FFFFE, LAWICEL.CANMSG_EXTENDED, 8);
                msg.setFlags(LAWICEL.CANMSG_EXTENDED);
                msg.setData(0x000000C031009C7A);
                m_canListener.setupWaitMessage(0x000021);
                canUsbDevice.sendMessage(msg);
                Thread.Sleep(10);
                response = new CANMessage();
                /*response = m_canListener.waitMessage(500);
                if (response.getID() == 0x000021)
                {
                    CastInfoEvent(response.getID().ToString("X6") + " " + response.getData().ToString("X16"), ActivityType.ConvertingFile);
                }*/

                CastInfoEvent("Start sending download bootloader", ActivityType.ConvertingFile);
                if (onStatusChanged != null) onStatusChanged(this, new StatusEventArgs("Uploading bootloader", 0, ECUState.CommunicationRunning));

                ME7SBL sbl = new ME7SBL();
                byte[] SBLBytes = sbl.getSBLDownload();
                //5788 bytes
                int idx = 0;
                for (int t = 0; t < 964; t++)
                {


                    msg = new CANMessage(0x000FFFFE, LAWICEL.CANMSG_EXTENDED, 8);
                    msg.setFlags(LAWICEL.CANMSG_EXTENDED);
                    msg.setData(0x000000000000AE7A);
                    msg.setCanData(SBLBytes[idx++], 2);
                    msg.setCanData(SBLBytes[idx++], 3);
                    msg.setCanData(SBLBytes[idx++], 4);
                    msg.setCanData(SBLBytes[idx++], 5);
                    msg.setCanData(SBLBytes[idx++], 6);
                    msg.setCanData(SBLBytes[idx++], 7);

                    m_canListener.setupWaitMessage(0x000021);
                    canUsbDevice.sendMessage(msg);
                    //Thread.Sleep(1);
                    response = new CANMessage();
                    response = m_canListener.waitMessage(1);
                    if (response.getID() == 0x000021 && response.getCanData(1) == 0xA9 && response.getCanData(2) == 0x02)
                    {
                        // failed programming
                        CastInfoEvent(response.getID().ToString("X6") + " " + response.getData().ToString("X16"), ActivityType.ConvertingFile);
                        CastInfoEvent("Uploading SBLDownload failed", ActivityType.UploadingFlash);
                        retval = false;
                        break;
                    }
                }
                msg = new CANMessage(0x000FFFFE, LAWICEL.CANMSG_EXTENDED, 8);
                msg.setFlags(LAWICEL.CANMSG_EXTENDED);
                msg.setData(0x0000000000AC7A);
                msg.setCanData(SBLBytes[idx++], 2);
                msg.setCanData(SBLBytes[idx++], 3);
                msg.setCanData(SBLBytes[idx++], 4);
                msg.setCanData(SBLBytes[idx++], 5);
                msg.setCanData(0x00, 6);
                msg.setCanData(0x00, 7);
                m_canListener.setupWaitMessage(0x000021);
                canUsbDevice.sendMessage(msg);
                Thread.Sleep(1);
                response = new CANMessage();
                response = m_canListener.waitMessage(500);
                if (response.getID() == 0x000021)
                {
                    CastInfoEvent(response.getID().ToString("X6") + " " + response.getData().ToString("X16"), ActivityType.ConvertingFile);
                }
                CastInfoEvent("Finished sending download bootloader", ActivityType.ConvertingFile);
                if (onStatusChanged != null) onStatusChanged(this, new StatusEventArgs("Bootloader uploaded", 0, ECUState.CommunicationRunning));

            }
            else
            {
                retval = false;
            }
            return retval;
        }




        /*
-> E id = 000FFFFE len = 8 data =ff 86 00 00 00 00 00 00 Resend until ECU shuts up and enters programming mode. 

-> E id = 000FFFFE len = 8 data =7a 90 00 00 00 00 00 00 Read serial number 
<- E id = 00000021 len = 8 data =7a 96 00 00 00 10 32 22 

-> E id = 000FFFFE len = 8 data =7a 88 00 00 00 00 00 00 Read hardware number 
<- E id = 00000021 len = 8 data =7a 8e 00 00 09 47 07 38 

-> E id = 000FFFFE len = 8 data =7a c8 00 00 00 00 00 00 Reset to normal mode 

Currently untested commands or commands with unknown response: 
C0 - starts the specified ECUs primary boot loader 
9C - specified base address where the action should take place 
F8 - erase the memory block 
B4 - commit up to the specified address 
A0 - used to jump to another subroutine address



In ME7 PBL mode software can be uploaded to address 31C000 and run.
This software can be Volvo SBL to allow erase and write commands on flash address area,
or homemade software to do whatever you like.
 * */
        public void EnterProgrammingMode()
        {
            
            if (canUsbDevice.isOpen())
            {
                CANMessage msg;
                CANMessage response;
                CastInfoEvent("Starting primary bootloader...", ActivityType.ConvertingFile);
                if (onStatusChanged != null) onStatusChanged(this, new StatusEventArgs("Entering programming mode", 0, ECUState.CommunicationRunning));

                for (int i = 0; i < 1000; i++)
                {
                    msg = new CANMessage(0x000FFFFE, LAWICEL.CANMSG_EXTENDED, 8);
                    msg.setFlags(LAWICEL.CANMSG_EXTENDED);
                    msg.setData(0x000000000086FF);
                    canUsbDevice.sendMessage(msg);
                    Thread.Sleep(0);
                }
                //CastInfoEvent("Primary bootloader should be running", ActivityType.ConvertingFile);

                /*msg = new CANMessage(0x000FFFFE, LAWICEL.CANMSG_EXTENDED, 8);
                msg.setFlags(LAWICEL.CANMSG_EXTENDED);
                msg.setData(0x0000000000907A);
                m_canListener.setupWaitMessage(0x000021);
                if (!canUsbDevice.sendMessage(msg))
                {
                    AddToCanTrace("Send failed to send");
                }
                response = new CANMessage();
                response = m_canListener.waitMessage(500);
                if (response.getID() == 0x000021)
                {
                    CastInfoEvent(response.getID().ToString("X6") + " " + response.getData().ToString("X16"), ActivityType.ConvertingFile);
                }

                msg = new CANMessage(0x000FFFFE, LAWICEL.CANMSG_EXTENDED, 8);
                msg.setFlags(LAWICEL.CANMSG_EXTENDED);
                msg.setData(0x0000000000887A);
                m_canListener.setupWaitMessage(0x000021);
                if (!canUsbDevice.sendMessage(msg))
                {
                    AddToCanTrace("Send failed to send");
                }
                response = new CANMessage();
                response = m_canListener.waitMessage(500);
                if (response.getID() == 0x000021)
                {
                    CastInfoEvent(response.getID().ToString("X6") + " " + response.getData().ToString("X16"), ActivityType.ConvertingFile);
                }*/

            }
        }

        public enum ME7LiveDataType : int
        {
            BatteryVoltage
        }

        public byte ReadLiveData(ME7LiveDataType type, bool wakeup)
        {
            byte retval = 0;
            CANMessage msg;
            CANMessage response;
            msg = new CANMessage(0x000FFFFE, LAWICEL.CANMSG_EXTENDED, 8);
            msg.setFlags(LAWICEL.CANMSG_EXTENDED);

            switch (type)
            {
                case ME7LiveDataType.BatteryVoltage:
                    msg.setData(0x00010A10A67ACD);
                    //A6 10 0A 01
                    break;

            }
            m_canListener.setupWaitMessage(0x800021);
            if (wakeup) canUsbDevice.sendMessage(msg);
            canUsbDevice.sendMessage(msg);
            response = m_canListener.waitMessage(50);
            if (response.getID() == 0x800021)
            {
                //CastInfoEvent(response.getID().ToString("X6") + " " + response.getData().ToString("X16"), ActivityType.ConvertingFile);
                retval = response.getCanData(5);
            }
            return retval;

        }


        public void TestComm()
        {
            //000FFFFE len = 8 data =cb 7a b9 f0 00 00 00 00
            if (canUsbDevice.isOpen())
            {
                CANMessage msg = new CANMessage(0x000FFFFE, LAWICEL.CANMSG_EXTENDED, 8);
                msg.setFlags(LAWICEL.CANMSG_EXTENDED);
                //msg.setData(0x000000F0B97ACB);
                msg.setData(0x0000000000907A);
                //7a 90
                if (!canUsbDevice.sendMessage(msg))
                {
                    AddToCanTrace("Send failed to send");
                }
                else
                {
                    CastInfoEvent("Send message ok", ActivityType.ConvertingFile);

                }
                /*msg = new CANMessage(0x000FFFFE, 0, 8);
                msg.setData(0xCB7AB9F000000000);
                if (canUsbDevice.sendMessage(msg))
                {
                    AddToCanTrace("Send OK 2");
                }*/
            }
        }

        public override void setCANDevice(string adapterType)
        {
            if (canUsbDevice == null)
            {
                if (adapterType == "LAWICEL")
                {
                    canUsbDevice = new CANUSBDevice();
                }
                else if (adapterType == "ELM327")
                {
                    canUsbDevice = new CANELM327Device();
                    canUsbDevice.ForcedComport = m_forcedComport;
                }
                else
                {
                    canUsbDevice = new LPCCANDevice_ME7();
                }
                canUsbDevice.EnableCanLog = m_EnableCanLog;
                canUsbDevice.onReceivedAdditionalInformation += new ICANDevice.ReceivedAdditionalInformation(canUsbDevice_onReceivedAdditionalInformation);
                //canUsbDevice.onReceivedAdditionalInformationFrame += new ICANDevice.ReceivedAdditionalInformationFrame(canUsbDevice_onReceivedAdditionalInformationFrame);
                canUsbDevice.UseOnlyPBus = true;
                if (m_canListener == null)
                {
                    m_canListener = new CANListener();
                }
                canUsbDevice.addListener(m_canListener);
            }

        }

        void canUsbDevice_onReceivedAdditionalInformation(object sender, ICANDevice.InformationEventArgs e)
        {
            CastCounterEvent(e.RxCount, e.TxCount, e.ErrCount);
        }

        /// <summary>
        /// Cleans up connections and resources in use by the T5CANLib DLL
        /// </summary>
        public void Cleanup()
        {
            try
            {
                tmr.Stop();
                tmrSpeed.Stop();
                MM_EndPeriod(1);
                Console.WriteLine("Cleanup called in ME7Communication");
                //m_canDevice.removeListener(m_canListener);
                m_canListener.FlushQueue();
                canUsbDevice.close();
                Console.WriteLine("Closed m_canDevice in ME7Communication");

            }
            catch (Exception E)
            {
                Console.WriteLine(E.Message);
            }
        }

        public float GetADCValue(uint channel)
        {
            return canUsbDevice.GetADCValue(channel);
        }

        public float GetThermoValue()
        {
            return canUsbDevice.GetThermoValue();
        }

        public override event ICommunication.DTCInfo onDTCInfo;
        public override event ICommunication.ECUInfo onECUInfo;
        public override event ICommunication.StatusChanged onStatusChanged;

        public override byte[] readSRAM(int address, int bytestoread, int timeout, out bool success)
        {
            success = false;
            return new byte[1];
        }
        private byte[] ReadFlashCE7A(int address, out bool success)
        {
            byte[] _data = new byte[0x10];
            int _dataidx = 0;
            float percentage = 0;
            ulong data;
            success = false;
            if (canUsbDevice.isOpen())
            {
                CANMessage msg;
                CANMessage response;
                bool _ok = true;
                _ok = true;
                ulong cmd;
                msg = new CANMessage(0x000FFFFE, LAWICEL.CANMSG_EXTENDED, 8);
                msg.setFlags(LAWICEL.CANMSG_EXTENDED);
                cmd = 0x0010000000BB7ACE;
                //ce 7a bb 32 ff f0 10 00
                ulong addressHigh = (uint)address & 0x0000000000FF0000;
                addressHigh /= 0x10000;
                ulong addressMiddle = (uint)address & 0x000000000000FF00;
                addressMiddle /= 0x100;
                ulong addressLow = (uint)address & 0x00000000000000FF;
                cmd |= (addressLow * 0x10000000000);
                cmd |= (addressMiddle * 0x100000000);
                cmd |= (addressHigh * 0x1000000);
                msg.setData(cmd);
                m_canListener.setupWaitMessage(0x00800021);
                canUsbDevice.sendMessage(msg);
                Thread.Sleep(1);
                response = new CANMessage();
                response = m_canListener.waitMessage(500);
                data = response.getData();
                if (response.getID() == 0x00800021 && getCanData(data, 1) == 0x7A)
                {
                    percentage = (float)(address * 100) / _data.Length;
                    CastInfoEvent(percentage.ToString("F2") + " % done", ActivityType.DownloadingFlash);
                    _data[_dataidx++] = getCanData(data, 6);
                    _data[_dataidx++] = getCanData(data, 7);
                }
                else
                {
                    CastInfoEvent("id: " + response.getID(), ActivityType.DownloadingFlash);
                    Console.WriteLine("id: " + response.getID());
                    _ok = false;
                }
                if (_ok)
                {
                    m_canListener.setupWaitMessage(0x00800021);
                    Thread.Sleep(1);
                    response = new CANMessage();
                    response = m_canListener.waitMessage(500);
                    data = response.getData();
                    if (response.getID() == 0x00800021)
                    {
                        success = true;
                        _data[_dataidx++] = getCanData(data, 2);
                        _data[_dataidx++] = getCanData(data, 3);
                        _data[_dataidx++] = getCanData(data, 4);
                        _data[_dataidx++] = getCanData(data, 5);
                        _data[_dataidx++] = getCanData(data, 6);
                        _data[_dataidx++] = getCanData(data, 7);
                        percentage = (float)(address * 100) / _data.Length;
                        CastInfoEvent(percentage.ToString("F2") + " % done", ActivityType.DownloadingFlash);
                        //if (onStatusChanged != null) onStatusChanged(this, new StatusEventArgs("Reading flash...", Convert.ToInt32(percentage), ECUState.CommunicationRunning));
                    }
                    else
                    {
                        _ok = false;
                    }
                    m_canListener.setupWaitMessage(0x00800021);
                    Thread.Sleep(1);
                    response = new CANMessage();
                    response = m_canListener.waitMessage(500);
                    data = response.getData();
                    if (response.getID() == 0x00800021)
                    {
                        success = true;
                        _data[_dataidx++] = getCanData(data, 2);
                        _data[_dataidx++] = getCanData(data, 3);
                        _data[_dataidx++] = getCanData(data, 4);
                        _data[_dataidx++] = getCanData(data, 5);
                        _data[_dataidx++] = getCanData(data, 6);
                        _data[_dataidx++] = getCanData(data, 7);
                        percentage = (float)(address * 100) / _data.Length;
                        CastInfoEvent(percentage.ToString("F2") + " % done", ActivityType.DownloadingFlash);
                        //if (onStatusChanged != null) onStatusChanged(this, new StatusEventArgs("Reading flash...", Convert.ToInt32(percentage), ECUState.CommunicationRunning));
                    }
                    else
                    {
                        _ok = false;
                    }
                }

                //float percentage2 = (float)(address * 100) / _data.Length;
                //CastProgressReadEvent(percentage);
            }
            return _data;
        }
     /*
-> E  id = 000FFFFE timestamp = 00000000 len = 8 data =ce 7a bb 32 ff f0 10 00 (Read 10(16) bytes from 32fff0)
<- E  id = 00800021 timestamp = 016A9E14 len = 8 data =8f 7a fb 32 ff f0 ae 1b 
<- E  id = 00800021 timestamp = 016A9E23 len = 8 data =09 db 00 de 1b db 00 7e 
<- E  id = 00800021 timestamp = 016A9E33 len = 8 data =4f 23 db 00 44 44 44 44 
In flash:
02FFF0: ae 1b db 00 de 1b db 00 7e 23 db 00 44 44 44 44
      * */
        public override void ReadEprom(string filename, int timeout)
        {
            CastInfoEvent("Read Eprom", ActivityType.DownloadingFlash);
            /*if (onECUInfo != null) onECUInfo(this, new ECUInfoEventArgs("Reading flash data... stand by", 0));
            int MaxBlocks = 0xE000;
            int startAddress = 0x320000;
            for (int iBlock = 0; iBlock < MaxBlocks; iBlock++)
            {
                bool success = false;
                byte[] _smalldata = ReadFlashCE7A(startAddress, out success);

                if (success)
                {
                    Console.WriteLine("read data: " + _smalldata[0].ToString("X2"));
                    CastInfoEvent("read data: " + _smalldata[0].ToString("X2"), ActivityType.DownloadingFlash);
                }
                else
                {
                    CastInfoEvent("no data " + iBlock.ToString(), ActivityType.DownloadingFlash);
                }
            }
            if (onECUInfo != null) onECUInfo(this, new ECUInfoEventArgs("Finished reading flash data... ", 0));
            */
            
            EnterProgrammingMode();
            if (SendSBLDownload())
            {
                StartSBL();
                Thread.Sleep(500);
                byte[] _data = new byte[0x100000];
                if (onECUInfo != null) onECUInfo(this, new ECUInfoEventArgs("Reading flash data... stand by", 0));
                int MaxBlocks = 0x2AAA;// was 2AAAA
                int idx = 0;
                CANMessage msg;
                CANMessage response;
                //Set start address
                //S FFFFE 7A 9C 00 00 80 00 00 00
                //R 21 7A 9C 00 00 80 00 AA AA

                for (int iBlock = 0; iBlock < MaxBlocks; iBlock++)
                {
                    //AddToLogItem("Reading flash data... stand by " + iBlock.ToString() + "/256");
                    int startAddress = (0x0 + iBlock * 0x06);
                    //int endAddress =  (iBlock * 0x06) + 0x06;
                    bool success = false;
                    //Thread.Sleep(1);
                    byte[] _smalldata = ReadFlash88(startAddress, out success);
                    if (success)
                    {
                        _data[idx++] = _smalldata[0];
                        _data[idx++] = _smalldata[1];
                        _data[idx++] = _smalldata[2];
                        _data[idx++] = _smalldata[3];
                        _data[idx++] = _smalldata[4];
                        _data[idx++] = _smalldata[5];
                        Thread.Sleep(1);
                        string strDebug = startAddress.ToString("X8") + " - " + _smalldata[0].ToString("X2") + " "
                            + _smalldata[1].ToString("X2") + " "
                            + _smalldata[2].ToString("X2") + " "
                            + _smalldata[3].ToString("X2") + " "
                            + _smalldata[4].ToString("X2") + " "
                            + _smalldata[5].ToString("X2");
                        Console.WriteLine(strDebug);
                        float percentage = (float)iBlock * 100 / MaxBlocks;
                        //CastInfoEvent("Reading flash... " + percentage.ToString("F2") + " %", ActivityType.DownloadingFlash);
                        CastProgressReadEvent(percentage);
                        if (onStatusChanged != null) onStatusChanged(this, new StatusEventArgs("Reading flash", Convert.ToInt32(percentage), ECUState.CommunicationRunning));
                    }
                    else
                    {
                        iBlock--;
                        Console.WriteLine("Retrying block: " + iBlock.ToString());
                        CastInfoEvent("Retrying block: " + iBlock.ToString(), ActivityType.ConvertingFile);
                    }
                }
                File.WriteAllBytes(filename, _data);

                Console.WriteLine("Leaving secondary bootloader");
                LeaveProgrammingMode();
            }
            else
            {
                onECUInfo(this, new ECUInfoEventArgs("Failed to upload secondary bootloader, please reset ECU", 0));
            }
        }

         private byte[] ReadFlash88(int address, out bool success)
         {
             byte[] _data = new byte[6];
             float percentage = 0;
             ulong data;
             success = false;
             // assumes SBL is already running!
             if (canUsbDevice.isOpen())
             {
                 //Thread.Sleep(1);

                 CANMessage msg;
                 CANMessage response;
                 //Set start address
                 //S FFFFE 7A 9C 00 00 80 00 00 00
                 //R 21 7A 9C 00 00 80 00 AA AA
                 bool _ok = true;
                 _ok = true;
                 ulong cmd;
                 msg = new CANMessage(0x000FFFFE, LAWICEL.CANMSG_EXTENDED, 8);
                 msg.setFlags(LAWICEL.CANMSG_EXTENDED);
                 cmd = 0x0000000000009C7A;
                 ulong addressHigh = (uint)address & 0x0000000000FF0000;
                 addressHigh /= 0x10000;
                 ulong addressMiddle = (uint)address & 0x000000000000FF00;
                 addressMiddle /= 0x100;
                 ulong addressLow = (uint)address & 0x00000000000000FF;
                 cmd |= (addressLow * 0x10000000000);
                 cmd |= (addressMiddle * 0x100000000);
                 cmd |= (addressHigh * 0x1000000);
                 msg.setData(cmd);
                 m_canListener.setupWaitMessage(0x000021);
                 canUsbDevice.sendMessage(msg);
                 Thread.Sleep(1);
                 response = new CANMessage();
                 response = m_canListener.waitMessage(500);
                 data = response.getData();
                 if (response.getID() == 0x000021 && getCanData(data, 1) == 0x9C)
                 {
                     //CastInfoEvent(response.getID().ToString("X6") + " " + response.getData().ToString("X16"), ActivityType.DownloadingFlash);
                     percentage = (float)(address * 100) / _data.Length;
                     CastInfoEvent(percentage.ToString("F2") + " % done", ActivityType.DownloadingFlash);
                    // if (onStatusChanged != null) onStatusChanged(this, new StatusEventArgs("Reading flash...", Convert.ToInt32(percentage), ECUState.CommunicationRunning));
                 }
                 else
                 {
                     _ok = false;
                 }
                if (_ok)
                {
                    // now start reading (f.e. 0x100 bytes to try)
                    msg = new CANMessage(0x000FFFFE, LAWICEL.CANMSG_EXTENDED, 8);
                    msg.setFlags(LAWICEL.CANMSG_EXTENDED);
                    cmd = 0x000000000000887A;
                    msg.setData(cmd);
                    m_canListener.setupWaitMessage(0x000021);
                    canUsbDevice.sendMessage(msg);
                    Thread.Sleep(1);
                    response = new CANMessage();
                    response = m_canListener.waitMessage(500);
                    data = response.getData();
                    if (response.getID() == 0x000021 && getCanData(data, 1) == 0x8D)
                    {
                        success = true;
                        _data[0] = getCanData(data, 2);
                        _data[1] = getCanData(data, 3);
                        _data[2] = getCanData(data, 4);
                        _data[3] = getCanData(data, 5);
                        _data[4] = getCanData(data, 6);
                        _data[5] = getCanData(data, 7);
                        percentage = (float)(address * 100) / _data.Length;
                        
                        CastInfoEvent(percentage.ToString("F2") + " % done", ActivityType.DownloadingFlash);
                        //if (onStatusChanged != null) onStatusChanged(this, new StatusEventArgs("Reading flash...", Convert.ToInt32(percentage), ECUState.CommunicationRunning));
                    }
                    else
                    {
                        _ok = false;
                    }
                }
                
                //float percentage2 = (float)(address * 100) / _data.Length;
                //CastProgressReadEvent(percentage);


            }
            return _data;
        }

        public override void ReadDTCCodes(int timeout)
        {
            // TODO: Implement    
        }

        public override string LogFolder
        {
            get
            {
                return "";
            }
            set
            {
                
            }
        }

        public override bool IsWaitingForResponse
        {
            get
            {
                return false;
            }
            set
            {
                
            }
        }

        public override bool EnableLogging
        {
            get
            {
                return canUsbDevice.EnableCanLog;
            }
            set
            {
                canUsbDevice.EnableCanLog = value;
            }
        }

        public override bool CommunicationRunning
        {
            get
            {
                if (canUsbDevice == null) return false;
                return canUsbDevice.isOpen();
            }
            set
            {
                
            }
        }

        public override void ClearDTC(int timeout)
        {
            //TODO: Implement
        }

        public override void StopCommunication()
        {
            Cleanup();
        }

        public override void StartCommunication(string comportnumber, bool HighSpeed)
        {
            if (canUsbDevice.isOpen()) return;
            openDevice(false, HighSpeed);
        }

        public bool openDevice(bool requestSecurityAccess, bool is500KB)
        {
            CastInfoEvent("Open called in ME7Communication", ActivityType.ConvertingFile);
            MM_BeginPeriod(1);
            if (canUsbDevice.open(is500KB) != OpenResult.OK)
            {
                CastInfoEvent("Open failed in ME7Communication", ActivityType.ConvertingFile);
                canUsbDevice.close();
                MM_EndPeriod(1);
                return false;
            }
            // read some data ... 
            /*for (int i = 0; i < 10; i++)
            {
                CANMessage response = new CANMessage();
                response = m_canListener.waitMessage(50);
            }*/

            if (requestSecurityAccess)
            {
                CastInfoEvent("Open succeeded in ME7Communication", ActivityType.ConvertingFile);
                InitializeSession();
                CastInfoEvent("Session initialized", ActivityType.ConvertingFile);
                // read some data ... 
                /*for (int i = 0; i < 10; i++)
                {
                    CANMessage response = new CANMessage();
                    response = m_canListener.waitMessage(50);
                }*/
                bool _securityAccessOk = false;
                for (int i = 0; i < 3; i++)
                {
                    if (RequestSecurityAccess(0))
                    {
                        _securityAccessOk = true;
                        tmr.Start();
                        Console.WriteLine("Timer started");
                        break;
                    }
                }
                if (!_securityAccessOk)
                {
                    CastInfoEvent("Failed to get security access", ActivityType.ConvertingFile);
                    canUsbDevice.close();
                    MM_EndPeriod(1);
                    return false;
                }
                CastInfoEvent("Open successful", ActivityType.ConvertingFile);
            }
            tmrSpeed.Start();
            Console.WriteLine(canUsbDevice.getVersion());
            return true;
        }

        private bool RequestSecurityAccess(int level)
        {
            return false;
        }

        private void InitializeSession()
        {
            //TODO: Implement 
        }

        public class CanInfoEventArgs : System.EventArgs
        {
            private ActivityType _type;

            public ActivityType Type
            {
                get { return _type; }
                set { _type = value; }
            }

            private string _info;

            public string Info
            {
                get { return _info; }
                set { _info = value; }
            }

            public CanInfoEventArgs(string info, ActivityType type)
            {
                this._info = info;
                this._type = type;
            }
        }


        public class CanbusLoadEventArgs : System.EventArgs
        {

            private float _load;

            public float Load
            {
                get { return _load; }
                set { _load = value; }
            }

            public CanbusLoadEventArgs(float load)
            {
                this._load = load;
            }
        }

        public class CanCountEventArgs : System.EventArgs
        {

            private int _rxCount;

            public int RxCount
            {
                get { return _rxCount; }
                set { _rxCount = value; }
            }
            private int _txCount;

            public int TxCount
            {
                get { return _txCount; }
                set { _txCount = value; }
            }
            private int _errCount;

            public int ErrCount
            {
                get { return _errCount; }
                set { _errCount = value; }
            }

            public CanCountEventArgs(int rxcount, int txcount, int errcount)
            {
                this._rxCount = rxcount;
                this._txCount = txcount;
                this._errCount = errcount;
            }
        }

        public class WriteProgressEventArgs : System.EventArgs
        {
            private float _percentage;

            private int _bytestowrite;

            public int Bytestowrite
            {
                get { return _bytestowrite; }
                set { _bytestowrite = value; }
            }

            private int _byteswritten;

            public int Byteswritten
            {
                get { return _byteswritten; }
                set { _byteswritten = value; }
            }

            public float Percentage
            {
                get { return _percentage; }
                set { _percentage = value; }
            }

            public WriteProgressEventArgs(float percentage)
            {
                this._percentage = percentage;
            }

            public WriteProgressEventArgs(float percentage, int bytestowrite, int byteswritten)
            {
                this._bytestowrite = bytestowrite;
                this._byteswritten = byteswritten;
                this._percentage = percentage;
            }
        }

        public class ReadProgressEventArgs : System.EventArgs
        {
            private float _percentage;

            public float Percentage
            {
                get { return _percentage; }
                set { _percentage = value; }
            }

            public ReadProgressEventArgs(float percentage)
            {
                this._percentage = percentage;
            }
        }

        public void RequestECUInfoForTest()
        {
            //-> E id = 000FFFFE len = 8 data =cb 7a b9 f0 00 00 00 00
            //<- E id = 00800021 len = 8 data =8f 7a f9 f0 00 09 47 07
            //<- E id = 00800021 len = 8 data =09 38 20 20 41 09 49 78
            //<- E id = 00800021 len = 8 data =4c 03 20 20 41 00 00 00
            //000FFFFE len = 8 data =cb 7a b9 f0 00 00 00 00
            if (canUsbDevice.isOpen())
            {
                CANMessage msg = new CANMessage(0x000FFFFE, LAWICEL.CANMSG_EXTENDED, 8);
                msg.setFlags(LAWICEL.CANMSG_EXTENDED);
                msg.setData(0x000000F0B97ACB);
                //msg.setData(0xCB7AB9F000000000);
                //msg.setData(0x0000010A10A67ACD);
                //cd 7a a6 10 0a 01 00 00
                //7a 90

                m_canListener.setupWaitMessage(0x800021);
                if (!canUsbDevice.sendMessage(msg))
                {
                    AddToCanTrace("Send failed to send");
                }
                CANMessage response = new CANMessage();
                response = m_canListener.waitMessage(500);
                if (response.getID() == 0x800021)
                {
                    CastInfoEvent(response.getID().ToString("X6") + " " + response.getData().ToString("X16"), ActivityType.ConvertingFile);
                }
                /*msg = new CANMessage(0x000FFFFE, 0, 8);
                msg.setData(0xCB7AB9F000000000);
                if (canUsbDevice.sendMessage(msg))
                {
                    AddToCanTrace("Send OK 2");
                }*/
            }
        }

        public void SendCommand(ulong cmd)
        {
            if (canUsbDevice.isOpen())
            {
                CANMessage msg = new CANMessage(0x000FFFFE, LAWICEL.CANMSG_EXTENDED, 8);
                msg.setFlags(LAWICEL.CANMSG_EXTENDED);
                msg.setData(cmd);
                m_canListener.setupWaitMessage(0x000021);
                canUsbDevice.sendMessage(msg);
                CANMessage response = new CANMessage();
                response = m_canListener.waitMessage(1);
                //if (response.getID() == 0x800021)
                {
                    CastInfoEvent(response.getID().ToString("X6") + " " + response.getData().ToString("X16"), ActivityType.ConvertingFile);
                }
            }
        }

        public UInt32 readChecksum(int startAddress, int endAddress)
        {
            UInt32 checksum = 0;
            if (canUsbDevice.isOpen())
            {
                //EnterProgrammingMode();
                CANMessage msg;
                ulong data;
                CANMessage response;
                msg = new CANMessage(0x000FFFFE, LAWICEL.CANMSG_EXTENDED, 8);
                msg.setFlags(LAWICEL.CANMSG_EXTENDED);
                ulong cmd = 0x0000000000009C7A;
                ulong addressHigh = (uint)startAddress & 0x0000000000FF0000;
                addressHigh /= 0x10000;
                ulong addressMiddle = (uint)startAddress & 0x000000000000FF00;
                addressMiddle /= 0x100;
                ulong addressLow = (uint)startAddress & 0x00000000000000FF;
                cmd |= (addressLow * 0x10000000000);
                cmd |= (addressMiddle * 0x100000000);
                cmd |= (addressHigh * 0x1000000);
                msg.setData(cmd);
                m_canListener.setupWaitMessage(0x000021);
                canUsbDevice.sendMessage(msg);
                Thread.Sleep(1);
                response = new CANMessage();
                response = m_canListener.waitMessage(2000);
                
                if (response.getID() == 0x000021 && response.getCanData(1) == 0x9C)
                {
                    CastInfoEvent(response.getID().ToString("X6") + " " + response.getData().ToString("X16"), ActivityType.ConvertingFile);
                }
                msg = new CANMessage(0x000FFFFE, LAWICEL.CANMSG_EXTENDED, 8);
                msg.setFlags(LAWICEL.CANMSG_EXTENDED);
                cmd = 0x000000000000B47A;
                int address2Read = endAddress;
                addressHigh = (uint)address2Read & 0x0000000000FF0000;
                addressHigh /= 0x10000;
                addressMiddle = (uint)address2Read & 0x000000000000FF00;
                addressMiddle /= 0x100;
                addressLow = (uint)address2Read & 0x00000000000000FF;
                cmd |= (addressLow * 0x10000000000);
                cmd |= (addressMiddle * 0x100000000);
                cmd |= (addressHigh * 0x1000000);
                msg.setData(cmd);
                CastInfoEvent("Sending " + msg.getID().ToString("X6") + " " + msg.getData().ToString("X16"), ActivityType.ConvertingFile);
                m_canListener.setupWaitMessage(0x000021);
                canUsbDevice.sendMessage(msg);
                Thread.Sleep(1);
                response = new CANMessage();
                response = m_canListener.waitMessage(2000);

                if (response.getID() == 0x000021 && response.getCanData(1) == 0xB1)
                {
                    //_data[address - startAddress] = getCanData(data, 2);
                    //percentage = (float)(address * 100) / _data.Length;
                    //CastInfoEvent(percentage.ToString("F2") + " % done", ActivityType.DownloadingFlash);
                    CastInfoEvent(response.getID().ToString("X6") + " " + response.getData().ToString("X16"), ActivityType.ConvertingFile);
                    //TODO: set checksum
                }

            }
            return checksum;
        }
    }
}
