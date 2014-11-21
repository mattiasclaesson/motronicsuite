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
    public partial class frmDecodeVIN : DevExpress.XtraEditors.XtraForm
    {
        public frmDecodeVIN()
        {
            InitializeComponent();
        }

        private void textEdit1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                DecodeVIN();
            }
        }

        private void DecodeVIN()
        {
            lblCarModel.Text = "---";
            lblEngineType.Text = "---";
            lblMakeyear.Text = "---";
            lblPlant.Text = "---";
            lblSeries.Text = "---";
            lblTurbo.Text = "---";
            lblExtraInfo.Text = "---";
            VINDecoder decoder = new VINDecoder();
            VINCarInfo carinfo = decoder.DecodeVINNumber(textEdit1.Text);
            lblCarModel.Text = carinfo.CarModel.ToString();
            lblEngineType.Text = carinfo.EngineType.ToString();
            lblMakeyear.Text = carinfo.Makeyear.ToString();
            lblPlant.Text = carinfo.PlantInfo;
            lblSeries.Text = carinfo.Series;
            lblTurbo.Text = carinfo.TurboModel.ToString();
            lblExtraInfo.Text = carinfo.ExtraInfo;
        }

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            DecodeVIN();
            
        }

        private void frmDecodeVIN_Load(object sender, EventArgs e)
        {
            
        }

        private void textEdit1_EditValueChanged(object sender, EventArgs e)
        {
            try
            {
                VINDecoder decoder = new VINDecoder();
                VINCarInfo carinfo = decoder.DecodeVINNumber(textEdit1.Text);
                lblCarModel.Text = carinfo.CarModel.ToString();
                lblEngineType.Text = carinfo.EngineType.ToString();
                lblMakeyear.Text = carinfo.Makeyear.ToString();
                lblPlant.Text = carinfo.PlantInfo;
                lblSeries.Text = carinfo.Series;
                lblTurbo.Text = carinfo.TurboModel.ToString();
                lblExtraInfo.Text = carinfo.ExtraInfo;
            }
            catch (Exception E)
            {
                Console.WriteLine("Failed to convert VIN number partially: " + E.Message);
            }
        }
    }
}