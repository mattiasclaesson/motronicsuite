using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using MotronicTools;

namespace MotronicSuite
{
    public partial class CompareResults : DevExpress.XtraEditors.XtraUserControl
    {
        public delegate void NotifySelectSymbol(object sender, SelectSymbolEventArgs e);
        public event CompareResults.NotifySelectSymbol onSymbolSelect;

        private SymbolCollection m_compareSymbolCollection = new SymbolCollection();
        private AxisCollection m_compareAxisCollection = new AxisCollection();

        public AxisCollection CompareAxisCollection
        {
            get { return m_compareAxisCollection; }
            set { m_compareAxisCollection = value; }
        }

        public SymbolCollection CompareSymbolCollection
        {
            get { return m_compareSymbolCollection; }
            set { m_compareSymbolCollection = value; }
        }

        private bool m_UseForFind = false;

        public bool UseForFind
        {
            get { return m_UseForFind; }
            set
            {
                m_UseForFind = value;
                if (m_UseForFind)
                {
                    // hide certain columns
                    gridColumn7.Visible = false;
                    gridColumn8.Visible = false;
                    gridColumn9.Visible = false;
                    gridColumn10.Visible = false;
                    gridColumn11.Visible = false;
                    gridColumn12.Visible = false;
                    gridColumn13.Visible = false;
                    showDifferenceMapToolStripMenuItem.Visible = false;
                    saveLayoutToolStripMenuItem.Visible = false;
                }
            }
        }

        private string m_filename = "";

        public string Filename
        {
            get { return m_filename; }
            set { m_filename = value; }
        }

        private bool m_ShowAddressesInHex = true;

        public bool ShowAddressesInHex
        {
            get { return m_ShowAddressesInHex; }
            set { m_ShowAddressesInHex = value; }
        }

        public void SetFilterMode(bool IsHexMode)
        {
            if (IsHexMode)
            {
                
                gridColumn2.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                gridColumn2.DisplayFormat.FormatString = "X6";
                gridColumn2.FilterMode = DevExpress.XtraGrid.ColumnFilterMode.DisplayText;
                gridColumn3.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                gridColumn3.DisplayFormat.FormatString = "X6";
                gridColumn3.FilterMode = DevExpress.XtraGrid.ColumnFilterMode.DisplayText;
                gridColumn4.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                gridColumn4.DisplayFormat.FormatString = "X6";
                gridColumn4.FilterMode = DevExpress.XtraGrid.ColumnFilterMode.DisplayText;
                gridColumn5.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                gridColumn5.DisplayFormat.FormatString = "X6";
                gridColumn5.FilterMode = DevExpress.XtraGrid.ColumnFilterMode.DisplayText;
                gridColumn12.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                gridColumn12.DisplayFormat.FormatString = "X6";
                gridColumn12.FilterMode = DevExpress.XtraGrid.ColumnFilterMode.DisplayText;
                gridColumn13.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                gridColumn13.DisplayFormat.FormatString = "X6";
                gridColumn13.FilterMode = DevExpress.XtraGrid.ColumnFilterMode.DisplayText;

            }
            else
            {
                gridColumn2.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                gridColumn2.DisplayFormat.FormatString = "";
                gridColumn2.FilterMode = DevExpress.XtraGrid.ColumnFilterMode.Value;
                gridColumn3.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                gridColumn3.DisplayFormat.FormatString = "";
                gridColumn3.FilterMode = DevExpress.XtraGrid.ColumnFilterMode.Value;
                gridColumn4.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                gridColumn4.DisplayFormat.FormatString = "";
                gridColumn4.FilterMode = DevExpress.XtraGrid.ColumnFilterMode.Value;
                gridColumn5.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                gridColumn5.DisplayFormat.FormatString = "";
                gridColumn5.FilterMode = DevExpress.XtraGrid.ColumnFilterMode.Value;
                gridColumn12.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                gridColumn12.DisplayFormat.FormatString = "";
                gridColumn12.FilterMode = DevExpress.XtraGrid.ColumnFilterMode.Value;
                gridColumn13.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                gridColumn13.DisplayFormat.FormatString = "";
                gridColumn13.FilterMode = DevExpress.XtraGrid.ColumnFilterMode.Value;
            }
        }


        public CompareResults()
        {
            InitializeComponent();
            gridView1.RestoreLayoutFromRegistry("HKEY_CURRENT_USER\\Software\\MotronicSuite\\CompareView");
        }

        public void SetGridWidth()
        {
            gridView1.BestFitColumns();
        }

        private void CastSelectEvent(int m_map_address, int m_map_length, string m_map_name, int symbolnumber1, int symbolnumber2)
        {
            if (onSymbolSelect != null)
            {
                // haal eerst de data uit de tabel van de gridview
                onSymbolSelect(this, new SelectSymbolEventArgs(m_map_address, m_map_length, m_map_name, m_filename, false, m_compareSymbolCollection, m_compareAxisCollection, symbolnumber1, symbolnumber2));
            }
        }

        private void CastDifferenceEvent(int m_map_address, int m_map_length, string m_map_name, int symbolnumber1, int symbolnumber2)
        {
            if (onSymbolSelect != null)
            {
                // haal eerst de data uit de tabel van de gridview
                onSymbolSelect(this, new SelectSymbolEventArgs(m_map_address, m_map_length, m_map_name, m_filename, true, m_compareSymbolCollection, m_compareAxisCollection, symbolnumber1, symbolnumber2));
            }
        }

        public void OpenGridViewGroups(GridControl ctrl, int groupleveltoexpand)
        {
            // open grouplevel 0 (if available)
            ctrl.BeginUpdate();
            try
            {
                GridView view = (GridView)ctrl.DefaultView;
                //view.ExpandAllGroups();
                view.MoveFirst();
                while (!view.IsLastRow)
                {
                    int rowhandle = view.FocusedRowHandle;
                    if (view.IsGroupRow(rowhandle))
                    {
                        int grouplevel = view.GetRowLevel(rowhandle);
                        if (grouplevel <= groupleveltoexpand)
                        {
                            view.ExpandGroupRow(rowhandle);
                        }
                    }
                    view.MoveNext();
                }
                view.MoveFirst();
            }
            catch (Exception E)
            {
                Console.WriteLine(E.Message);
            }
            ctrl.EndUpdate();
        }

        private void StartTableViewer()
        {
            if (gridView1.SelectedRowsCount > 0)
            {
                int[] selrows = gridView1.GetSelectedRows();
                if (selrows.Length > 0)
                {
                    DataRowView dr = (DataRowView)gridView1.GetRow((int)selrows.GetValue(0));
                    string Map_name = dr.Row["SYMBOLNAME"].ToString();
                    int address = Convert.ToInt32(dr.Row["FLASHADDRESS"].ToString());
                    int length = Convert.ToInt32(dr.Row["LENGTHBYTES"].ToString());
                    int symbolnumber1 = 0;
                    int symbolnumber2 = 0;
                    if (dr.Row["SymbolNumber1"] != DBNull.Value)
                    {
                        symbolnumber1 = Convert.ToInt32(dr.Row["SymbolNumber1"]);
                    }
                    if (dr.Row["SymbolNumber2"] != DBNull.Value)
                    {
                        symbolnumber2 = Convert.ToInt32(dr.Row["SymbolNumber2"]);
                    }
                    CastSelectEvent(address, length, Map_name, symbolnumber1, symbolnumber2);
                }
            }

        }


        private void gridView1_DoubleClick(object sender, EventArgs e)
        {
            int[] selectedrows = gridView1.GetSelectedRows();
            if (selectedrows.Length > 0)
            {
                int grouplevel = gridView1.GetRowLevel((int)selectedrows.GetValue(0));
                if (grouplevel >= gridView1.GroupCount)
                {
                    StartTableViewer();
                }
            }
        }

        public class SelectSymbolEventArgs : System.EventArgs
        {
            private int _symbolnumber1;

            public int Symbolnumber1
            {
                get { return _symbolnumber1; }
                set { _symbolnumber1 = value; }
            }
            private int _symbolnumber2;

            public int Symbolnumber2
            {
                get { return _symbolnumber2; }
                set { _symbolnumber2 = value; }
            }
            private int _address;
            private int _length;
            private string _mapname; 
            private string _filename;
            private bool _showdiffmap;
            private SymbolCollection _symbols;

            public SymbolCollection Symbols
            {
                get { return _symbols; }
                set { _symbols = value; }
            }

            private AxisCollection _axis;

            public AxisCollection Axis
            {
                get { return _axis; }
                set { _axis = value; }
            }

            public bool ShowDiffMap
            {
                get
                {
                    return _showdiffmap;
                }
            }

            public int SymbolAddress
            {
                get
                {
                    return _address;
                }
            }

            public int SymbolLength
            {
                get
                {
                    return _length;
                }
            }

            public string SymbolName
            {
                get
                {
                    return _mapname;
                }
            }

            public string Filename
            {
                get
                {
                    return _filename;
                }
            }

            public SelectSymbolEventArgs(int address, int length, string mapname, string filename, bool showdiffmap, SymbolCollection symColl, AxisCollection axis, int symbolnumber1, int symbolnumber2)
            {
                this._address = address;
                this._length = length;
                this._mapname = mapname;
                this._filename = filename;
                this._showdiffmap = showdiffmap;
                this._symbols = symColl;
                this._axis = axis;
                this._symbolnumber1 = symbolnumber1;
                this._symbolnumber2 = symbolnumber2;
            }
        }

        private void gridView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                StartTableViewer();
                e.Handled = true;
            }
        }

        private void gridView1_CustomDrawCell(object sender, DevExpress.XtraGrid.Views.Base.RowCellCustomDrawEventArgs e)
        {
            if (e.Column.Name == gridColumn6.Name)
            {
                object o = gridView1.GetRowCellValue(e.RowHandle, "CATEGORY");
                Color c = Color.White;
                if (o != DBNull.Value)
                {
                    if (Convert.ToInt32(o) == (int)XDFCategories.Fuel)
                    {
                        c = Color.LightSteelBlue;
                    }
                    else if (Convert.ToInt32(o) == (int)XDFCategories.Ignition)
                    {
                        c = Color.LightGreen;
                    }
                    else if (Convert.ToInt32(o) == (int)XDFCategories.Boost_control)
                    {
                        c = Color.OrangeRed;
                    }
                    else if (Convert.ToInt32(o) == (int)XDFCategories.Misc)
                    {
                        c = Color.LightGray;
                    }
                    else if (Convert.ToInt32(o) == (int)XDFCategories.Sensor)
                    {
                        c = Color.Yellow;
                    }
                    else if (Convert.ToInt32(o) == (int)XDFCategories.Correction)
                    {
                        c = Color.LightPink;
                    }
                    else if (Convert.ToInt32(o) == (int)XDFCategories.Idle)
                    {
                        c = Color.BurlyWood;
                    }
                }
                if (c != Color.White)
                {
                    System.Drawing.Drawing2D.LinearGradientBrush gb = new System.Drawing.Drawing2D.LinearGradientBrush(e.Bounds, c, Color.White, System.Drawing.Drawing2D.LinearGradientMode.Horizontal);
                    e.Graphics.FillRectangle(gb, e.Bounds);
                }

            }
            else if (e.Column.Name == gridColumn2.Name || e.Column.Name == gridColumn3.Name || e.Column.Name == gridColumn4.Name || e.Column.Name == gridColumn5.Name)
            {
                /*if (!m_ShowAddressesInHex)
                {
                    if (e.CellValue != null)
                    {
                        if (e.CellValue != DBNull.Value)
                        {

                            e.DisplayText = e.CellValue.ToString();
                        }
                    }
                }*/
            }

        }

        private void showDifferenceMapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // trek de ene map van de andere af en toon het resultaat in een mapviewer!
            if (gridView1.SelectedRowsCount > 0)
            {
                int[] selrows = gridView1.GetSelectedRows();
                if (selrows.Length > 0)
                {
                    DataRowView dr = (DataRowView)gridView1.GetRow((int)selrows.GetValue(0));
                    string Map_name = dr.Row["SYMBOLNAME"].ToString();
                    int address = Convert.ToInt32(dr.Row["FLASHADDRESS"].ToString());
                    int length = Convert.ToInt32(dr.Row["LENGTHBYTES"].ToString());
                    int symbolnumber1 = 0;
                    int symbolnumber2 = 0;
                    if (dr.Row["SymbolNumber1"] != DBNull.Value)
                    {
                        symbolnumber1 = Convert.ToInt32(dr.Row["SymbolNumber1"]);
                    }
                    if (dr.Row["SymbolNumber2"] != DBNull.Value)
                    {
                        symbolnumber2 = Convert.ToInt32(dr.Row["SymbolNumber2"]);
                    }

                    CastDifferenceEvent(address, length, Map_name, symbolnumber1, symbolnumber2);
                }
            }
        }

        private void exportToExcelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            gridControl1.ExportToXls(Application.StartupPath + "\\diffexport" + ".xls");
            System.Diagnostics.Process.Start(Application.StartupPath + "\\diffexport"  + ".xls");
        }

        private void saveLayoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            gridView1.SaveLayoutToRegistry("HKEY_CURRENT_USER\\Software\\MotronicSuite\\CompareView");
        }


    }
}
