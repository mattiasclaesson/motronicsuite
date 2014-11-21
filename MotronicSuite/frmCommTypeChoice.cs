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
    public partial class frmCommTypeChoice : DevExpress.XtraEditors.XtraForm
    {
        public frmCommTypeChoice()
        {
            InitializeComponent();
        }

        private void simpleButton3_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private int _commType = 0;

        public int CommType
        {
            get { return _commType; }
            set { _commType = value; }
        }

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            _commType = 1; // 1 = Motronic 4.3
            this.Close();
        }

        private void simpleButton2_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            _commType = 2; // 2 = Motronic 4.4
            this.Close();
        }

        private void simpleButton4_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            _commType = 3; // 3 = Motronic ME7
            this.Close();
        }

        private void simpleButton5_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            _commType = 4; // 4 = Motronic 2.10.3
            this.Close();
        }
    }
}