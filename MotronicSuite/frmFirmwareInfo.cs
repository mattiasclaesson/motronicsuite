using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraEditors;

namespace MotronicSuite
{
    public partial class frmFirmwareInfo : DevExpress.XtraEditors.XtraForm
    {
        private int _SpeedLimit = 0;

        public int SpeedLimit
        {
            get
            {
                _SpeedLimit = Convert.ToInt32(spinEdit1.EditValue);
                return _SpeedLimit;
            }
            set
            {
                _SpeedLimit = value;
                if (_SpeedLimit != 0)
                {
                    spinEdit1.Enabled = true;
                    spinEdit1.EditValue = _SpeedLimit;
                }
            }
        }
        private int _RpmLimit = 0;

        public int RpmLimit
        {
            get
            {
                _RpmLimit = Convert.ToInt32(spinEdit2.EditValue);
                return _RpmLimit;
            }
            set
            {
                _RpmLimit = value;
                if (_RpmLimit != 0)
                {
                    spinEdit2.Enabled = true;
                    spinEdit2.EditValue = _RpmLimit;
                }
            }
        }

        private bool m_compare_file = false;

        public bool Compare_file
        {
            get { return m_compare_file; }
            set { m_compare_file = value; }
        }

        private bool m_open_file = false;
        private string m_filetoOpen = string.Empty;

        public string FiletoOpen
        {
            get { return m_filetoOpen; }
            set { m_filetoOpen = value; }
        }
        public bool Open_file
        {
            get { return m_open_file; }
            set { m_open_file = value; }
        }
        public string HardwareID
        {
            get
            {
                return textEdit1.Text;
            }
            set
            {
                textEdit1.Text = value;
                textEdit1.Properties.MaxLength = textEdit1.Text.Length;
            }
        }

        public string DamosInfo
        {
            get
            {
                return textEdit2.Text;
            }
            set
            {
                textEdit2.Text = value;
                textEdit2.Properties.MaxLength = textEdit2.Text.Length;

            }
        }

        public string PartNumber
        {
            get
            {
                return textEdit3.Text;
            }
            set
            {
                textEdit3.Text = value;
                textEdit3.Properties.MaxLength = textEdit3.Text.Length;
                if (textEdit3.Text.Length > 0)
                {
                    textEdit3.Enabled = true;
                }
            }
        }

        public string SoftwareID
        {
            get
            {
                return textEdit4.Text;
            }
            set
            {
                textEdit4.Text = value;
                textEdit4.Properties.MaxLength = textEdit4.Text.Length;

            }
        }

        

        public string Checksum
        {
            get
            {
                return textEdit6.Text;
            }
            set
            {
                try
                {
                    textEdit6.Text = value;
                }
                catch (Exception E)
                {
                    Console.WriteLine("Failed to determine checksum in info screen: "+ E.Message );
                }

            }
        }

       

        public frmFirmwareInfo()
        {
            InitializeComponent();
        }

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            this.Close();
        }

        private void simpleButton2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            this.Close();
        }

        

        internal void DisableAdvancedControls()
        {
            


        }

        private void textEdit3_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            frmPartnumberLookup partnumberlookup = new frmPartnumberLookup();
            partnumberlookup.LookUpPartnumber(textEdit3.Text);
            partnumberlookup.ShowDialog();
            if (partnumberlookup.Open_File)
            {
                m_filetoOpen = partnumberlookup.GetFileToOpen();
                m_open_file = true;
                this.DialogResult = DialogResult.Abort;
                this.Close();
            }
            else if (partnumberlookup.Compare_File)
            {
                m_filetoOpen = partnumberlookup.GetFileToOpen();
                m_compare_file = true;
                this.DialogResult = DialogResult.Abort;
                this.Close();
            }
        }

        public bool AutomaticTransmission
        {
            set
            {
                cbAutomaticTransmission.Checked = value;
                cbAutomaticTransmission.Visible = true;
            }
            get
            {
                return cbAutomaticTransmission.Checked;
            }
        }

        internal bool SpeedLimiterEnabled()
        {
            if (spinEdit1.Enabled) return true;
            return false;
        }
        internal bool RpmLimiterEnabled()
        {
            if (spinEdit2.Enabled) return true;
            return false;
        }
    }
}