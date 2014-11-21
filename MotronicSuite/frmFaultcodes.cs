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
    public partial class frmFaultcodes : DevExpress.XtraEditors.XtraForm
    {
        public delegate void onClearDTC(object sender, ClearDTCEventArgs e);
        public event frmFaultcodes.onClearDTC onClearCurrentDTC;

        public frmFaultcodes()
        {
            InitializeComponent();
        }

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        public void addFault(int code, string faultcode)
        {
            AddFaultCode(code, faultcode);
            gridView1.BestFitColumns();
            //listBox1.Items.Add(faultcode);
        }

        public void addFault(string code, string faultcode)
        {
            AddFaultCode(code, faultcode);
            gridView1.BestFitColumns();
            //listBox1.Items.Add(faultcode);
        }

        private void AddFaultCode(int code, string description)
        {
            if (gridControl1.DataSource == null)
            {
                DataTable dtn = new DataTable();
                dtn.Columns.Add("Code");
                dtn.Columns.Add("Description");
                gridControl1.DataSource = dtn;
            }
            DataTable dt = (DataTable)gridControl1.DataSource;
            bool _found = false;
            foreach (DataRow dr in dt.Rows)
            {
                if (dr["Code"] != DBNull.Value)
                {
                    if (dr["Code"].ToString() == code.ToString())
                    {
                        _found = true;
                    }
                }
            }
            if (!_found)
            {
                dt.Rows.Add(code, description);
            }
               
        }

        private void AddFaultCode(string code, string description)
        {
            if (gridControl1.DataSource == null)
            {
                DataTable dtn = new DataTable();
                dtn.Columns.Add("Code");
                dtn.Columns.Add("Description");
                gridControl1.DataSource = dtn;
            }
            DataTable dt = (DataTable)gridControl1.DataSource;
            bool _found = false;
            foreach (DataRow dr in dt.Rows)
            {
                if (dr["Code"] != DBNull.Value)
                {
                    if (dr["Code"].ToString() == code.ToString())
                    {
                        _found = true;
                    }
                }
            }
            if (!_found)
            {
                dt.Rows.Add(code, description);
            }

        }


        public void ClearCodes()
        {
            //listBox1.Items.Clear();
            DataTable dtn = new DataTable();
            dtn.Columns.Add("Code");
            dtn.Columns.Add("Description");
            gridControl1.DataSource = dtn;
        }

        private void simpleButton2_Click(object sender, EventArgs e)
        {
            // clear this specific DTC code
            //TODO: cast an event to the main application to have it cleared
            int[] selrows = gridView1.GetSelectedRows();
            if (selrows.Length > 0)
            {
                foreach (int i in selrows)
                {
                    DataRow drv = gridView1.GetDataRow(i);
                    if (drv["Code"] != DBNull.Value)
                    {
                        if (onClearCurrentDTC != null)
                        {
                            onClearCurrentDTC(this, new ClearDTCEventArgs(drv["Code"].ToString()));
                        }
                    }
                }
            }
                 
            

           /* if (listBox1.SelectedIndex >= 0)
            {
                if (onClearCurrentDTC != null)
                {
                    onClearCurrentDTC(this, new ClearDTCEventArgs((string)listBox1.Items[listBox1.SelectedIndex]));
                }
            }*/
        }

        public class ClearDTCEventArgs : System.EventArgs
        {
            private string _dtccode;
            
            public string DTCCode
            {
                get
                {
                    return _dtccode;
                }
            }


            public ClearDTCEventArgs(string dtccode)
            {
                this._dtccode = dtccode;
            }
        }
    }
}