using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using MotronicTools;

namespace MotronicSuite
{
    public partial class AxisBrowser : DevExpress.XtraEditors.XtraUserControl
    {
        public delegate void StartSymbolViewer(object sender, SymbolViewerRequestedEventArgs e);
        public event AxisBrowser.StartSymbolViewer onStartSymbolViewer;

        public delegate void StartAxisViewer(object sender, AxisViewerRequestedEventArgs e);
        public event AxisBrowser.StartAxisViewer onStartAxisViewer;

        public class AxisViewerRequestedEventArgs : System.EventArgs
        {
            private string _axisaddress;

            public string AxisAddress
            {
                get { return _axisaddress; }
                set { _axisaddress = value; }
            }


            public AxisViewerRequestedEventArgs(string axisaddress)
            {
                this._axisaddress = axisaddress;
            }
        }

        public class SymbolViewerRequestedEventArgs : System.EventArgs
        {
            private string _mapname;

            public string Mapname
            {
                get { return _mapname; }
                set { _mapname = value; }
            }


            public SymbolViewerRequestedEventArgs(string mapname)
            {
                this._mapname = mapname;
            }
        }

        private AxisCollection m_axis = new AxisCollection();

        public AxisCollection Axis
        {
            get { return m_axis; }
            set { m_axis = value; }
        }

        public AxisBrowser()
        {
            InitializeComponent();
        }

        private bool AxisHasLeadingAxis(AxisCollection axis, int address, out int axisaddress)
        {
            bool retval = false;
            axisaddress = 0;
            foreach (AxisHelper ah in m_axis)
            {
                int endaddress = ah.Addressinfile + ah.Length + 2;
                if (endaddress == address)
                {
                    retval = true;
                    axisaddress = ah.Addressinfile;
                }
            }
            return retval;
        }

        private string GetXAxisAddress(SymbolCollection m_symbols, string mapname)
        {
            string x = string.Empty;
            string y = string.Empty;

            foreach (SymbolHelper sh in m_symbols)
            {
                if (sh.Varname == mapname)
                {
                    // get the axis
                    foreach (AxisHelper ah in m_axis)
                    {
                        int endaddress = ah.Addressinfile + ah.Length + 2;
                        if (endaddress == sh.Flash_start_address)
                        {
                            // this is an axis for this table... 
                            // see if there is another one that leads 
                            int newaddress = 0;
                            if (AxisHasLeadingAxis(m_axis, ah.Addressinfile, out newaddress))
                            {
                                x = newaddress.ToString("X4");
                            }
                            else
                            {
                                x = ah.Addressinfile.ToString("X4");
                            }
                        }
                    }
                    foreach (AxisHelper ah in m_axis)
                    {
                        int endaddress = ah.Addressinfile + ah.Length + 2;
                        if (endaddress == sh.Flash_start_address)
                        {
                            // this is an axis for this table... 
                            // see if there is another one that leads 
                            //y = GetLeadingAxis(axis, ah.Addressinfile);
                            y = ah.Addressinfile.ToString("X4");
                        }
                    }

                }
            }
            return y;
        }

        private string GetYAxisAddress(SymbolCollection m_symbols, string mapname)
        {
            string x = string.Empty;
            string y = string.Empty;

            foreach (SymbolHelper sh in m_symbols)
            {
                if (sh.Varname == mapname)
                {
                    // get the axis
                    foreach (AxisHelper ah in m_axis)
                    {
                        int endaddress = ah.Addressinfile + ah.Length + 2;
                        if (endaddress == sh.Flash_start_address)
                        {
                            // this is an axis for this table... 
                            // see if there is another one that leads 
                            int newaddress = 0;
                            if (AxisHasLeadingAxis(m_axis, ah.Addressinfile, out newaddress))
                            {
                                x = newaddress.ToString("X4");
                            }
                            else
                            {
                               // x = ah.Addressinfile.ToString("X4");
                            }
                        }
                    }
                }
            }
            return x;
        }

        public void ShowSymbolCollection(SymbolCollection sc)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("SYMBOLNAME");
            dt.Columns.Add("DESCRIPTION");
            dt.Columns.Add("XAXIS");
            dt.Columns.Add("XAXISDESCRIPTION");
            dt.Columns.Add("YAXIS");
            dt.Columns.Add("YAXISDESCRIPTION");
            dt.Columns.Add("XAXISADDRESS");
            dt.Columns.Add("YAXISADDRESS");
            //SymbolAxesTranslator sat = new SymbolAxesTranslator();
            //SymbolTranslator symtrans = new SymbolTranslator();
            string helptext = string.Empty;
            XDFCategories cat = XDFCategories.Undocumented;
            XDFSubCategory subcat = XDFSubCategory.Undocumented;
            foreach (SymbolHelper sh in sc)
            {
                string xaxis = sh.XDescr;
                string yaxis = sh.YDescr;
                string symboldescr = sh.Varname;//symtrans.TranslateSymbolToHelpText(sh.Varname, out helptext, out cat, out subcat);
                string xaxisdescr = "";
                string xaxisaddress = GetXAxisAddress(sc, sh.Varname);
                string yaxisaddress = GetYAxisAddress(sc, sh.Varname);
                if (sh.X_axisvalues != null)
                {
                    foreach (float fval in sh.X_axisvalues)
                    {
                        xaxisdescr += fval.ToString("F2") + " ";
                    }
                }
                string yaxisdescr = "";
                if (sh.Y_axisvalues != null)
                {
                    foreach (float fval in sh.Y_axisvalues)
                    {
                        yaxisdescr += fval.ToString("F2") + " ";
                    }
                }
                if (xaxis != "" || yaxis != "")
                {
                    dt.Rows.Add(sh.Varname, symboldescr, xaxis, xaxisdescr, yaxis, yaxisdescr, xaxisaddress, yaxisaddress);
                }
            }
            gridControl1.DataSource = dt;
        }

        public void SetCurrentSymbol(string symbolname)
        {
            if (symbolname == "")
            {
                ClearFilters();
            }
            else
            {
                SetDefaultFilters(symbolname);
            }
        }
        private void ClearFilters()
        {
            gridView1.ActiveFilterEnabled = false;
        }

        private void SetDefaultFilters(string symbolname)
        {
            DevExpress.XtraGrid.Columns.ColumnFilterInfo fltr = new DevExpress.XtraGrid.Columns.ColumnFilterInfo(@"([SYMBOLNAME] = '" + symbolname + "')", "Symbol:" + symbolname);
            gridView1.ActiveFilter.Clear();
            gridView1.ActiveFilter.Add(gcBrowseSymbolName, fltr);
            gridView1.ActiveFilterEnabled = true;
        }

        private void gridControl1_DoubleClick(object sender, EventArgs e)
        {
            DevExpress.XtraGrid.Views.Grid.GridView obj = gridControl1.MainView as DevExpress.XtraGrid.Views.Grid.GridView;
            DevExpress.XtraGrid.Views.Grid.ViewInfo.GridHitInfo hi = obj.CalcHitInfo(gridControl1.PointToClient(Cursor.Position));

            // valid info?
            if (!(hi.IsValid && hi.InRowCell)) return;

            // is symbol (symbol or axis) column?
            if (!(obj.FocusedColumn.FieldName == "SYMBOLNAME" || obj.FocusedColumn.FieldName == "XAXIS" || obj.FocusedColumn.FieldName == "YAXIS" || obj.FocusedColumn.FieldName == "XAXISADDRESS" || obj.FocusedColumn.FieldName == "YAXISADDRESS")) return;

            // check for null ref.
            if (obj.FocusedValue == null || string.IsNullOrEmpty(obj.FocusedValue.ToString())) return;

            if (onStartSymbolViewer != null && obj.FocusedColumn.FieldName == "SYMBOLNAME")
            {
                onStartSymbolViewer(this, new SymbolViewerRequestedEventArgs(obj.FocusedValue.ToString().Trim()));
            }
            else if (onStartAxisViewer != null && (obj.FocusedColumn.FieldName == "XAXISADDRESS" || obj.FocusedColumn.FieldName == "XAXIS"))
            {
                object odata = gridView1.GetRowCellValue(hi.RowHandle, "XAXISADDRESS");
                if(odata != null)
                {
                    onStartAxisViewer(this, new AxisViewerRequestedEventArgs(odata.ToString()));
                }
            }
            else if (onStartAxisViewer != null && (obj.FocusedColumn.FieldName == "YAXISADDRESS" || obj.FocusedColumn.FieldName == "YAXIS"))
            {
                object odata = gridView1.GetRowCellValue(hi.RowHandle, "YAXISADDRESS");
                if (odata != null)
                {
                    onStartAxisViewer(this, new AxisViewerRequestedEventArgs(odata.ToString()));
                }

            }

        }

    }
}
