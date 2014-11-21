using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using MotronicTools;

namespace MotronicSuite
{
    public partial class frmEditRealtimeSymbol : DevExpress.XtraEditors.XtraForm
    {
        private bool _isM44 = false;

        public bool IsM44
        {
            get { return _isM44; }
            set { _isM44 = value; }
        }

        private string _varname;

        public string Varname
        {
            get { return _varname; }
            set { _varname = value; }
        }

        private int _symbolnumber;

        public int Symbolnumber
        {
            get { return _symbolnumber; }
            set { _symbolnumber = value; }
        }


        public double MinimumValue
        {
            get { return Convert.ToDouble(spinEdit1.EditValue); }
            set { spinEdit1.EditValue = value; }
        }
        public double MaximumValue
        {
            get { return Convert.ToDouble(spinEdit2.EditValue); }
            set { spinEdit2.EditValue = value; }
        }
        public double OffsetValue
        {
            get { return Convert.ToDouble(spinEdit3.EditValue); }
            set { spinEdit3.EditValue = value; }
        }
        public double CorrectionValue
        {
            get { return Convert.ToDouble(spinEdit4.EditValue); }
            set { spinEdit4.EditValue = value; }
        }

        public string Symbolname
        {
            get { return lookUpEdit1.EditValue.ToString(); }
            set { lookUpEdit1.EditValue = value; }
        }


        public string Description
        {
            get { return textEdit1.Text; }
            set { textEdit1.Text = value; }
        }

        public string Units
        {
            get { return textEdit5.Text; }
            set { textEdit5.Text = value; }
        }

        public int Address
        {
            get { return Convert.ToInt32(textEdit2.Text, 16); }
            set { textEdit2.Text = value.ToString("X4"); }
        }

        public int Length
        {
            get { return Convert.ToInt32(textEdit3.Text); }
            set { textEdit3.Text = value.ToString(); }
        }

        public frmEditRealtimeSymbol()
        {
            InitializeComponent();
        }

        private SymbolCollection m_symbols = new SymbolCollection();

        public SymbolCollection Symbols
        {
            get { return m_symbols; }
            set { m_symbols = value; }
        }

        private void frmEditRealtimeSymbol_Load(object sender, EventArgs e)
        {
            lookUpEdit1.Properties.DataSource = m_symbols;
            lookUpEdit1.Properties.DisplayMember = "Varname";
            lookUpEdit1.Properties.ValueMember = "Varname";
        }

        private void lookUpEdit1_EditValueChanged(object sender, EventArgs e)
        {
            string varname = lookUpEdit1.EditValue.ToString();
            foreach (SymbolHelper sh in m_symbols)
            {
                if (sh.Varname == varname)
                {
                    _varname = varname;

                    spinEdit1.EditValue = sh.MinValue;
                    spinEdit2.EditValue = sh.MaxValue;
                    spinEdit3.EditValue = sh.CorrectionOffset;
                    spinEdit4.EditValue = sh.CorrectionFactor;
                    textEdit1.Text = sh.Description;
                    textEdit2.Text = sh.Start_address.ToString("X4");
                    textEdit3.Text = sh.Length.ToString();
                    textEdit4.BackColor = sh.Color;
                    textEdit5.Text = sh.Units;
                }
            }
        }

        private float GetSymbolCorrectionFactor(string varname)
        {
            float returnvalue = 1;
            switch (varname)
            {
                case "Internal load":
                    returnvalue = 0.05F;
                    break;
                case "Battery voltage":
                    returnvalue = 0.0704F;
                    break;
                case "Engine speed":
                    returnvalue = 40F;
                    if (_isM44) returnvalue = 30F;
                    break;
                case "Ignition advance":
                    returnvalue = 0.75F;
                    if (_isM44) returnvalue = -0.75F;
                    break;
            }
            return returnvalue;
        }

        private float GetSymbolOffset(string varname)
        {
            float returnvalue = 0;
            switch (varname)
            {
                case "Internal load":
                    returnvalue = 0;
                    break;
                case "Engine speed":
                    returnvalue = 0;
                    break;
                case "Ignition advance":
                    returnvalue = -22.5F;
                    if (_isM44) returnvalue = 78F;
                    break;
                case "Battery voltage":
                    returnvalue = 0;
                    break;
            }
            return returnvalue;
        }

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            if (_varname == string.Empty)
            {
                try
                {
                    _varname = lookUpEdit1.EditValue.ToString();
                }
                catch (Exception)
                {

                }
            }

            DialogResult = DialogResult.OK;
            this.Close();
        }

        private void simpleButton2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            this.Close();
        }

        public Color SymbolColor
        {
            get
            {
                return textEdit4.BackColor;
            }
            set
            {
                textEdit4.BackColor = value;
                textEdit4.Text = value.ToString();
            }
        }

        private void textEdit4_DoubleClick(object sender, EventArgs e)
        {
            colorDialog1.Color = textEdit4.BackColor;
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                textEdit4.BackColor = colorDialog1.Color;
                textEdit4.Text = colorDialog1.Color.ToString();
            }
        }

    }
}