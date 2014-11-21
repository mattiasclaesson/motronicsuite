using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using System.IO;

namespace MotronicSuite
{
    public partial class frmPartNumberList : DevExpress.XtraEditors.XtraForm
    {
        private string m_selectedpartnumber = "";
        private DataTable partnumbers = new DataTable();
        public string Selectedpartnumber
        {
            get { return m_selectedpartnumber; }
            set { m_selectedpartnumber = value; }
        }

        private string m_selectedSoftwareID = string.Empty;

        public string SelectedSoftwareID
        {
            get { return m_selectedSoftwareID; }
            set { m_selectedSoftwareID = value; }
        }

        public frmPartNumberList()
        {
            InitializeComponent();
            partnumbers.Columns.Add("FILENAME");
            partnumbers.Columns.Add("PARTNUMBER");
            partnumbers.Columns.Add("ENGINETYPE");
            partnumbers.Columns.Add("CARTYPE");
            partnumbers.Columns.Add("SWID");
            partnumbers.Columns.Add("DAMOS", Type.GetType("System.Boolean"));
        }

        private void LoadPartNumbersFromFiles()
        {
            
            if (Directory.Exists(Application.StartupPath + "\\Binaries"))
            {
                string[] binfiles = Directory.GetFiles(Application.StartupPath + "\\Binaries", "*.BIN");
                foreach (string binfile in binfiles)
                {
                    string binfilename = Path.GetFileNameWithoutExtension(binfile);
                    string partnumber = "";

                    string enginetype = "";
                    string cartype = "";
                    string swid = string.Empty;
                    string additionalinfo = "";
                    string damossupport = "";
                    bool _supportsDamosFile = false;
                    if (binfilename.Contains("_"))
                    {
                        char[] sep = new char[1];
                        sep.SetValue('_', 0);
                        string[] values = binfilename.Split(sep);
                        if (values.Length == 1)
                        {
                            // assume partnumber
                            partnumber = (string)binfilename;
                            partnumbers.Rows.Add(binfile, partnumber, enginetype, cartype, swid, false);
                        }
                        else if (values.Length == 2)
                        {
                            partnumber = (string)values.GetValue(0);
                            swid = (string)values.GetValue(1);
                            partnumbers.Rows.Add(binfile, partnumber, enginetype, cartype, swid, false);
                        }
                        else if (values.Length == 3)
                        {
                            partnumber = (string)values.GetValue(0);
                            swid = (string)values.GetValue(1);
                            damossupport = (string)values.GetValue(2);
                            _supportsDamosFile = false;
                            try
                            {
                                if (Convert.ToInt32(damossupport) == 1) _supportsDamosFile = true;
                            }
                            catch (Exception E)
                            {
                                Console.WriteLine(E.Message);
                            }
                            partnumbers.Rows.Add(binfile, partnumber, enginetype, cartype, swid, _supportsDamosFile);
                        }
                    }
                    else
                    {
                        // assume partnumber
                        partnumber = (string)binfilename;
                        partnumbers.Rows.Add(binfile, partnumber, enginetype, cartype, swid, false);
                    }
                }
            }
        }

        private void frmPartNumberList_Load(object sender, EventArgs e)
        {
            PartnumberCollection pnc = new PartnumberCollection();
            DataTable dt = pnc.GeneratePartNumberCollection();
            //dt.Columns.Add("Filename");
            //dt.Columns.Add("Tuner");
            //dt.Columns.Add("Stage");
            //dt.Columns.Add("Info");

            LoadPartNumbersFromFiles();

            gridControl1.DataSource = dt;
            gridView1.Columns["Carmodel"].Group();
            gridView1.Columns["Enginetype"].Group();
            gridView1.BestFitColumns();
        }

        private void gridView1_DoubleClick(object sender, EventArgs e)
        {
            int[] rows = gridView1.GetSelectedRows();
            if(rows.Length > 0)
            {
                m_selectedpartnumber = (string)gridView1.GetRowCellValue((int)rows.GetValue(0), "Partnumber");
                m_selectedSoftwareID = (string)gridView1.GetRowCellValue((int)rows.GetValue(0), "SoftwareVersion");
                if (m_selectedpartnumber != null)
                {
                    if (m_selectedpartnumber != string.Empty)
                    {
                        this.Close();
                    }
                }
            }
        }

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            int[] rows = gridView1.GetSelectedRows();
            if (rows.Length > 0)
            {
                m_selectedpartnumber = (string)gridView1.GetRowCellValue((int)rows.GetValue(0), "Partnumber");
                m_selectedSoftwareID = (string)gridView1.GetRowCellValue((int)rows.GetValue(0), "SoftwareVersion");
            }
            this.Close();
        }

        private bool CheckInAvailableLibrary(string partnumber, string swid, out bool damos)
        {
            bool retval = false;
            damos = false;
            //Console.WriteLine("Looking for partnumber: " + partnumber + " swid: " + swid);
            foreach (DataRow dr in partnumbers.Rows)
            {
                //Console.WriteLine("check: " + dr["PARTNUMBER"].ToString() + " " + dr["SWID"].ToString());
                if (dr["PARTNUMBER"] != DBNull.Value)
                {
                    if (dr["PARTNUMBER"].ToString() == partnumber)
                    {
                        if (swid != string.Empty)
                        {
                            if (dr["SWID"] != DBNull.Value)
                            {
                                if (dr["SWID"].ToString() == swid)
                                {
                                    retval = true;
                                    try
                                    {
                                        damos = Convert.ToBoolean(dr["DAMOS"]);
                                    }
                                    catch (Exception bE)
                                    {
                                        Console.WriteLine(bE.Message);
                                    }
                                    break;
                                }
                            }
                        }
                        else
                        {
                            retval = true;
                            break;
                        }
                    }
                }
            }
            return retval;
        }

        private void gridView1_CustomDrawCell(object sender, DevExpress.XtraGrid.Views.Base.RowCellCustomDrawEventArgs e)
        {
            if (e.Column.FieldName == "Partnumber")
            {
                if (e.CellValue != null)
                {
                    if (e.CellValue != DBNull.Value)
                    {
                        // check sw version as well
                        bool damos = false;
                        object oswid = gridView1.GetRowCellValue(e.RowHandle, "SoftwareVersion");
                        if (CheckInAvailableLibrary(e.CellValue.ToString(), oswid.ToString(), out damos))
                        {
                            if (damos)
                            {
                                e.Graphics.FillRectangle(Brushes.LightBlue, e.Bounds);
                            }
                            else
                            {
                                e.Graphics.FillRectangle(Brushes.YellowGreen, e.Bounds);
                            }
                        }
                    }
                }
            }
        }
    }
}