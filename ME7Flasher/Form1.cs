using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MotronicCommunication;
using System.Threading;
using System.IO;

namespace ME7CommunicationTester
{
    public partial class Form1 : Form
    {
        public delegate void DelegateSetLabelText(Label lbl, string text);
        public DelegateSetLabelText m_DelegateSetLabelText;
        public delegate void DelegateSetProgressValue(ProgressBar bar, int position);
        public DelegateSetProgressValue m_DelegateSetProgressValue;
        private bool _AppExiting = false;
        private ME7CommunicationTester.Properties.Settings set = new ME7CommunicationTester.Properties.Settings();
        ME7Communication _cancomms;
        //ME7KLineCommunication _comms;
        private bool _holdUpdate = false;
        public Form1()
        {
            InitializeComponent();
            Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
            m_DelegateSetLabelText = new DelegateSetLabelText(this.SetLabelCaption);
            m_DelegateSetProgressValue = new DelegateSetProgressValue(this.SetProgressValue);
            Application.ApplicationExit += new EventHandler(Application_ApplicationExit);
            _cancomms = new ME7Communication();
            _cancomms.onCanInfo += new ME7Communication.CanInfo(_comms_onCanInfo);
            _cancomms.onCanCount += new ME7Communication.CanCount(_cancomms_onCanCount);
            _cancomms.onWriteProgress += new ME7Communication.WriteProgress(_cancomms_onWriteProgress);
            _cancomms.onCanBusLoad += new ME7Communication.CanBusLoad(_cancomms_onCanBusLoad);
            _cancomms.setCANDevice("LAWICEL"); //TODO: Make this selectable
            _cancomms.onReadProgress += new ME7Communication.ReadProgress(_cancomms_onReadProgress);
            //_comms = new ME7KLineCommunication();
            //_comms.LogFolder = Application.StartupPath;
            //_comms.EnableLogging = true;
            //_comms.onStatusChanged += new ICommunication.StatusChanged(_comms_onStatusChanged);
            //_comms.onECUInfo += new ICommunication.ECUInfo(_comms_onECUInfo);

        }

       
        void _cancomms_onCanBusLoad(object sender, ME7Communication.CanbusLoadEventArgs e)
        {
            SetProgressBarValue(progressBar2, Convert.ToInt32(e.Load));
            SetLabelText(label6, e.Load.ToString("F1") + " %");
        }

        private void SetProgressBarValue(ProgressBar bar, int position)
        {
            try
            {
                if (!this.IsDisposed && !_AppExiting)
                {
                    Invoke(m_DelegateSetProgressValue, bar, position);
                }
            }
            catch (Exception)
            {

            }
        }

        void Application_ApplicationExit(object sender, EventArgs e)
        {
            _AppExiting = true;
        }

        void _cancomms_onWriteProgress(object sender, ME7Communication.WriteProgressEventArgs e)
        {
            int percentage = (int)e.Percentage;
            if (progressBar1.Value != percentage)
            {
                progressBar1.Value = percentage;
                Application.DoEvents();
            }
        }


        void _cancomms_onReadProgress(object sender, ME7Communication.ReadProgressEventArgs e)
        {
            int percentage = (int)e.Percentage;
            if (progressBar1.Value != percentage)
            {
                progressBar1.Value = percentage;
            }
            Application.DoEvents();
        }

        private void SetProgressValue(ProgressBar bar, int position)
        {
            bar.Value = position;
        }

        private void SetLabelCaption(Label lbl, string text)
        {
            if (lbl.Text != text)
            {
                lbl.Text = text;
                tmrDelay.Enabled = true;
            }
        }

        private void SetLabelText(Label lbl, string text)
        {
            if (!this.IsDisposed)
            {
                Invoke(m_DelegateSetLabelText, lbl, text);
            }
        }

        void _cancomms_onCanCount(object sender, ME7Communication.CanCountEventArgs e)
        {
            if (!_holdUpdate)
            {
                SetLabelText(lblRxCount, e.RxCount.ToString());
                SetLabelText(lblTxCount, e.TxCount.ToString());
                SetLabelText(lblErrCount, e.ErrCount.ToString());
                _holdUpdate = true;
            }
        }

        void _comms_onECUInfo(object sender, ICommunication.ECUInfoEventArgs e)
        {
            Console.WriteLine(e.IDNumber.ToString() + " " + e.Info);

        }

        void _comms_onStatusChanged(object sender, ICommunication.StatusEventArgs e)
        {
            Console.WriteLine(e.Info);   
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _cancomms.openDevice(false, set.HighSpeedCAN);
            //_comms.StartCommunication("COM1");

        }

        void _comms_onCanInfo(object sender, ME7Communication.CanInfoEventArgs e)
        {
            if (e.Type == ActivityType.DownloadingFlash)
            {
                SetLabelText(label5, e.Info);
                Application.DoEvents();
            }
            else
            {
                AddToLogItem(e.Info);
            }
        }

        private void AddToLogItem(string item)
        {
            string totitem = DateTime.Now.ToString("HH:mm:ss.fff") + " " + item;
            listBox1.Items.Add(totitem);
            listBox1.SelectedIndex = listBox1.Items.Count - 1;
            Console.WriteLine(totitem);
            while (listBox1.Items.Count > 1000) listBox1.Items.RemoveAt(0);
            Application.DoEvents();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _cancomms.Cleanup();
            //_comms.StopCommunication();
        }

        private void button2_Click(object sender, EventArgs e)
        {

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Binary files|*.bin";
            ofd.Multiselect = false;
            if (ofd.ShowDialog() == DialogResult.OK)
            {

                // verify checksums.. 
                if (!Checksum_Volvo_ME7(ofd.FileName, true))
                {
                    DialogResult dr = MessageBox.Show("The checksums of this file seem to be incorrect. Do you want to update them?", "Attention!", MessageBoxButtons.YesNoCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        return;
                    }
                    if (dr == DialogResult.Yes)
                    {
                        Checksum_Volvo_ME7(ofd.FileName, false);
                    }
                }
                if (Checksum_Volvo_ME7(ofd.FileName, true)) AddToLogItem("Checksums ok.");
                else AddToLogItem("Checksums failed.");
                _cancomms.ProgramECU(ofd.FileName);
            }
            
        }

        public void savebytetobinary(int address, byte data, string filename)
        {
            FileStream fsi1 = File.OpenWrite(filename);
            BinaryWriter bw1 = new BinaryWriter(fsi1);
            fsi1.Position = address;
            bw1.Write((byte)data);
            fsi1.Flush();
            bw1.Close();
            fsi1.Close();
            fsi1.Dispose();
        }

        /*******************************************************************************
  *  Routine: Checksum_Volvo_ME7
  *  Input: file_buffer = bin file buffer for checksum calculation 
  *  Output: file_buffer is directly modified
  *  
  *  Author: Salvatore Faro
  *  E-Mail: info@mtx-electronics.com
  *  Website: www.mtx-electronics.com
  * 
  *  License: Dilemma use the same open source license that you are using for your prog        
  ******************************************************************************/

        private bool Checksum_Volvo_ME7(string filename, bool checkOnly)
        {
            UInt32 buffer_index = 0x1F810;
            UInt32 start_addr;
            UInt32 end_addr;
            UInt32 checksum;
            UInt32 currChecksum;
            UInt32 ComplimentcurrChecksum;
            byte[] file_buffer = File.ReadAllBytes(filename);
            bool valid = true;
            do
            {
                // Get the checksum zone start address
                start_addr = ((UInt32)file_buffer[buffer_index + 3] << 24)
                           + ((UInt32)file_buffer[buffer_index + 2] << 16)
                           + ((UInt32)file_buffer[buffer_index + 1] << 8)
                           + (UInt32)file_buffer[buffer_index];

                // Get the checksum zone end address
                end_addr = ((UInt32)file_buffer[buffer_index + 7] << 24)
                         + ((UInt32)file_buffer[buffer_index + 6] << 16)
                         + ((UInt32)file_buffer[buffer_index + 5] << 8)
                         + (UInt32)file_buffer[buffer_index + 4];

                // Calculate the checksum by 32bit sum from star_addr to end_addr
                checksum = 0;
                for (UInt32 addr = start_addr; addr < end_addr; addr += 2)
                    checksum += ((UInt32)file_buffer[addr + 1] << 8) + (UInt32)file_buffer[addr];


                currChecksum = ((UInt32)file_buffer[buffer_index + 11] << 24)
                           + ((UInt32)file_buffer[buffer_index + 10] << 16)
                           + ((UInt32)file_buffer[buffer_index + 9] << 8)
                           + (UInt32)file_buffer[buffer_index + 8];
                ComplimentcurrChecksum = ((UInt32)file_buffer[buffer_index + 15] << 24)
                           + ((UInt32)file_buffer[buffer_index + 14] << 16)
                           + ((UInt32)file_buffer[buffer_index + 13] << 8)
                           + (UInt32)file_buffer[buffer_index + 12];

               // Console.WriteLine("checksum calc: " + checksum.ToString("X8") + " file: " + currChecksum.ToString("X8"));

                if (checksum != currChecksum)
                {
                    valid = false;
                }
                UInt32 complchecksum = ~checksum;
                //Console.WriteLine("checksum inv calc: " + checksum.ToString("X8") + " file: " + currChecksum.ToString("X8"));
                if (ComplimentcurrChecksum != complchecksum)
                {
                    valid = false;
                }
                if (!checkOnly)
                {
                    // Save the new checksum

                    savebytetobinary((int)(buffer_index + 8), (byte)(checksum & 0x000000FF), filename);
                    savebytetobinary((int)(buffer_index + 9), (byte)((checksum & 0x0000FF00) >> 8), filename);
                    savebytetobinary((int)(buffer_index + 10), (byte)((checksum & 0x00FF0000) >> 16), filename);
                    savebytetobinary((int)(buffer_index + 11), (byte)((checksum & 0xFF000000) >> 24), filename);
                    // Save the complement of the new checksum
                    checksum = ~checksum;
                    savebytetobinary((int)(buffer_index + 12), (byte)(checksum & 0x000000FF), filename);
                    savebytetobinary((int)(buffer_index + 13), (byte)((checksum & 0x0000FF00) >> 8), filename);
                    savebytetobinary((int)(buffer_index + 14), (byte)((checksum & 0x00FF0000) >> 16), filename);
                    savebytetobinary((int)(buffer_index + 15), (byte)((checksum & 0xFF000000) >> 24), filename);
                }
                buffer_index += 0x10;
            }
            while (buffer_index < 0x1FA00);
            return valid;
        }


        private void timer1_Tick(object sender, EventArgs e)
        {
            //_comms.TestComm();
            byte battvoltage = _cancomms.ReadLiveData(ME7Communication.ME7LiveDataType.BatteryVoltage, wakeup);
            if (battvoltage != 0)
            {
                float batteryVoltage = battvoltage;
                batteryVoltage /= 10;
                //label1.Text = batteryVoltage.ToString("F1") + " V";
                //AddToLogItem(label1.Text);
            }
            wakeup = false;
        }

        private bool wakeup = true;

        private void button3_Click(object sender, EventArgs e)
        {
            timer1.Enabled = !timer1.Enabled;

        }

        private void button4_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Binary files|*.bin";
            ofd.Multiselect = false;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                if (Checksum_Volvo_ME7(ofd.FileName, true)) AddToLogItem(Path.GetFileName(ofd.FileName) + " - Checksums ok.");
                else AddToLogItem(Path.GetFileName(ofd.FileName) + " - Checksums failed.");
            }

        }

        private void button5_Click(object sender, EventArgs e)
        {
            _cancomms.EraseFlash();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            //_cancomms.EnterProgrammingMode();
            //Thread.Sleep(500);
            _cancomms.SendCommand(0x000000000000807A);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            //_cancomms.ReadFlash();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            _cancomms.Reset();
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            try
            {
                if (_cancomms.openDevice(false, set.HighSpeedCAN))
                {
                    if (set.HighSpeedCAN)
                    {
                        AddToLogItem("CAN channel opened at 250kb/s");
                    }
                    else
                    {
                        AddToLogItem("CAN channel opened at 500kb/s");
                    }
                }
                else
                {
                    AddToLogItem("Failed to open CAN channel");
                }
            }
            catch (Exception E)
            {
                AddToLogItem("Failed to open CAN channel");
            }
        }

        /*

0x320000 to 0x3FFFFF = 0x020000 to 0x0FFFFF
         * 
-> E  id = 000FFFFE timestamp = 00000000 len = 8 data =ce 7a bb 32 ff f0 10 00 (Read 10(16) bytes from 32fff0)
<- E  id = 00800021 timestamp = 016A9E14 len = 8 data =8f 7a fb 32 ff f0 ae 1b 
<- E  id = 00800021 timestamp = 016A9E23 len = 8 data =09 db 00 de 1b db 00 7e 
<- E  id = 00800021 timestamp = 016A9E33 len = 8 data =4f 23 db 00 44 44 44 44 

In flash:
02FFF0: ae 1b db 00 de 1b db 00 7e 23 db 00 44 44 44 44


-> E  id = 000FFFFE timestamp = 00000000 len = 8 data =ce 7a bb 33 00 00 10 00 
<- E  id = 00800021 timestamp = 016F837F len = 8 data =8f 7a fb 33 00 00 f3 f8 
<- E  id = 00800021 timestamp = 016F837F len = 8 data =09 8b c7 f7 f8 19 8c f7 
<- E  id = 00800021 timestamp = 016F838E len = 8 data =4f 8e 18 8c db 00 9a 3d 

In flash:
030000: f3 f8 8b c7 f7 f8 19 8c f7 8e 18 8c db 00 9a 3d          * */

        private void button9_Click(object sender, EventArgs e)
        { 
            /*SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Binary files|*.bin";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                _cancomms.ReadEprom(sfd.FileName, 1000);
            }*/
            // try to read the flash contents
            
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Binary files|*.bin";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                _cancomms.EnterProgrammingMode();
                if (_cancomms.SendSBL())
                {
                    _cancomms.StartSBL();
                    Thread.Sleep(500);
                    byte[] _data = new byte[0x100000];
                    int totalIdx = 0;
                    AddToLogItem("Reading flash data... stand by");


                    int offset = 0x8000;
                    for (int iBlock = 0; iBlock < 0xF800; iBlock++) // was 10000
                    {
                        if (this.IsDisposed) break;
                        if (_AppExiting) break;
                        Application.DoEvents();
                        
                        int startAddress = offset + (iBlock * 0x10);
                        int endAddress = offset + (iBlock * 0x10) + 0x10;
                        float percentage = (float)iBlock * 100 / 0x10000;
                        //AddToLogItem("Reading flash data... stand by " + iBlock.ToString() + "/256 [" + startAddress.ToString("X8") + "-" + endAddress.ToString("X8") + "]");
                        byte[] _smalldata = _cancomms.ReadFlash(startAddress, endAddress, percentage);
                        string strDebug = startAddress.ToString("X8") + " - ";
                        for (int i = 0; i < _smalldata.Length; i++)
                        {
                            strDebug += _smalldata[i].ToString("X2") + " ";
                            _data[startAddress + i] = _smalldata[i];
                        }
                        label5.Text = percentage.ToString("F2") + " % done";
                        progressBar1.Value = Convert.ToInt32(percentage);
                        //AddToLogItem(strDebug);
                    }
                    File.WriteAllBytes(sfd.FileName, _data);
                    Console.WriteLine("Leaving secondary bootloader");
                    _cancomms.LeaveProgrammingMode();
                }
                else
                {
                    AddToLogItem("Failed to upload secondary bootloader, please reset ECU");
                }
            }
        }

        private void tmrDelay_Tick(object sender, EventArgs e)
        {
            _holdUpdate = false;
            tmrDelay.Enabled = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            _cancomms.EnableCanLog = checkBox1.Checked;
        }

        private void button10_Click(object sender, EventArgs e)
        {
            // read ECU's checksums and verify correctness for them
            _cancomms.EnterProgrammingMode();
            if (_cancomms.SendSBL())
            {
                _cancomms.StartSBL();
                AddToLogItem("Starting checksum verification");
                Thread.Sleep(500);
                Int32 ecu_index = 0x1F810;
                Int32 buffer_index = 0;
                byte[] checksum_buffer = _cancomms.ReadFlash(ecu_index, ecu_index + (31 * 4), 0);
                UInt32 start_addr;
                UInt32 end_addr;
                UInt32 checksum;
                UInt32 currChecksum;
                UInt32 ComplimentcurrChecksum;
                bool valid = true;
                for (int checksumNumber = 0; checksumNumber < 31; checksumNumber++)
                {
                    // Get the checksum zone start address

                    start_addr = ((UInt32)checksum_buffer[buffer_index + 3] << 24)
                               + ((UInt32)checksum_buffer[buffer_index + 2] << 16)
                               + ((UInt32)checksum_buffer[buffer_index + 1] << 8)
                               + (UInt32)checksum_buffer[buffer_index + 0];

                    // Get the checksum zone end address
                    end_addr = ((UInt32)checksum_buffer[buffer_index + 7] << 24)
                             + ((UInt32)checksum_buffer[buffer_index + 6] << 16)
                             + ((UInt32)checksum_buffer[buffer_index + 5] << 8)
                             + (UInt32)checksum_buffer[buffer_index + 4];
                    AddToLogItem("Verify checksum area " + start_addr.ToString("X8") + " - " + end_addr.ToString("X8"));
                    if (start_addr >= 0x8000 && end_addr >= 0x8000)
                    {
                        //TODO: we should have the ECU calculate the checksum for us with the B4 command!
                        // Calculate the checksum by 32bit sum from star_addr to end_addr
                        //UInt32 _readChecksum = _cancomms.readChecksum((int)start_addr, (int)end_addr);
                        //Console.WriteLine("ECU checksum: " + _readChecksum.ToString("X8"));
                        byte[] file_buffer = _cancomms.ReadFlash((int)start_addr, (int)end_addr + 1, 0);
                        if (file_buffer.Length == 0) break;
                        checksum = 0;
                        for (UInt32 addr = 0; addr < end_addr - start_addr; addr += 2)
                            checksum += ((UInt32)file_buffer[addr + 1] << 8) + (UInt32)file_buffer[addr];


                        currChecksum = ((UInt32)checksum_buffer[buffer_index + 11] << 24)
                                   + ((UInt32)checksum_buffer[buffer_index + 10] << 16)
                                   + ((UInt32)checksum_buffer[buffer_index + 9] << 8)
                                   + (UInt32)checksum_buffer[buffer_index + 8];
                        ComplimentcurrChecksum = ((UInt32)checksum_buffer[buffer_index + 15] << 24)
                                   + ((UInt32)checksum_buffer[buffer_index + 14] << 16)
                                   + ((UInt32)checksum_buffer[buffer_index + 13] << 8)
                                   + (UInt32)checksum_buffer[buffer_index + 12];

                        // Console.WriteLine("checksum calc: " + checksum.ToString("X8") + " file: " + currChecksum.ToString("X8"));

                        if (checksum != currChecksum)
                        {
                            valid = false;
                        }
                        UInt32 complchecksum = ~checksum;
                        //Console.WriteLine("checksum inv calc: " + checksum.ToString("X8") + " file: " + currChecksum.ToString("X8"));
                        if (ComplimentcurrChecksum != complchecksum)
                        {
                            valid = false;
                        }
                        if (!valid)
                        {
                            AddToLogItem("checksum area " + start_addr.ToString("X8") + " - " + end_addr.ToString("X8") + " failed ");
                            AddToLogItem(checksum.ToString("X8") + " " + currChecksum.ToString("X8"));
                            AddToLogItem(ComplimentcurrChecksum.ToString("X8") + " " + complchecksum.ToString("X8"));
                            break;
                        }
                    }
                    buffer_index += 0x10;
                    listBox1.Items[listBox1.Items.Count - 1] += " OK";
                }
                AddToLogItem("Checksum verification done");
                _cancomms.LeaveProgrammingMode();
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            _cancomms.EnterProgrammingMode();
            Thread.Sleep(100);
            _cancomms.SendSBL();
            Thread.Sleep(100);
            _cancomms.StartSBL();
            Thread.Sleep(500);
            Console.WriteLine("Checksum1: " + _cancomms.readChecksum(0x8000, 0xBFFF));
            Thread.Sleep(500);
            Console.WriteLine("Checksum2: " + _cancomms.readChecksum(0x8000, 0xC000));
            Thread.Sleep(500);
            _cancomms.LeaveProgrammingMode();
        }
    }
}
