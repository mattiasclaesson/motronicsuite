using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MotronicCommunication;
using System.IO;

namespace M4Flasher
{
    public partial class Form1 : Form
    {
        public delegate void DelegateUpdateProgress(string information, int percentage);
        M4Flasher.Properties.Settings set = new M4Flasher.Properties.Settings();
        public DelegateUpdateProgress m_DelegateUpdateProgress;


        public Form1()
        {
            InitializeComponent();
            m_DelegateUpdateProgress = new DelegateUpdateProgress(this.UpdateProgress);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Binary files|*.bin";
            ofd.Multiselect = false;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                Application.DoEvents();
                SetProgressPercentage("Preparing flasher", 0);
                IFlasher flasher;
                // what kind of file is loaded?
                FileInfo fi = new FileInfo(ofd.FileName);
                if (fi.Length == 0x20000) //M4.4 file
                {
                    flasher = new M44Flasher();
                    flasher.onStatusChanged += new IFlasher.StatusChanged(flasher_onStatusChanged);
                    flasher.FlashFile(ofd.FileName, set.COMPORT);
                }
                else if (fi.Length == 0x10000) // M4.3 file
                {
                    flasher = new M43Flasher();
                    flasher.onStatusChanged += new IFlasher.StatusChanged(flasher_onStatusChanged);
                    flasher.FlashFile(ofd.FileName, set.COMPORT);
                }
            }
        }

        void flasher_onStatusChanged(object sender, IFlasher.StatusEventArgs e)
        {
            if (!this.IsDisposed)
            {
                try
                {
                    this.Invoke(m_DelegateUpdateProgress, e.Info, e.Percentage);
                }
                catch (Exception E)
                {
                    Console.WriteLine(E.Message);
                }
            }
            Application.DoEvents();

        }

        private string lastInfo = string.Empty;

        private void UpdateProgress(string information, int percentage)
        {
            Console.WriteLine(information + " " + percentage.ToString() + " %");
            SetProgressPercentage(information, percentage);
        }

        private void SetProgressPercentage(string item, int percentage)
        {
            if (item != lastInfo)
            {
                lastInfo = item;
                listBox1.Items.Add(item);
                listBox1.SelectedIndex = listBox1.Items.Count - 1;
            }
            progressBar1.Value = percentage;
            Application.DoEvents();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Binary files|*.bin";
            ofd.Multiselect = false;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                Application.DoEvents();
                IFlasher flasher;
                FileInfo fi = new FileInfo(ofd.FileName);
                if (fi.Length == 0x20000) //M4.4 file
                {
                    flasher = new M44Flasher();
                    flasher.onStatusChanged += new IFlasher.StatusChanged(flasher_onStatusChanged);
                    flasher.VerifyChecksum(ofd.FileName, "COM1");
                }
            }
        }
    }
}
