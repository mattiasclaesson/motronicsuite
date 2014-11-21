using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace MotronicSuite
{
    public partial class frmProgress : Form
    {
        public frmProgress()
        {
            InitializeComponent();
        }

        public void SetProgress(string text)
        {
            label1.Text = text ;
            Application.DoEvents();
        }

        internal void SetProgressPercentage(int p)
        {
            this.Height = 163;
            progressBarControl1.Visible = true;
            progressBarControl1.EditValue = p;
            Application.DoEvents();
        }
    }
}