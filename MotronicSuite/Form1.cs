#region Information

/*
 * 3B is Engine speed
 * 40 is Engine load
 * 37 or 38 is ECT or IAT
 * 
Motronic 8bit maps 
Most of Motronic maps use the same format.
The code is usually in the beginning of the EPROM, simply because IT vectors start at address $0000. This code is general purpose, very configurable and usable by any Bosch's customer. The other part of the EPROM contains customer's code and data (maps or parameters). 

Maps can be 1D or 2D. They are all accessed by the same procedure which is given a table address and the map number.
We'll call this table Map descriptors.
Each byte of this table describes where and how to find the needed map. The meaning of each map has been defined once forever by Bosch, they refer to these tables in their code. The customer is free to define whether the map is 1D or 2D, which parameters it depends on and where to find it. 

The map access procedure reads from DPTR pointers the location of the table and reads the byte in the requested position (as in a 1D array). If the read byte is even then the map is 1D, if it's odd - 2D. In both cases the rest of the byte is used to find the address of the map. The address is found at: (<map directory addr> + & 0xFE), because addresses are 2 byte long.
The map directory address is given by the caller or is known by the procedure. The directory stores 2 byte addresses with the MSByte first. The directory is usually straightforward to find as it contains addresses that point at the socend part of the EPROM address space. 


Each map describes which input parameters must be fetched to get the output value. In case of input values out of the map, the border map value is used (the output saturates on border values).

By looking at the map you can not find if it's 1D or 2D, you must know it from the map descriptors.

Example of a 2D map starting at 49F4

049F0 38 50 50 80 3A 04 05 23 23 9C 42 02 0A 9C FF 00
04A00 CD 00 9A 00 4D 00 38 06 1F 1F 20 1E 1F 51 80 60

map byte description:
3A - the X parameter name, in fact the 8051 sram location
04 - the X map size
05 23 23 9C - value of X axis points calculated as follows
x0: 0x100 - (9c - 23 - 23 - 05)
x1: 0x100 - (9c - 23 - 23)
x2: 0x100 - (9c - 23)
x3: 0x100 - 9c
it's a delta format, the x3 value can not be less then x2 value
42 - the Y param name
02 - the Y size
0A 9C - Y axis values (100-9C-0A) (100-9C)

FF 00 CD 00 9A 00 4D 00 - the Z values in the order
x0y0 x0y1 x1y0 x1y1 x2y0 x2y1 x3y0 x3y1  * * */

#endregion

#region todo M43
// DONE: show axis in the correct tabpage
// DONE: Dwell map (0x54 length) = "Dwell angle characteristic map", goes into catergory ignition
// DONE: Figure out the incorrectly detected map @F535 with length 0x0A
// -TEST- Cranking fuel enrichment: 0xEB10
// -TEST- Warmup fuel correction: 0xEA6B
// -TEST- Idle fuel map: 0xF298
// -TEST- NORMAL - WOT ignition? low octane ignition map
// -TEST- WARMUP - part throttle (highest values in low load)? warmup ignition map is actually the high-octane 
// -TEST- KNOCKING - bad fuel, limp-home mode?
// Implement a tuning wizard
// Additional maps to be found!
// More settings (like lambda control on/off) to be determined
// Coolant fuel correction (warmup): 0xEBAA
// DONE: Communication with ECU through K-line interface
// PRIO: Fix overboost map labeling. What about the axis of the 0x08 byte length maps then?
#endregion

#region todo M44
// Finish the M44 flasher
// Implement M44 communication protocol for live data
// Implement M44 communication protocol for DTC codes
#endregion

#region todo general
// DONE: Indicator in the statusbar about what type of file was loaded
// DONE: Option to assign user defined descriptions to symbols and axis
// DONE: Implement project based development
// DONE: Search maps options
// DONE: Skins support
// DONE: Synchronize mapviewers option!
// DONE: Partnumber lookup stuff for different ECU type including indicator for what type the file is
// DONE: Expand with correct engine codes, turbo types, power & torque levels
// DONE: Ability to rescale axis as well, start table viewer from axis gridcontrol / Implement axis browser
// DONE: Split file intelligence into IECUFile class per type (LH2.2, LH2.4, LH2.4.2, M4.3, M4.4 and ME7)
// Tuning packages options
// DONE: Realtime logging through realtimegraph component in realtime panel!
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using Sim8051Sharp;
using MotronicSuite;
using System.Diagnostics;
using DevExpress.XtraBars.Docking;
using DevExpress.XtraBars;
using MotronicCommunication;
using RealtimeGraph;
using Microsoft.Win32;
using MotronicTools;
using DevExpress.Skins;

namespace MotronicSuite
{
    #region delegate functions 

    public delegate void DelegateShowChangeLog(Version v);
    public delegate void DelegateStartReleaseNotePanel(string filename, string version);
    public delegate void DelegateUpdateProgress(string information, int percentage);
    public delegate void DelegateUpdateECUInfo(string ecuinfo);
    public delegate void DelegateUpdateRealTimeValue(string symbolname, float value);


    #endregion

    public partial class Form1 : Form
    {
        #region local variables

        frmSplash splash;
        AppSettings m_appSettings;
        IECUFile _workingFile = new M43File(); // default use M4.3
        AxisCollection m_tempaxis = new AxisCollection();
        msiupdater m_msiUpdater;
        public DelegateShowChangeLog m_DelegateShowChangeLog;
        public DelegateUpdateProgress m_DelegateUpdateProgress;
        public DelegateUpdateECUInfo m_DelegateUpdateECUInfo;
        public DelegateStartReleaseNotePanel m_DelegateStartReleaseNotePanel;
        public DelegateUpdateRealTimeValue m_DelegateUpdateRealTimeValue;

        ICommunication _ecucomms;// = new M43Communication(); //TODO: Expand for M4.4 now that flashing is done for M4.4
        private SymbolCollection _realtimeSymbolsM2103;

        #endregion

        public Form1()
        {
            splash = new frmSplash();
            splash.Show();
            Application.DoEvents();

            InitializeComponent();
            m_appSettings = new AppSettings();
            // set correct menu
            ribbonControl1.SelectedPage = ribbonPage1;
            try
            {
                m_DelegateShowChangeLog = new DelegateShowChangeLog(this.ShowChangeLog);
                m_DelegateStartReleaseNotePanel = new DelegateStartReleaseNotePanel(this.StartReleaseNotesViewer);
                m_DelegateUpdateProgress = new DelegateUpdateProgress(this.UpdateProgress);
                m_DelegateUpdateECUInfo = new DelegateUpdateECUInfo(this.UpdateECUInfo);
                m_DelegateUpdateRealTimeValue = new DelegateUpdateRealTimeValue(this.UpdateRealtimeInformationValue);
            }
            catch (Exception E)
            {
                Console.WriteLine(E.Message);
            }
            SetFilterMode();
        }

        private void UpdateRealtimeInformationValueM2103(string symbolname, float value)
        {
            // Console.WriteLine(symbolname + " = " + value.ToString("F2"));
            switch (symbolname)
            {
                case "Engine speed": // rpm
                case "Rpm": // rpm
                    gaugeRPMM2103.Value = value;
                    //_currentEngineStatus.CurrentRPM = value;
                    break;
                case "Engine temperature":
                    gaugeCoolantM2103.Value = value;
                    break;
                case "Throttle position":
                    gaugeThrottleM2103.Value = value;
                    break;
                case "Ignition advance":
                    gaugeIgnM2103.Value = value;
                    break;
                case "Engine load":
                    gaugeLoadM2103.Value = value;
                    break;
                case "Air flow rate":
                    gaugeAirM2103.Value = value;
                    break;
                case "Lambda voltage":
                    gaugeLambdaM2103.Value = value;
                    break;
                case "Vehicle speed":
                    gaugeSpeedM2103.Value = value;
                    break;
                case "Short term trim":
                    gaugeShortM2103.Value = value;
                    break;
                case "Long term trim":
                    gaugeLongM2103.Value = value;
                    break;
            }
        }

        private void UpdateRealtimeInformationValue(string symbolname, float value)
        {
           // Console.WriteLine(symbolname + " = " + value.ToString("F2"));
            switch (symbolname)
            {
                case "Engine speed": // rpm
                case "Rpm": // rpm
                    gaugeEngineSpeed.Value = value;
                    //_currentEngineStatus.CurrentRPM = value;
                    break;
                case "Ignition advance": // ignition advance
                    gaugeIgnitionAdvance.Value = value;
                    break;
                case "Battery voltage": // V batt
                    gaugeBatteryVoltage.Value = value;
                    break;
                case "Internal load": // load
                    gaugeLoad.Value = value;
                    break;
                case "FPSCounter":
                    //label1.Text = value.ToString("F1") + " fps";
                    if (value == 0)
                    {
                        dockRealtime.Text = "Realtime panel [ not monitoring ]";
                    }
                    else
                    {
                        dockRealtime.Text = "Realtime panel [" + value.ToString("F1") + " fps]";
                    }
                    break;
            }
            bool _refresh = false;
            if (gridControl3.DataSource is SymbolCollection)
            {
                SymbolCollection sc = (SymbolCollection)gridControl3.DataSource;
                foreach (SymbolHelper sh in sc)
                {
                    if (sh.Varname == symbolname)
                    {
                        sh.CurrentValue = value;
                        if (sh.PeakValue < value) sh.PeakValue = value;
                        _refresh = true;
                        break;
                    }
                }
                if (_refresh)
                {
                    gridControl3.RefreshDataSource();
                    Application.DoEvents();

                }

            }
        }
       

        #region table/mapviewer

        private void StartHexViewer()
        {
            if (FileTools.Instance.Currentfile != "")
            {
                dockManager1.BeginUpdate();
                try
                {
                    DevExpress.XtraBars.Docking.DockPanel dockPanel;
                    //= dockManager1.AddPanel(DevExpress.XtraBars.Docking.DockingStyle.Right);
                    dockPanel = dockManager1.AddPanel(DevExpress.XtraBars.Docking.DockingStyle.Right);

                    dockPanel.Text = "Hexviewer: " + Path.GetFileName(FileTools.Instance.Currentfile);
                    HexViewer hv = new HexViewer();
                    hv.Issramviewer = false;
                    hv.Dock = DockStyle.Fill;
                    dockPanel.Width = 580;
                    hv.LoadDataFromFile(FileTools.Instance.Currentfile, _workingFile.Symbols);
                    dockPanel.Controls.Add(hv);
                }
                catch (Exception E)
                {
                    Console.WriteLine(E.Message);
                }
                dockManager1.EndUpdate();
            }
        }

        private void StartCompareMapViewer(string SymbolName, string Filename, int SymbolAddress, int SymbolLength, SymbolCollection curSymbols, AxisCollection axis, int symbolnumber)
        {
            try
            {
                int cols = 1;
                int rows = 1; //<GS-21022011>
                // TEST SYMBOLNUMBERS
                if (symbolnumber > 0 && SymbolName.StartsWith("Symbol"))
                {
                    foreach (SymbolHelper h in curSymbols)
                    {
                        if (h.Symbol_number == symbolnumber)
                        {
                            SymbolName = h.Varname;
                        }
                    }
                }
                DevExpress.XtraBars.Docking.DockPanel dockPanel;
                bool pnlfound = false;
                foreach (DevExpress.XtraBars.Docking.DockPanel pnl in dockManager1.Panels)
                {

                    if (pnl.Text == "Symbol: " + SymbolName + " [" + Path.GetFileName(Filename) + "]")
                    {
                        dockPanel = pnl;
                        pnlfound = true;
                        dockPanel.Show();
                        // nog data verversen?
                        foreach (Control c in dockPanel.Controls)
                        {
                            /* if (c is IMapViewer)
                             {
                                 IMapViewer tempviewer = (IMapViewer)c;
                                 tempviewer.Map_content
                             }*/
                        }
                    }
                }
                if (!pnlfound)
                {
                    dockManager1.BeginUpdate();
                    try
                    {
                        //dockPanel = dockManager1.AddPanel(new System.Drawing.Point(-500, -500));
                        dockPanel = dockManager1.AddPanel(DevExpress.XtraBars.Docking.DockingStyle.Right);
                        dockPanel.Tag = Filename;// FileTools.Instance.Currentfile; changed 24/01/2008
                        IMapViewer tabdet;
                        if (m_appSettings.UseNewMapviewer) tabdet = new MapViewerEx();
                        else tabdet = new MapViewer();

                        tabdet.SetViewSize(ViewSize.NormalView);

                        //tabdet.IsHexMode = barViewInHex.Checked;
                        tabdet.Viewtype = ViewType.Easy;
                        tabdet.DisableColors = false;
                        tabdet.AutoSizeColumns = true;
                        tabdet.GraphVisible = true;
                        tabdet.IsRedWhite = false;
                        tabdet.Filename = Filename;
                        tabdet.Map_name = SymbolName;
                        tabdet.Map_descr = tabdet.Map_name;
                        tabdet.Map_cat = XDFCategories.Undocumented;
                        tabdet.AllAxis = _workingFile.Axis;

                        tabdet.XAxisSymbol = Helpers.Instance.GetXaxisSymbol(FileTools.Instance.Currentfile, _workingFile.Symbols, _workingFile.Axis, SymbolName, SymbolAddress);
                        tabdet.YAxisSymbol = Helpers.Instance.GetYAxisSymbol(FileTools.Instance.Currentfile, _workingFile.Symbols, _workingFile.Axis, SymbolName, SymbolAddress);
                        if (tabdet.XAxisSymbol.Flash_start_address == 0 && SymbolLength == 8)
                        {
                            tabdet.XAxisSymbol = Helpers.Instance.GetXaxisSymbol(FileTools.Instance.Currentfile, _workingFile.Symbols, _workingFile.Axis, "Overboost map", Helpers.Instance.GetSymbolAddress(_workingFile.Symbols, "Overboost map"));
                        }
                        tabdet.CurrentFiletype = FileTools.Instance.CurrentFiletype;

                        int[] xvals;
                        int[] yvals;
                        string xdescr = "";
                        string ydescr = "";
                        Helpers.Instance.GetAxisValues(Filename, curSymbols, axis, tabdet.Map_name, SymbolAddress, rows, cols, out xvals, out yvals, out xdescr, out ydescr);
                        tabdet.X_axisvalues = xvals;
                        tabdet.Y_axisvalues = yvals;
                        tabdet.X_axis_name = xdescr;
                        tabdet.Y_axis_name = ydescr;
                        //tabdet.Z_axis_name = 
                        cols = xvals.Length;
                        rows = yvals.Length;
                        string zdescr = string.Empty;
                        tabdet.X_axis_name = xdescr;
                        tabdet.Y_axis_name = ydescr;
                        tabdet.Z_axis_name = zdescr;
                        //<GS-21022011>
                        if (xvals.Length == 1 && yvals.Length == 1 && SymbolLength == 8)
                        {
                            Helpers.Instance.GetAxisValues(FileTools.Instance.Currentfile, _workingFile.Symbols, _workingFile.Axis, "Overboost map", Helpers.Instance.GetSymbolAddress(_workingFile.Symbols, "Overboost map"), rows, cols, out xvals, out yvals, out xdescr, out ydescr);
                            tabdet.X_axisvalues = xvals;
                            tabdet.X_axis_name = xdescr;
                            cols = 8;
                            rows = 1;
                        }
                        //tabdet.Map_sramaddress = GetSymbolAddressSRAM(SymbolName);
                        int columns = cols;
                        int tablewidth = cols;
                        int address = Convert.ToInt32(SymbolAddress);
                        if (address != 0)
                        {
                            tabdet.Map_address = address;
                            int length = SymbolLength;
                            tabdet.Map_length = length;
                            byte[] mapdata = FileTools.Instance.readdatafromfile(Filename, address, length, false);
                            tabdet.Map_content = mapdata;
                            //tabdet.Correction_factor = Helpers.Instance.GetMapCorrectionFactor(tabdet.Map_name);
                            //tabdet.Correction_offset = Helpers.Instance.GetMapCorrectionOffset(tabdet.Map_name);
                            tabdet.Correction_factor = _workingFile.GetCorrectionFactorForMap(tabdet.Map_name);//Helpers.Instance.GetMapCorrectionFactor(tabdet.Map_name);
                            tabdet.Correction_offset = _workingFile.GetOffsetForMap(tabdet.Map_name); //Helpers.Instance.GetMapCorrectionOffset(tabdet.Map_name);

                            tabdet.IsUpsideDown = true;
                            tabdet.Map_length = length;
                            tabdet.ShowTable(columns, false);
                            tabdet.Dock = DockStyle.Fill;
                            //tabdet.onSymbolSave += new MapViewer.NotifySaveSymbol(tabdet_onSymbolSave);
                            tabdet.onClose += new IMapViewer.ViewerClose(tabdet_onClose);
                            tabdet.onSelectionChanged += new IMapViewer.SelectionChanged(tabdet_onSelectionChanged);
                            tabdet.onSurfaceGraphViewChangedEx += new IMapViewer.SurfaceGraphViewChangedEx(mv_onSurfaceGraphViewChangedEx);
                            tabdet.onSurfaceGraphViewChanged += new IMapViewer.SurfaceGraphViewChanged(mv_onSurfaceGraphViewChanged);


                            //dockPanel.DockAsTab(dockPanel1);
                            dockPanel.Text = "Symbol: " + SymbolName + " [" + Path.GetFileName(Filename) + "]";



                            dockPanel.DockTo(dockManager1, DevExpress.XtraBars.Docking.DockingStyle.Right, 0);
                            if (tabdet.X_axisvalues.Length > 0)
                            {
                                dockPanel.Width = 30 + ((tabdet.X_axisvalues.Length + 1) * 35);
                            }
                            else
                            {
                                //dockPanel.Width = this.Width - dockSymbols.Width - 10;

                            }
                            if (dockPanel.Width < 400) dockPanel.Width = 400;
                            dockPanel.Controls.Add(tabdet);
                        }
                    }
                    catch (Exception E)
                    {
                        Console.WriteLine(E.Message);
                    }
                    dockManager1.EndUpdate();
                }
            }
            catch (Exception startnewcompareE)
            {
                Console.WriteLine(startnewcompareE.Message);
            }

        }

        private void StartCompareDifferenceViewer(string SymbolName, string Filename, int SymbolAddress, int SymbolLength)
        {
            int cols = 1;
            int rows = 1; //<GS-21022011>
            DevExpress.XtraBars.Docking.DockPanel dockPanel;
            bool pnlfound = false;
            foreach (DevExpress.XtraBars.Docking.DockPanel pnl in dockManager1.Panels)
            {

                if (pnl.Text == "Symbol difference: " + SymbolName + " [" + Path.GetFileName(Filename) + "]")
                {
                    dockPanel = pnl;
                    pnlfound = true;
                    dockPanel.Show();
                }
            }
            if (!pnlfound)
            {
                dockManager1.BeginUpdate();
                try
                {
                    dockPanel = dockManager1.AddPanel(new System.Drawing.Point(-500, -500));
                    dockPanel.Tag = Filename;
                    //MapViewer tabdet = new MapViewer();
                    IMapViewer tabdet;
                    if (m_appSettings.UseNewMapviewer) tabdet = new MapViewerEx();
                    else tabdet = new MapViewer();
                    tabdet.IsCompareViewer = true;
                    //tabdet.IsHexMode = true; // always in hexmode!
                    tabdet.Viewtype = ViewType.Easy;
                    tabdet.DisableColors = false;
                    tabdet.AutoSizeColumns = true;
                    tabdet.GraphVisible = true;
                    tabdet.IsRedWhite = true;
                    tabdet.SetViewSize(ViewSize.NormalView);
                    tabdet.Filename = Filename;
                    tabdet.Map_name = SymbolName;
                    tabdet.AllAxis = _workingFile.Axis;
                    tabdet.XAxisSymbol = Helpers.Instance.GetXaxisSymbol(FileTools.Instance.Currentfile, _workingFile.Symbols, _workingFile.Axis, SymbolName, Helpers.Instance.GetSymbolAddress(_workingFile.Symbols, SymbolName)/* SymbolAddress*/);
                    tabdet.YAxisSymbol = Helpers.Instance.GetYAxisSymbol(FileTools.Instance.Currentfile, _workingFile.Symbols, _workingFile.Axis, SymbolName, Helpers.Instance.GetSymbolAddress(_workingFile.Symbols, SymbolName)/* SymbolAddress*/);
                    tabdet.CurrentFiletype = FileTools.Instance.CurrentFiletype;

                    //tabdet.Map_descr = TranslateSymbolName(tabdet.Map_name);
                    //tabdet.Map_cat = TranslateSymbolNameToCategory(tabdet.Map_name);
                    int[] xvals;
                    int[] yvals;
                    string xdescr = "";
                    string ydescr = "";
                    Helpers.Instance.GetAxisValues(Filename, _workingFile.Symbols, _workingFile.Axis, tabdet.Map_name, Helpers.Instance.GetSymbolAddress(_workingFile.Symbols, SymbolName)/*SymbolAddress*/, rows, cols, out xvals, out yvals, out xdescr, out ydescr);
                    tabdet.X_axisvalues = xvals;
                    tabdet.Y_axisvalues = yvals;
                    tabdet.X_axis_name = xdescr;
                    tabdet.Y_axis_name = ydescr;
                    cols = xvals.Length;
                    rows = yvals.Length;
                    string zdescr = string.Empty;
                    tabdet.X_axis_name = xdescr;
                    tabdet.Y_axis_name = ydescr;
                    tabdet.Z_axis_name = zdescr;
                    //tabdet.Map_sramaddress = GetSymbolAddressSRAM(SymbolName);
                    int columns = cols;
                    int tablewidth = cols;
                    int address = Convert.ToInt32(SymbolAddress);
                    if (address != 0)
                    {
                        tabdet.Map_address = address;
                        int length = SymbolLength;
                        tabdet.Map_length = length;
                        byte[] mapdata = FileTools.Instance.readdatafromfile(Filename, address, length, false);
                        byte[] Orimapdata = FileTools.Instance.readdatafromfile(Filename, address, length, false);
                        byte[] mapdata2 = FileTools.Instance.readdatafromfile(FileTools.Instance.Currentfile, (int)Helpers.Instance.GetSymbolAddress(_workingFile.Symbols, SymbolName), Helpers.Instance.GetSymbolLength(_workingFile.Symbols, SymbolName), false);
                        if (mapdata.Length == mapdata2.Length)
                        {

                            tabdet.Map_original_content = Orimapdata;
                            tabdet.Map_compare_content = mapdata2;
                            tabdet.UseNewCompare = true;
                            for (int bt = 0; bt < mapdata2.Length; bt++)
                            {
                                //Console.WriteLine("Byte diff: " + mapdata.GetValue(bt).ToString() + " - " + mapdata2.GetValue(bt).ToString() + " = " + (byte)Math.Abs(((byte)mapdata.GetValue(bt) - (byte)mapdata2.GetValue(bt))));
                                mapdata.SetValue((byte)Math.Abs(((byte)mapdata.GetValue(bt) - (byte)mapdata2.GetValue(bt))), bt);
                            }


                            tabdet.Map_content = mapdata;
                            tabdet.Correction_factor = Helpers.Instance.GetMapCorrectionFactor(tabdet.Map_name);
                            //tabdet.Correction_offset = GetMapCorrectionOffset(tabdet.Map_name);
                            tabdet.IsUpsideDown = true;
                            tabdet.ShowTable(columns, false);
                            tabdet.Dock = DockStyle.Fill;
                            //tabdet.onSymbolSave += new MapViewer.NotifySaveSymbol(tabdet_onSymbolSave);
                            tabdet.onClose += new IMapViewer.ViewerClose(tabdet_onClose);
                            tabdet.onSelectionChanged += new IMapViewer.SelectionChanged(tabdet_onSelectionChanged);
                            tabdet.onSurfaceGraphViewChangedEx += new IMapViewer.SurfaceGraphViewChangedEx(mv_onSurfaceGraphViewChangedEx);
                            tabdet.onSurfaceGraphViewChanged += new IMapViewer.SurfaceGraphViewChanged(mv_onSurfaceGraphViewChanged);

                            //tabdet.onAxisLock += new MapViewer.NotifyAxisLock(tabdet_onAxisLock);
                            //tabdet.onSliderMove += new MapViewer.NotifySliderMove(tabdet_onSliderMove);
                            //tabdet.onSelectionChanged += new MapViewer.SelectionChanged(tabdet_onSelectionChanged);
                            //tabdet.onSplitterMoved += new MapViewer.SplitterMoved(tabdet_onSplitterMoved);
                            //tabdet.onSurfaceGraphViewChanged += new MapViewer.SurfaceGraphViewChanged(tabdet_onSurfaceGraphViewChanged);
                            //tabdet.onGraphSelectionChanged += new MapViewer.GraphSelectionChanged(tabdet_onGraphSelectionChanged);
                            //tabdet.onViewTypeChanged += new MapViewer.ViewTypeChanged(tabdet_onViewTypeChanged);


                            //dockPanel.DockAsTab(dockPanel1);
                            dockPanel.Text = "Symbol difference: " + SymbolName + " [" + Path.GetFileName(Filename) + "]";

                            dockPanel.DockTo(dockManager1, DevExpress.XtraBars.Docking.DockingStyle.Right, 0);
                            if (tabdet.X_axisvalues.Length > 0)
                            {
                                dockPanel.Width = 30 + ((tabdet.X_axisvalues.Length + 1) * 35);
                            }
                            else
                            {
                                //dockPanel.Width = this.Width - dockSymbols.Width - 10;

                            }

                            if (dockPanel.Width < 400) dockPanel.Width = 400;

                            dockPanel.Controls.Add(tabdet);
                        }
                        else
                        {
                            frmInfoBox info = new frmInfoBox("Map lengths don't match...");
                        }
                    }
                }
                catch (Exception E)
                {

                    Console.WriteLine(E.Message);
                }
                dockManager1.EndUpdate();
            }
        }

        void tabdet_onClose(object sender, EventArgs e)
        {
            // close the corresponding dockpanel
            if (sender is IMapViewer)
            {
                IMapViewer tabdet = (IMapViewer)sender;


                string dockpanelname = "Symbol: " + tabdet.Map_name + " [" + Path.GetFileName(tabdet.Filename) + "]";
                string dockpanelname2 = "SRAM Symbol: " + tabdet.Map_name + " [" + Path.GetFileName(tabdet.Filename) + "]";
                string dockpanelname3 = "Symbol difference: " + tabdet.Map_name + " [" + Path.GetFileName(tabdet.Filename) + "]";
                string dockpanelname4 = "Axis: " + tabdet.Map_name + " [" + Path.GetFileName(tabdet.Filename) + "]";
                foreach (DevExpress.XtraBars.Docking.DockPanel dp in dockManager1.Panels)
                {
                    if (dp.Text == dockpanelname)
                    {
                        dockManager1.RemovePanel(dp);
                        break;
                    }
                    else if (dp.Text == dockpanelname2)
                    {
                        dockManager1.RemovePanel(dp);
                        break;
                    }
                    else if (dp.Text == dockpanelname3)
                    {
                        dockManager1.RemovePanel(dp);
                        break;
                    }
                    else if (dp.Text == dockpanelname4)
                    {
                        dockManager1.RemovePanel(dp);
                        break;
                    }
                }

            }
        }

        private void HighlightMapInGridView(SymbolHelper shToShow)
        {
            try
            {
                //gridViewSymbols.ActiveFilter.Clear(); // clear filter
                SymbolCollection sc = (SymbolCollection)gridControl1.DataSource;
                int rtel = 0;
                foreach (SymbolHelper sh in sc)
                {
                    string varname1 = sh.Varname;
                    string varname2 = shToShow.Varname;
                    if (sh.UserDescription != string.Empty) varname1 = sh.UserDescription;
                    if (shToShow.UserDescription != string.Empty) varname2 = shToShow.UserDescription;

                    if (varname1 == varname2)
                    {
                        try
                        {
                            int rhandle = gridViewSymbols.GetRowHandle(rtel);
                            gridViewSymbols.OptionsSelection.MultiSelect = true;
                            gridViewSymbols.OptionsSelection.MultiSelectMode = DevExpress.XtraGrid.Views.Grid.GridMultiSelectMode.RowSelect;
                            gridViewSymbols.ClearSelection();
                            gridViewSymbols.SelectRow(rhandle);
                            gridViewSymbols.MakeRowVisible(rhandle, true);
                            gridViewSymbols.FocusedRowHandle = rhandle;
                            break;
                        }
                        catch (Exception E)
                        {
                            Console.WriteLine(E.Message);
                        }
                    }

                    rtel++;
                }
            }
            catch (Exception E2)
            {
                Console.WriteLine(E2.Message);
            }
        }

        private void StartTableViewer(SymbolHelper sh)
        {
            int cols = 1;
            int rows = 1;//<GS-21022011>
            DevExpress.XtraBars.Docking.DockPanel dockPanel;
            dockManager1.BeginUpdate();
            try
            {
                IMapViewer tabdet ;

                HighlightMapInGridView(sh);


                if (m_appSettings.UseNewMapviewer) tabdet = new MapViewerEx();
                else tabdet = new MapViewer();
                tabdet.SetViewSize(ViewSize.NormalView);
                tabdet.mapSymbolCollection = _workingFile.Symbols;
                tabdet.Visible = false;
                tabdet.Filename = FileTools.Instance.Currentfile;
                tabdet.GraphVisible = true;
                tabdet.Viewtype = ViewType.Easy;
                tabdet.DisableColors = false;
                tabdet.AutoSizeColumns = true;
                tabdet.IsRedWhite = false;

                tabdet.Map_name = sh.Varname;// address.ToString("X6");
                if (sh.UserDescription != string.Empty) tabdet.Map_name = sh.UserDescription;
                tabdet.Map_descr = tabdet.Map_name;
                tabdet.Map_cat = XDFCategories.Undocumented;
                tabdet.AllAxis = _workingFile.Axis;
                tabdet.XAxisSymbol = Helpers.Instance.GetXaxisSymbol(FileTools.Instance.Currentfile, _workingFile.Symbols, _workingFile.Axis, sh.Varname, sh.Flash_start_address);
                tabdet.YAxisSymbol = Helpers.Instance.GetYAxisSymbol(FileTools.Instance.Currentfile, _workingFile.Symbols, _workingFile.Axis, sh.Varname, sh.Flash_start_address);
                /*if (tabdet.XAxisSymbol.Flash_start_address == 0 && sh.Length == 8)
                {
                    tabdet.XAxisSymbol = Helpers.Instance.GetXaxisSymbol(FileTools.Instance.Currentfile, _workingFile.Symbols, _workingFile.Axis, "Overboost map", Helpers.Instance.GetSymbolAddress(_workingFile.Symbols, "Overboost map"));
                }*/
                tabdet.CurrentFiletype = FileTools.Instance.CurrentFiletype;
                int[] xvals = new int[1];
                int[] yvals = new int[1];
                string xdescr = "";
                string ydescr = "";
                if (sh.X_axis_address > 0 && sh.Y_axis_address > 0)
                {
                    //<GS-22032011>
                    if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC44)
                    {
                        // get x and y axis
                        foreach (AxisHelper ahx in _workingFile.Axis)
                        {
                            if (ahx.Addressinfile == sh.X_axis_address /*&& sh.Cols == ahx.Length*/)
                            {
                                cols = sh.Cols;// ahx.Length;
                                xdescr = ahx.Descr;
                                //ahx.CalculateRealValues();
                                //xvals = ahx.CalculcatedIntValues;
                                xvals = ahx.Values;
                                SymbolHelper shx = new SymbolHelper();
                                shx.Varname = ahx.Descr;
                                shx.X_axis_address = ahx.Addressinfile;
                                shx.Flash_start_address = ahx.Addressinfile;
                                tabdet.XAxisSymbol = shx;
                                break;
                            }
                        }
                        foreach (AxisHelper ahy in _workingFile.Axis)
                        {
                            if (ahy.Addressinfile == sh.Y_axis_address /*&& sh.Rows == ahy.Length*/)
                            {
                                rows = sh.Rows;// ahy.Length;
                                ydescr = ahy.Descr;
                                //ahy.CalculateRealValues();
                                
                                //yvals = ahy.CalculcatedIntValues;
                                yvals = ahy.Values;
                                SymbolHelper shy = new SymbolHelper();
                                shy.Varname = ahy.Descr;
                                shy.Y_axis_address = ahy.Addressinfile;
                                shy.Flash_start_address = ahy.Addressinfile;
                                tabdet.YAxisSymbol = shy;

                                break;
                            }
                        }
                    }
                    else
                    {
                        // if already given by XDF file
                        cols = sh.X_axis_length;
                        rows = sh.Y_axis_length;
                        xdescr = sh.XDescr;
                        ydescr = sh.YDescr;
                        AxisHelper ahx = new AxisHelper();
                        ahx.Values = FileTools.Instance.readdatafromfileasint(FileTools.Instance.Currentfile, sh.X_axis_address, sh.X_axis_length);
                        ahx.Length = cols;
                        if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC44)
                        {
                            ahx.IsMotronic44 = true;
                        }
                        else if (FileTools.Instance.CurrentFiletype == FileType.LH242)
                        {
                            ahx.IsLH242 = true;
                        }
                        else if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC210)
                        {
                            ahx.IsM210 = true;
                        }

                        ahx.CalculateRealValues();
                        xvals = ahx.Values; // <GS-01022011>
                        AxisHelper ahy = new AxisHelper();
                        ahy.Values = FileTools.Instance.readdatafromfileasint(FileTools.Instance.Currentfile, sh.Y_axis_address, sh.Y_axis_length);
                        ahy.Length = rows;
                        if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC44)
                        {
                            ahy.IsMotronic44 = true;
                        }
                        else if (FileTools.Instance.CurrentFiletype == FileType.LH242)
                        {
                            ahy.IsLH242 = true;
                        }
                        else if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC210)
                        {
                            ahy.IsM210 = true;
                        }

                        ahy.CalculateRealValues();
                        yvals = ahy.Values; //<GS-01022011>
                    }
                }
                else
                {
                    Helpers.Instance.GetAxisValues(FileTools.Instance.Currentfile, _workingFile.Symbols, _workingFile.Axis, tabdet.Map_name, sh.Flash_start_address, rows, cols, out xvals, out yvals, out xdescr, out ydescr);
                }
                
                tabdet.X_axisvalues = xvals;
                tabdet.Y_axisvalues = yvals;
                tabdet.X_axis_name = xdescr;
                tabdet.Y_axis_name = ydescr;
                if (sh.XDescr != string.Empty) tabdet.X_axis_name = sh.XDescr;
                if (sh.YDescr != string.Empty) tabdet.Y_axis_name = sh.YDescr;
                tabdet.Z_axis_name = sh.ZDescr;
                if (xvals.Length == 1 && yvals.Length == 1)
                {
                    cols = sh.Cols;
                    rows = sh.Rows;
                }
                else
                {
                    cols = xvals.Length;
                    rows = yvals.Length;
                }
                /*if (xvals.Length == 1 && yvals.Length == 1 && sh.Length == 8)
                {
                    Helpers.Instance.GetAxisValues(FileTools.Instance.Currentfile, _workingFile.Symbols, _workingFile.Axis, "Overboost map", Helpers.Instance.GetSymbolAddress(_workingFile.Symbols, "Overboost map"), rows, cols, out xvals, out yvals, out xdescr, out ydescr);
                    tabdet.X_axisvalues = xvals;
                    tabdet.X_axis_name = xdescr;
                    cols = 8;
                    rows = 1;
                }*/
                dockPanel = dockManager1.AddPanel(DevExpress.XtraBars.Docking.DockingStyle.Right);

                int dw = 650;
                if (tabdet.X_axisvalues.Length > 0)
                {
                    dw = 30 + ((tabdet.X_axisvalues.Length + 1) * 35);
                }
                else
                {
                    dw = 30 + ((cols + 1) * 35);
                }
                if (dw < 650) dw = 650;
                dockPanel.FloatSize = new Size(dw, 900);
                dockPanel.Width = dw;
                dockPanel.Tag = FileTools.Instance.Currentfile;

                string zdescr = string.Empty;
                //GetAxisDescriptions(FileTools.Instance.Currentfile, _workingFile.Symbols, tabdet.Map_name, out xdescr, out ydescr, out zdescr);
                //tabdet.X_axis_name = xdescr;
                ////.Y_axis_name = ydescr;
                //tabdet.Z_axis_name = zdescr;
                int columns = cols;
                int tablewidth = cols;
                int sramaddress = 0;
                if (sh.Flash_start_address != 0)
                {

                    tabdet.Map_address = sh.Flash_start_address;
                    tabdet.Map_sramaddress = 0;

                    int length = /*sh.Length;*/rows * cols; //<GS-21022011>
                    tabdet.Map_length = length;
                    byte[] mapdata = FileTools.Instance.readdatafromfile(FileTools.Instance.Currentfile, sh.Flash_start_address, length, sh.IsSixteenbits);
                    tabdet.Map_content = mapdata;
                    tabdet.mapAllowsNegatives = sh.MapAllowsNegatives;

                    //tabdet.Correction_factor = Helpers.Instance.GetMapCorrectionFactor(tabdet.Map_name);
                    //tabdet.Correction_offset = Helpers.Instance.GetMapCorrectionOffset(tabdet.Map_name);

                    tabdet.Correction_factor = _workingFile.GetCorrectionFactorForMap(tabdet.Map_name);//Helpers.Instance.GetMapCorrectionFactor(tabdet.Map_name);
                    tabdet.Correction_offset = _workingFile.GetOffsetForMap(tabdet.Map_name); //Helpers.Instance.GetMapCorrectionOffset(tabdet.Map_name);

                    tabdet.IsUpsideDown = true;
                    tabdet.ShowTable(cols, sh.IsSixteenbits);
                    tabdet.Dock = DockStyle.Fill;
                    tabdet.onSymbolSave += new IMapViewer.NotifySaveSymbol(tabdet_onSymbolSave);
                    tabdet.onClose += new IMapViewer.ViewerClose(tabdet_onClose);
                    tabdet.onSelectionChanged += new IMapViewer.SelectionChanged(tabdet_onSelectionChanged);
                    tabdet.onSurfaceGraphViewChangedEx += new IMapViewer.SurfaceGraphViewChangedEx(mv_onSurfaceGraphViewChangedEx);
                    tabdet.onSurfaceGraphViewChanged += new IMapViewer.SurfaceGraphViewChanged(mv_onSurfaceGraphViewChanged);
                    tabdet.onAxisEditorRequested += new IMapViewer.AxisEditorRequested(tabdet_onAxisEditorRequested);
                    //tabdet.onAxisLock += new MapViewer.NotifyAxisLock(tabdet_onAxisLock);
                    //tabdet.onSliderMove += new MapViewer.NotifySliderMove(tabdet_onSliderMove);
                    //tabdet.onSelectionChanged += new MapViewer.SelectionChanged(tabdet_onSelectionChanged);
                    //tabdet.onSplitterMoved += new MapViewer.SplitterMoved(tabdet_onSplitterMoved);
                    //tabdet.onSurfaceGraphViewChanged += new MapViewer.SurfaceGraphViewChanged(tabdet_onSurfaceGraphViewChanged);
                    //tabdet.onGraphSelectionChanged += new MapViewer.GraphSelectionChanged(tabdet_onGraphSelectionChanged);
                    //tabdet.onViewTypeChanged += new MapViewer.ViewTypeChanged(tabdet_onViewTypeChanged);
                    //tabdet.onAxisEditorRequested += new MapViewer.AxisEditorRequested(tabdet_onAxisEditorRequested);
                    //tabdet.onReadFromSRAM += new MapViewer.ReadDataFromSRAM(tabdet_onReadFromSRAM);
                    //tabdet.onWriteToSRAM += new MapViewer.WriteDataToSRAM(tabdet_onWriteToSRAM);
                    //dockPanel.DockAsTab(dockPanel1);
                    dockPanel.Text = "Symbol: " + tabdet.Map_name + " [" + Path.GetFileName(FileTools.Instance.Currentfile) + "]";
                    dockPanel.Controls.Add(tabdet);
                }
                tabdet.Visible = true;
            }
            catch (Exception newdockE)
            {
                Console.WriteLine(newdockE.Message);
            }
            dockManager1.EndUpdate();

            System.Windows.Forms.Application.DoEvents();
        }

        private void StartAxisViewer(AxisHelper ah)
        {
            
            DevExpress.XtraBars.Docking.DockPanel dockPanel;
            dockManager1.BeginUpdate();
            try
            {
                
                dockPanel = dockManager1.AddPanel(DevExpress.XtraBars.Docking.DockingStyle.Right);
                int dw = 650;
                dockPanel.FloatSize = new Size(dw, 900);
                dockPanel.Width = dw;
                dockPanel.Tag = FileTools.Instance.Currentfile;
                ctrlAxisEditor tabdet = new ctrlAxisEditor();
                tabdet.FileName = FileTools.Instance.Currentfile;
                tabdet.AxisID = ah.Identifier;
                tabdet.AxisAddress = ah.Addressinfile;
                tabdet.Map_name = ah.Addressinfile.ToString("X4");
                tabdet.SetData(ah.CalculcatedValues);
                dockPanel.Text = "Axis: " + tabdet.Map_name + " [" + Path.GetFileName(FileTools.Instance.Currentfile) + "]";
                tabdet.onClose += new ctrlAxisEditor.ViewerClose(axis_Close);
                tabdet.onSave += new ctrlAxisEditor.DataSave(axis_Save);
                tabdet.Dock = DockStyle.Fill;
                dockPanel.Controls.Add(tabdet);
            }
            catch (Exception newdockE)
            {
                Console.WriteLine(newdockE.Message);
            }
            dockManager1.EndUpdate();

            System.Windows.Forms.Application.DoEvents();
        }

        void axis_Save(object sender, EventArgs e)
        {
            if (sender is ctrlAxisEditor)
            {
                ctrlAxisEditor editor = (ctrlAxisEditor)sender;
                // recalculate the values back and store it in the file at the correct location
                float[] newvalues = editor.GetData();
                // well.. recalculate the data based on these new values
                AxisHelper ahhelp = new AxisHelper();
                ahhelp.Identifier = editor.AxisID;
                ahhelp.IsLH242 = false;
                ahhelp.IsMotronic44 = false; // TODO: fix for M4.4 as well
                ahhelp.Length = newvalues.Length;
                ahhelp.Addressinfile = editor.AxisAddress;
                int[] orivalues = ahhelp.CalculateOriginalValues(newvalues);
                byte[] borivalues = new byte[orivalues.Length];
                for (int i = 0; i < orivalues.Length; i++)
                {
                    borivalues.SetValue(Convert.ToByte(orivalues.GetValue(i)), i);
                }
                FileTools.Instance.savedatatobinary(ahhelp.Addressinfile + 2, ahhelp.Length, borivalues, editor.FileName);

                // now we need to reload the axis map with this stuff... 
                foreach (AxisHelper ah in _workingFile.Axis)
                {
                    if (ah.Addressinfile == ahhelp.Addressinfile)
                    {
                        ah.Values = orivalues;
                        ah.CalculateRealValues();
                        break;
                    }
                }
                // and we need to update mapviewers maybe?
                UpdateOpenViewers();
            }
        }

        private void UpdateViewer(IMapViewer tabdet)
        {
            int cols = 1;
            int rows = 1;
            string mapname = tabdet.Map_name;
            foreach (SymbolHelper sh in _workingFile.Symbols)
            {
                if (sh.Varname == mapname)
                {
                    // refresh data and axis in the viewer
                    tabdet.XAxisSymbol = Helpers.Instance.GetXaxisSymbol(FileTools.Instance.Currentfile, _workingFile.Symbols, _workingFile.Axis, sh.Varname, sh.Flash_start_address);
                    tabdet.YAxisSymbol = Helpers.Instance.GetYAxisSymbol(FileTools.Instance.Currentfile, _workingFile.Symbols, _workingFile.Axis, sh.Varname, sh.Flash_start_address);
                    if (tabdet.XAxisSymbol.Flash_start_address == 0 && sh.Length == 8)
                    {
                        tabdet.XAxisSymbol = Helpers.Instance.GetXaxisSymbol(FileTools.Instance.Currentfile, _workingFile.Symbols, _workingFile.Axis, "Overboost map", Helpers.Instance.GetSymbolAddress(_workingFile.Symbols, "Overboost map"));
                    }
                    tabdet.CurrentFiletype = FileTools.Instance.CurrentFiletype;
                    int[] xvals;
                    int[] yvals;
                    string xdescr = "";
                    string ydescr = "";
                    Helpers.Instance.GetAxisValues(FileTools.Instance.Currentfile, _workingFile.Symbols, _workingFile.Axis, tabdet.Map_name, sh.Flash_start_address, rows, cols, out xvals, out yvals, out xdescr, out ydescr);
                    tabdet.X_axisvalues = xvals;
                    tabdet.Y_axisvalues = yvals;
                    tabdet.X_axis_name = xdescr;
                    tabdet.Y_axis_name = ydescr;
                    if (sh.XDescr != string.Empty) tabdet.X_axis_name = sh.XDescr;
                    if (sh.YDescr != string.Empty) tabdet.Y_axis_name = sh.YDescr;
                    tabdet.Z_axis_name = sh.ZDescr;
                    if (xvals.Length == 1 && yvals.Length == 1)
                    {
                        cols = sh.Cols;
                        rows = sh.Rows;
                    }
                    else
                    {
                        cols = xvals.Length;
                        rows = yvals.Length;
                    }
                    if (xvals.Length == 1 && yvals.Length == 1 && sh.Length == 8)
                    {
                        Helpers.Instance.GetAxisValues(FileTools.Instance.Currentfile, _workingFile.Symbols, _workingFile.Axis, "Overboost map", Helpers.Instance.GetSymbolAddress(_workingFile.Symbols, "Overboost map"), rows, cols, out xvals, out yvals, out xdescr, out ydescr);
                        tabdet.X_axisvalues = xvals;
                        tabdet.X_axis_name = xdescr;
                        cols = 8;
                        rows = 1;
                    }
                    string zdescr = string.Empty;
                    int columns = cols;
                    int tablewidth = cols;
                    if (sh.Flash_start_address != 0)
                    {
                        tabdet.Map_address = sh.Flash_start_address;
                        tabdet.Map_sramaddress = 0;
                        int length = /*sh.Length;*/rows * cols; //<GS-21022011>
                        tabdet.Map_length = length;
                        byte[] mapdata = FileTools.Instance.readdatafromfile(FileTools.Instance.Currentfile, sh.Flash_start_address, length, sh.IsSixteenbits);
                        tabdet.Map_content = mapdata;
                        //tabdet.Correction_factor = Helpers.Instance.GetMapCorrectionFactor(tabdet.Map_name);
                        //tabdet.Correction_offset = Helpers.Instance.GetMapCorrectionOffset(tabdet.Map_name);
                        tabdet.Correction_factor = _workingFile.GetCorrectionFactorForMap(tabdet.Map_name);//Helpers.Instance.GetMapCorrectionFactor(tabdet.Map_name);
                        tabdet.Correction_offset = _workingFile.GetOffsetForMap(tabdet.Map_name); //Helpers.Instance.GetMapCorrectionOffset(tabdet.Map_name);

                        tabdet.IsUpsideDown = true;
                        tabdet.ShowTable(cols, sh.IsSixteenbits);
                    }
                    break;
                }
            }
        }

        private void UpdateOpenViewers()
        {

            try
            {
                // convert feedback map in memory to byte[] in stead of float[]
                foreach (DevExpress.XtraBars.Docking.DockPanel pnl in dockManager1.Panels)
                {
                    if (pnl.Text.StartsWith("Symbol: "))
                    {
                        foreach (Control c in pnl.Controls)
                        {
                            if (c is IMapViewer)
                            {
                                IMapViewer vwr = (IMapViewer)c;
                                UpdateViewer(vwr);
                            }
                            else if (c is DevExpress.XtraBars.Docking.DockPanel)
                            {
                                DevExpress.XtraBars.Docking.DockPanel tpnl = (DevExpress.XtraBars.Docking.DockPanel)c;
                                foreach (Control c2 in tpnl.Controls)
                                {
                                    if (c2 is IMapViewer)
                                    {
                                        IMapViewer vwr2 = (IMapViewer)c2;
                                        UpdateViewer(vwr2);
                                    }
                                }
                            }
                            else if (c is DevExpress.XtraBars.Docking.ControlContainer)
                            {
                                DevExpress.XtraBars.Docking.ControlContainer cntr = (DevExpress.XtraBars.Docking.ControlContainer)c;
                                foreach (Control c3 in cntr.Controls)
                                {
                                    if (c3 is IMapViewer)
                                    {
                                        IMapViewer vwr3 = (IMapViewer)c3;
                                        UpdateViewer(vwr3);
                                    }
                                }
                            }
                        }

                    }
                }
            }
            catch (Exception E)
            {
                Console.WriteLine("Refresh viewer error: " + E.Message);
            }
        }

        void axis_Close(object sender, EventArgs e)
        {
            tabdet_onClose(sender, EventArgs.Empty); // recast
        }

        void tabdet_onAxisEditorRequested(object sender, IMapViewer.AxisEditorRequestedEventArgs e)
        {
            // allow axis editing
            string x = string.Empty;
            string y = string.Empty;

            foreach (SymbolHelper sh in _workingFile.Symbols)
            {
                if (sh.Varname == e.Mapname)
                {
                    // get the axis
                    foreach (AxisHelper ah in _workingFile.Axis)
                    {
                        int endaddress = ah.Addressinfile + ah.Length + 2;
                        if (endaddress == sh.Flash_start_address)
                        {
                            // this is an axis for this table... 
                            // see if there is another one that leads 
                            int newaddress = 0;
                            if (Helpers.Instance.AxisHasLeadingAxis(_workingFile.Axis, ah.Addressinfile, out newaddress))
                            {
                                x = newaddress.ToString("X4");
                            }
                            else
                            {
                                x = ah.Addressinfile.ToString("X4");
                            }
                        }
                    }
                    foreach (AxisHelper ah in _workingFile.Axis)
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
            if (e.Axisident == IMapViewer.AxisIdent.X_Axis)
            {
                StartAxisEditor(e.Filename, y); 
            }
            else if (e.Axisident == IMapViewer.AxisIdent.Y_Axis)
            {
                StartAxisEditor(e.Filename, x); 
            }
        }

        private void StartAxisEditor(string filename, string axisaddress)
        {
            foreach (AxisHelper ah in _workingFile.Axis)
            {
                if (ah.Addressinfile.ToString("X4") == axisaddress)
                {
                    // start axis editor
                    StartAxisViewer(ah);
                    break;
                }
            }
        }

        void mv_onSurfaceGraphViewChanged(object sender, IMapViewer.SurfaceGraphViewChangedEventArgs e)
        {
            if (m_appSettings.SynchronizeMapviewers)
            {
                foreach (DevExpress.XtraBars.Docking.DockPanel pnl in dockManager1.Panels)
                {
                    foreach (Control c in pnl.Controls)
                    {
                        if (c is IMapViewer)
                        {
                            if (c != sender)
                            {
                                IMapViewer vwr = (IMapViewer)c;
                                if (vwr.Map_name == e.Mapname || vwr.Map_name.StartsWith("Ignition map:") && e.Mapname.StartsWith("Ignition map:"))
                                {
                                    vwr.SetSurfaceGraphView(e.Pov_x, e.Pov_y, e.Pov_z, e.Pan_x, e.Pan_y, e.Pov_d);
                                    vwr.Invalidate();
                                }
                            }
                        }
                        else if (c is DevExpress.XtraBars.Docking.DockPanel)
                        {
                            DevExpress.XtraBars.Docking.DockPanel tpnl = (DevExpress.XtraBars.Docking.DockPanel)c;
                            foreach (Control c2 in tpnl.Controls)
                            {
                                if (c2 is IMapViewer)
                                {
                                    if (c2 != sender)
                                    {
                                        IMapViewer vwr2 = (IMapViewer)c2;
                                        if (vwr2.Map_name == e.Mapname || vwr2.Map_name.StartsWith("Ignition map:") && e.Mapname.StartsWith("Ignition map:"))
                                        {
                                            vwr2.SetSurfaceGraphView(e.Pov_x, e.Pov_y, e.Pov_z, e.Pan_x, e.Pan_y, e.Pov_d);
                                            vwr2.Invalidate();
                                        }
                                    }
                                }
                            }
                        }
                        else if (c is DevExpress.XtraBars.Docking.ControlContainer)
                        {
                            DevExpress.XtraBars.Docking.ControlContainer cntr = (DevExpress.XtraBars.Docking.ControlContainer)c;
                            foreach (Control c3 in cntr.Controls)
                            {
                                if (c3 is IMapViewer)
                                {
                                    if (c3 != sender)
                                    {
                                        IMapViewer vwr3 = (IMapViewer)c3;
                                        if (vwr3.Map_name == e.Mapname || vwr3.Map_name.StartsWith("Ignition map:") && e.Mapname.StartsWith("Ignition map:"))
                                        {
                                            vwr3.SetSurfaceGraphView(e.Pov_x, e.Pov_y, e.Pov_z, e.Pan_x, e.Pan_y, e.Pov_d);
                                            vwr3.Invalidate();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        void mv_onSurfaceGraphViewChangedEx(object sender, IMapViewer.SurfaceGraphViewChangedEventArgsEx e)
        {
            if (m_appSettings.SynchronizeMapviewers)
            {
                foreach (DevExpress.XtraBars.Docking.DockPanel pnl in dockManager1.Panels)
                {
                    foreach (Control c in pnl.Controls)
                    {
                        if (c is IMapViewer)
                        {
                            if (c != sender)
                            {
                                IMapViewer vwr = (IMapViewer)c;
                                if (vwr.Map_name == e.Mapname || vwr.Map_name.StartsWith("Ignition map:") && e.Mapname.StartsWith("Ignition map:"))
                                {
                                    vwr.SetSurfaceGraphViewEx(e.DepthX, e.DepthY, e.Zoom, e.Rotation, e.Elevation);
                                    vwr.Invalidate();
                                }
                            }
                        }
                        else if (c is DevExpress.XtraBars.Docking.DockPanel)
                        {
                            DevExpress.XtraBars.Docking.DockPanel tpnl = (DevExpress.XtraBars.Docking.DockPanel)c;
                            foreach (Control c2 in tpnl.Controls)
                            {
                                if (c2 is IMapViewer)
                                {
                                    if (c2 != sender)
                                    {
                                        IMapViewer vwr2 = (IMapViewer)c2;
                                        if (vwr2.Map_name == e.Mapname || vwr2.Map_name.StartsWith("Ignition map:") && e.Mapname.StartsWith("Ignition map:"))
                                        {
                                            vwr2.SetSurfaceGraphViewEx(e.DepthX, e.DepthY, e.Zoom, e.Rotation, e.Elevation);
                                            vwr2.Invalidate();
                                        }
                                    }
                                }
                            }
                        }
                        else if (c is DevExpress.XtraBars.Docking.ControlContainer)
                        {
                            DevExpress.XtraBars.Docking.ControlContainer cntr = (DevExpress.XtraBars.Docking.ControlContainer)c;
                            foreach (Control c3 in cntr.Controls)
                            {
                                if (c3 is IMapViewer)
                                {
                                    if (c3 != sender)
                                    {
                                        IMapViewer vwr3 = (IMapViewer)c3;
                                        if (vwr3.Map_name == e.Mapname || vwr3.Map_name.StartsWith("Ignition map:") && e.Mapname.StartsWith("Ignition map:"))
                                        {
                                            vwr3.SetSurfaceGraphViewEx(e.DepthX, e.DepthY, e.Zoom, e.Rotation, e.Elevation);
                                            vwr3.Invalidate();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        void tabdet_onSelectionChanged(object sender, IMapViewer.CellSelectionChangedEventArgs e)
        {
            //<GS-22042010>
            // sync mapviewers maybe?
            if (m_appSettings.SynchronizeMapviewers)
            {
                // andere cell geselecteerd, doe dat ook bij andere viewers met hetzelfde symbool (mapname)
                foreach (DevExpress.XtraBars.Docking.DockPanel pnl in dockManager1.Panels)
                {
                    foreach (Control c in pnl.Controls)
                    {
                        if (c is IMapViewer)
                        {
                            if (c != sender)
                            {
                                IMapViewer vwr = (IMapViewer)c;
                                if (vwr.Map_name == e.Mapname || vwr.Map_name.StartsWith("Ignition map:") && e.Mapname.StartsWith("Ignition map:"))
                                {
                                    vwr.SelectCell(e.Rowhandle, e.Colindex);
                                    vwr.Invalidate();
                                }
                            }
                        }
                        else if (c is DevExpress.XtraBars.Docking.DockPanel)
                        {
                            DevExpress.XtraBars.Docking.DockPanel tpnl = (DevExpress.XtraBars.Docking.DockPanel)c;
                            foreach (Control c2 in tpnl.Controls)
                            {
                                if (c2 is IMapViewer)
                                {
                                    if (c2 != sender)
                                    {
                                        IMapViewer vwr2 = (IMapViewer)c2;
                                        if (vwr2.Map_name == e.Mapname || vwr2.Map_name.StartsWith("Ignition map:") && e.Mapname.StartsWith("Ignition map:"))
                                        {
                                            vwr2.SelectCell(e.Rowhandle, e.Colindex);
                                            vwr2.Invalidate();
                                        }
                                    }
                                }
                            }
                        }
                        else if (c is DevExpress.XtraBars.Docking.ControlContainer)
                        {
                            DevExpress.XtraBars.Docking.ControlContainer cntr = (DevExpress.XtraBars.Docking.ControlContainer)c;
                            foreach (Control c3 in cntr.Controls)
                            {
                                if (c3 is IMapViewer)
                                {
                                    if (c3 != sender)
                                    {
                                        IMapViewer vwr3 = (IMapViewer)c3;
                                        if (vwr3.Map_name == e.Mapname || vwr3.Map_name.StartsWith("Ignition map:") && e.Mapname.StartsWith("Ignition map:"))
                                        {
                                            vwr3.SelectCell(e.Rowhandle, e.Colindex);
                                            vwr3.Invalidate();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        void tabdet_onSymbolSave(object sender, MapViewer.SaveSymbolEventArgs e)
        {
            if (sender is IMapViewer)
            {
                // juiste filename kiezen 

                IMapViewer tabdet = (IMapViewer)sender;

                string note = string.Empty;
                if (m_appSettings.RequestProjectNotes && FileTools.Instance.CurrentWorkingProject != "")
                {
                    //request a small note from the user in which he/she can denote a description of the change
                    frmChangeNote changenote = new frmChangeNote();
                    changenote.ShowDialog();
                    note = changenote.Note;
                }
                FileTools.Instance.savedatatobinary(e.SymbolAddress, e.SymbolLength, e.SymbolDate, e.Filename, true, note);
                //savedatatobinary(e.SymbolAddress, e.SymbolLength, e.SymbolDate, e.Filename);
                if (m_appSettings.AutoChecksum)
                {
                    UpdateCRC(FileTools.Instance.Currentfile);
                }
                tabdet.Map_content = FileTools.Instance.readdatafromfile(e.Filename, e.SymbolAddress, e.SymbolLength, false);
            }
        }

        private void StartTableViewer(string symbolname, int address, int cols, int rows, bool isSixteenbits)
        {

            DevExpress.XtraBars.Docking.DockPanel dockPanel;
            dockManager1.BeginUpdate();
            try
            {
                IMapViewer tabdet;
                if (m_appSettings.UseNewMapviewer) tabdet = new MapViewerEx();
                else tabdet = new MapViewer();
                tabdet.SetViewSize(ViewSize.NormalView);
                tabdet.Visible = false;
                tabdet.Filename = FileTools.Instance.Currentfile;
                tabdet.GraphVisible = true;
                tabdet.Viewtype = ViewType.Easy;
                tabdet.DisableColors = false;
                tabdet.AutoSizeColumns = true;
                tabdet.IsRedWhite = false;
                tabdet.Map_name = symbolname;// address.ToString("X6");
                tabdet.Map_descr = tabdet.Map_name;
                tabdet.Map_cat = XDFCategories.Undocumented;
                tabdet.AllAxis = _workingFile.Axis;
                tabdet.XAxisSymbol = Helpers.Instance.GetXaxisSymbol(FileTools.Instance.Currentfile, _workingFile.Symbols, _workingFile.Axis, symbolname, address);
                tabdet.YAxisSymbol = Helpers.Instance.GetYAxisSymbol(FileTools.Instance.Currentfile, _workingFile.Symbols, _workingFile.Axis, symbolname, address);

                tabdet.CurrentFiletype = FileTools.Instance.CurrentFiletype;

                int[] xvals;
                int[] yvals;
                string xdescr = "X axis";
                string ydescr = "Y axis";
                Helpers.Instance.GetAxisValues(FileTools.Instance.Currentfile, _workingFile.Symbols, _workingFile.Axis, tabdet.Map_name, address, rows, cols, out xvals, out yvals, out xdescr, out ydescr);
                tabdet.X_axisvalues = xvals;
                tabdet.Y_axisvalues = yvals;
                tabdet.X_axis_name = xdescr;
                tabdet.Y_axis_name = ydescr;
                dockPanel = dockManager1.AddPanel(DevExpress.XtraBars.Docking.DockingStyle.Right);
                
                    int dw = 650;
                    if (tabdet.X_axisvalues.Length > 0)
                    {
                        dw = 30 + ((tabdet.X_axisvalues.Length + 1) * 35);
                    }
                    if (dw < 400) dw = 400;
                    dockPanel.FloatSize = new Size(dw, 900);
                    dockPanel.Width = dw;
                dockPanel.Tag = FileTools.Instance.Currentfile;

                string zdescr = string.Empty;
                //GetAxisDescriptions(FileTools.Instance.Currentfile, _workingFile.Symbols, tabdet.Map_name, out xdescr, out ydescr, out zdescr);
                tabdet.X_axis_name = xdescr;
                tabdet.Y_axis_name = ydescr;
                tabdet.Z_axis_name = zdescr;
                int columns = cols;
                int tablewidth = cols;
                int sramaddress = 0;
                if (address != 0)
                {

                    tabdet.Map_address = address;
                    tabdet.Map_sramaddress = 0;
                    
                    int length = rows * cols;
                    tabdet.Map_length = length;
                    byte[] mapdata = FileTools.Instance.readdatafromfile(FileTools.Instance.Currentfile, address, length, isSixteenbits);
                    tabdet.Map_content = mapdata;

                    tabdet.Correction_factor = _workingFile.GetCorrectionFactorForMap(tabdet.Map_name);//Helpers.Instance.GetMapCorrectionFactor(tabdet.Map_name);
                    tabdet.Correction_offset = _workingFile.GetOffsetForMap(tabdet.Map_name); //Helpers.Instance.GetMapCorrectionOffset(tabdet.Map_name);
                    tabdet.IsUpsideDown = true;
                    tabdet.ShowTable(cols, isSixteenbits);
                    tabdet.Dock = DockStyle.Fill;
                    tabdet.onSymbolSave += new IMapViewer.NotifySaveSymbol(tabdet_onSymbolSave);
                    
                    tabdet.onClose += new IMapViewer.ViewerClose(tabdet_onClose);
                    tabdet.onSelectionChanged += new IMapViewer.SelectionChanged(tabdet_onSelectionChanged);
                    tabdet.onSurfaceGraphViewChangedEx += new IMapViewer.SurfaceGraphViewChangedEx(mv_onSurfaceGraphViewChangedEx);
                    tabdet.onSurfaceGraphViewChanged += new IMapViewer.SurfaceGraphViewChanged(mv_onSurfaceGraphViewChanged);

                    //tabdet.onAxisLock += new MapViewer.NotifyAxisLock(tabdet_onAxisLock);
                    //tabdet.onSliderMove += new MapViewer.NotifySliderMove(tabdet_onSliderMove);
                    //tabdet.onSelectionChanged += new MapViewer.SelectionChanged(tabdet_onSelectionChanged);
                    //tabdet.onSplitterMoved += new MapViewer.SplitterMoved(tabdet_onSplitterMoved);
                    //tabdet.onSurfaceGraphViewChanged += new MapViewer.SurfaceGraphViewChanged(tabdet_onSurfaceGraphViewChanged);
                    //tabdet.onGraphSelectionChanged += new MapViewer.GraphSelectionChanged(tabdet_onGraphSelectionChanged);
                    //tabdet.onViewTypeChanged += new MapViewer.ViewTypeChanged(tabdet_onViewTypeChanged);
                    //tabdet.onAxisEditorRequested += new MapViewer.AxisEditorRequested(tabdet_onAxisEditorRequested);
                    //tabdet.onReadFromSRAM += new MapViewer.ReadDataFromSRAM(tabdet_onReadFromSRAM);
                    //tabdet.onWriteToSRAM += new MapViewer.WriteDataToSRAM(tabdet_onWriteToSRAM);
                    //dockPanel.DockAsTab(dockPanel1);
                    dockPanel.Text = "Symbol: " + tabdet.Map_name + " [" + Path.GetFileName(FileTools.Instance.Currentfile) + "]";
                    dockPanel.Controls.Add(tabdet);
                }
                tabdet.Visible = true;
            }
            catch (Exception newdockE)
            {
                Console.WriteLine(newdockE.Message);
            }
            dockManager1.EndUpdate();

            System.Windows.Forms.Application.DoEvents();
        }

        private void StartTableViewerByName(string symbol)
        {
            foreach (SymbolHelper sh in _workingFile.Symbols)
            {
                if (sh.Varname == symbol)
                {
                    StartTableViewer(sh);
                }
            }
        }

        #endregion

        #region map and axis methods


        private void TestSymbolListIntegrety(SymbolCollection symbols, AxisCollection axis)
        {
            symbols.SortColumn = "Flash_start_address";
            symbols.SortingOrder = GenericComparer.SortOrder.Ascending;
            symbols.Sort();
            int prev_start_address = 0;
            foreach (SymbolHelper sh in symbols)
            {
                if (prev_start_address > 0)
                {

                    SymbolHelper xsym = Helpers.Instance.GetXaxisSymbol(FileTools.Instance.Currentfile, symbols, axis, sh.Varname, sh.Flash_start_address);
                    SymbolHelper ysym = Helpers.Instance.GetYAxisSymbol(FileTools.Instance.Currentfile, symbols, axis, sh.Varname, sh.Flash_start_address);
                    int diff = sh.Flash_start_address - prev_start_address - xsym.Length - 2 - ysym.Length - 2 - sh.Length;
                    
                    if (diff > 0)
                    {
                        Console.WriteLine(sh.Varname + " diff: " + diff.ToString());
                        Console.WriteLine("Possible breach in map detection algo: " + sh.Varname);
                    }
                }
                prev_start_address = sh.Flash_start_address;
            }

        }

        private void LoadSymbolTable(string filename)
        {
            FileTools.Instance.Speedlimit = 0;
            FileTools.Instance.Rpmlimit = 0;
            FileTools.Instance.Rpmlimit2 = 0;

            barFileTypeIndicator.Caption = "";
            SymbolTranslator st = new SymbolTranslator();
            string ht = string.Empty;
            string cat = string.Empty;
            string subcat = string.Empty;

            _workingFile.SelectFile(filename);
            _workingFile.ParseFile();
            TryToLoadAdditionalSymbols(filename, true, _workingFile.Symbols);
            foreach (SymbolHelper sh in _workingFile.Symbols)
            {
                sh.Description = st.TranslateSymbolToHelpText(sh.Varname, out ht, out cat, out subcat);
            }

            bool _containsExtraInfo = false;
            foreach (SymbolHelper sh in _workingFile.Symbols)
            {
                if (sh.UserDescription != string.Empty) _containsExtraInfo = true;
            }
            if (!_containsExtraInfo)
            {
                foreach (SymbolHelper sh in _workingFile.Symbols)
                {
                    sh.UserDescription = sh.Varname;
                }
                SaveAdditionalSymbols(); // Done to preserve initially detected maps.
            }
            

            /*
            if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC43)
            {
                LoadMotronic43File(filename, out _workingFile.Symbols, out _workingFile.Axis);

                foreach (SymbolHelper sh in _workingFile.Symbols)
                {
                    sh.Description = st.TranslateSymbolToHelpText(sh.Varname, out ht, out cat, out subcat);
                }
                TryToLoadAdditionalSymbols(filename, true, _workingFile.Symbols);
                gridControl1.DataSource = _workingFile.Symbols;
                gridControl2.DataSource = _workingFile.Axis;
                OpenGridViewGroups(gridControl1, 0);
                gridViewSymbols.BestFitColumns();
                barFileTypeIndicator.Caption = "M4.3";
            }
            else if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC44)
            {
                LoadMotronic44File(filename, out _workingFile.Symbols, out _workingFile.Axis);
                TryToLoadAdditionalSymbols(filename, true, _workingFile.Symbols);
                gridControl1.DataSource = _workingFile.Symbols;
                gridControl2.DataSource = _workingFile.Axis;
                OpenGridViewGroups(gridControl1, 0);
                gridViewSymbols.BestFitColumns();
                barFileTypeIndicator.Caption = "M4.4";
            }
            else if (FileTools.Instance.CurrentFiletype == FileType.LH24)
            {
                LoadLH24File(filename, out _workingFile.Symbols, out _workingFile.Axis);
                TryToLoadAdditionalSymbols(filename, true, _workingFile.Symbols);
                gridControl1.DataSource = _workingFile.Symbols;
                OpenGridViewGroups(gridControl1, 0);
                gridViewSymbols.BestFitColumns();
                // find 0x00 0x08 0x018 in file 0x20 0x28 0x2E 0x32 to find the addresstable
                barFileTypeIndicator.Caption = "LH2.4";
            }
            else if (FileTools.Instance.CurrentFiletype == FileType.LH242)
            {
                LoadLH242File(filename, out _workingFile.Symbols, out _workingFile.Axis);
                TryToLoadAdditionalSymbols(filename, true, _workingFile.Symbols);
                gridControl1.DataSource = _workingFile.Symbols;
                OpenGridViewGroups(gridControl1, 0);
                gridViewSymbols.BestFitColumns();
                // find 0x00 0x08 0x018 in file 0x20 0x28 0x2E 0x32 to find the addresstable
                barFileTypeIndicator.Caption = "LH2.4.2";
            }*/

        }

        /*
        private int LookupMotronic43RpmPointer()
        {
            int readstate = 0;
            readstate = 0;
            int indexInFile = 0;
            FileStream fs = new FileStream(FileTools.Instance.Currentfile, FileMode.Open);

            using (BinaryReader br = new BinaryReader(fs))
            {
                for (int t = 0; t < fs.Length; t++)
                {
                    byte b = br.ReadByte();
                    switch (readstate)
                    {
                        case 0:
                            if (b == 0x74) readstate++;
                            break;
                        case 1:
                            if (b == 0x04) readstate++; // second one found
                            else readstate = 0;
                            break;
                        case 2:
                            if (b == 0xB5) readstate++;
                            else readstate = 0;
                            break;
                        case 3:
                            if (b == 0x3E) readstate++;
                            else readstate = 0;
                            break;
                        case 4:
                            if (b == 0x05) readstate++;
                            else readstate = 0;
                            break;
                        case 5:
                            if (b == 0x74) readstate++;
                            else readstate = 0;
                            break;
                        case 6:
                            if (b == 0x86) readstate++;
                            else readstate = 0;
                            break;
                        case 7:
                            if (b == 0xB5) readstate++;
                            else readstate = 0;
                            break;
                        case 8:
                            if (b == 0x3D) readstate++;
                            else readstate = 0;
                            break;
                        case 9:
                            if (b == 0x00)
                            {
                                readstate++; // third one found
                                indexInFile = Convert.ToInt32(fs.Position);
                                indexInFile -= 12;
                            }
                            break;
                        default:
                            break;
                    }
                }
                if (indexInFile > 0)
                {
                    fs.Position = indexInFile;
                    byte b1 = br.ReadByte();
                    byte b2 = br.ReadByte();
                    indexInFile = Convert.ToInt32(b1) * 256 + Convert.ToInt32(b2);
                    return indexInFile;
                }
            }
            fs.Close();
            fs.Dispose();
            return indexInFile;
        }

        private int LookupMotronic43SpeedPointer()
        {
            int readstate = 0;
            readstate = 0;
            int indexInFile = 0;
            FileStream fs = new FileStream(FileTools.Instance.Currentfile, FileMode.Open);

            using (BinaryReader br = new BinaryReader(fs))
            {
                for (int t = 0; t < fs.Length; t++)
                {
                    byte b = br.ReadByte();
                    switch (readstate)
                    {
                        case 0:
                            if (b == 0xE4) readstate++;
                            break;
                        case 1:
                            if (b == 0x93) readstate++; // second one found
                            else readstate = 0;
                            break;
                        case 2:
                            if (b == 0x87) readstate++;
                            else readstate = 0;
                            break;
                        case 3:
                            if (b == 0xF0) readstate++;
                            else readstate = 0;
                            break;
                        case 4:
                            if (b == 0xB5) readstate++;
                            else readstate = 0;
                            break;
                        case 5:
                            if (b == 0xF0) readstate++;
                            else readstate = 0;
                            break;
                        case 6:
                            if (b == 0x00) readstate++;
                            else readstate = 0;
                            break;
                        case 7:
                            if (b == 0x92) readstate++;
                            else readstate = 0;
                            break;
                        case 8:
                            if (b == 0x50) readstate++;
                            else readstate = 0;
                            break;
                        case 9:
                            if (b == 0x22) readstate++;
                            else readstate = 0;
                            break;
                        case 10:
                            if (b == 0x75) readstate++;
                            else readstate = 0;
                            break;
                        case 11:
                            if (b == 0xA0)
                            {
                                readstate++; // third one found
                                indexInFile = Convert.ToInt32(fs.Position);
                                indexInFile -= 14;
                            }
                            break;
                        default:
                            break;
                    }
                }
                if (indexInFile > 0)
                {
                    fs.Position = indexInFile;
                    byte b1 = br.ReadByte();
                    byte b2 = br.ReadByte();
                    indexInFile = Convert.ToInt32(b1) * 256 + Convert.ToInt32(b2);
                    return indexInFile;
                }
            }
            fs.Close();
            fs.Dispose();
            return indexInFile;
        }*/

        private void LoadLimiters()
        {
            barLimiterInfo.Caption = "";
            int rpmlimiter = _workingFile.ReadRpmLimiter();
            int speedlimiter = _workingFile.ReadSpeedLimiter();
            if (rpmlimiter > 0 && speedlimiter > 0)
            {
                barLimiterInfo.Caption = "Speed limit: " + FileTools.Instance.Speedlimit.ToString() + " rpm limit: " + FileTools.Instance.Rpmlimit.ToString();
            }
            else if (rpmlimiter > 0)
            {
                barLimiterInfo.Caption = "Rpm limit: " + FileTools.Instance.Rpmlimit.ToString();
            }
            else if (speedlimiter > 0)
            {
                barLimiterInfo.Caption = "Speed limit: " + FileTools.Instance.Speedlimit.ToString();
            }
            if (FileTools.Instance.Rpmlimit2 > 0) barLimiterInfo.Caption += " rpm limit2: " + FileTools.Instance.Rpmlimit2.ToString();
        }

        /*private void LoadMotronic43Limiters()
        {
            barLimiterInfo.Caption = "";
            int rpmpointer = LookupMotronic43RpmPointer();
            int speedpointer = LookupMotronic43SpeedPointer();
            Console.WriteLine("speed pointer = " + speedpointer.ToString("X4") + " rpm pointer = " + rpmpointer.ToString("X4"));
            if (rpmpointer > 0 && speedpointer > 0)
            {
                byte[] rpm_limiter = FileTools.Instance.readdatafromfile(FileTools.Instance.Currentfile, rpmpointer, 2);
                byte[] rpm_limiter2 = FileTools.Instance.readdatafromfile(FileTools.Instance.Currentfile, rpmpointer + 3, 1);
                FileTools.Instance.Rpmlimit2 = Convert.ToInt32(rpm_limiter2[0]);
                FileTools.Instance.Rpmlimit2 *= 40;
                Console.WriteLine("RPM limiter 2: " + FileTools.Instance.Rpmlimit2.ToString());
                byte[] speed_limiter = FileTools.Instance.readdatafromfile(FileTools.Instance.Currentfile, speedpointer, 1);


                // index 119 = speed limiter
                // index 109 = rpm limiter
                FileTools.Instance.Rpmlimit = Convert.ToInt32(rpm_limiter[0]);
                int temp = Convert.ToInt32(Math.Pow(2, Convert.ToDouble(rpm_limiter[1])));
                FileTools.Instance.Rpmlimit *= temp;
                FileTools.Instance.Rpmlimit = 9650000 / FileTools.Instance.Rpmlimit;
                //Console.WriteLine("RPM limiter: " + rpmlimit.ToString() + " rpm");
                FileTools.Instance.Speedlimit = Convert.ToInt32(speed_limiter[0]);
                //MessageBox.Show("Speed limit: " + speedlimit.ToString() + Environment.NewLine + "Rpm limit: " + rpmlimit.ToString());
                barLimiterInfo.Caption = "Speed limit: " + FileTools.Instance.Speedlimit.ToString() + " rpm limit: " + FileTools.Instance.Rpmlimit.ToString() + " rpm limit2: " + FileTools.Instance.Rpmlimit2.ToString();
            }
        }*/

        private void WriteRpmLimiter(int rpmlimiter)
        {
            _workingFile.WriteRpmLimiter(rpmlimiter);
        }

        private void WriteSpeedLimiter(int speedlimiter)
        {
            _workingFile.WriteSpeedLimiter(speedlimiter);
        }

        private void FillAxisInfoInSymbols(SymbolCollection symbols, AxisCollection axis)
        {
            foreach (SymbolHelper sh in symbols)
            {
                float[] xaxis = new float[1];
                float[] yaxis = new float[1];
                xaxis.SetValue(0, 0);
                yaxis.SetValue(0, 0);
                string xdescr = "";
                string ydescr = "";
                Helpers.Instance.GetAxisValues(FileTools.Instance.Currentfile, symbols, axis, sh.Varname, sh.Flash_start_address, 1, 1, out xaxis, out yaxis, out xdescr, out ydescr);
                sh.X_axisvalues = xaxis;
                sh.Y_axisvalues = yaxis;
                sh.XDescr = xdescr;
                sh.YDescr = ydescr;
            }
        }

        #endregion

        #region XML descriptor files

        private string GetFileDescriptionFromFile(string file)
        {
            string retval = string.Empty;
            try
            {
                using (StreamReader sr = new StreamReader(file))
                {
                    sr.ReadLine();
                    sr.ReadLine();
                    string name = sr.ReadLine();
                    name = name.Trim();
                    name = name.Replace("<", "");
                    name = name.Replace(">", "");
                    //name = name.Replace("x0020", " ");
                    name = name.Replace("_x0020_", " ");
                    for (int i = 0; i <= 9; i++)
                    {
                        name = name.Replace("_x003" + i.ToString() + "_", i.ToString());
                    }
                    retval = name;
                }
            }
            catch (Exception E)
            {
                Console.WriteLine(E.Message);
            }
            return retval;
        }

        private void SaveAdditionalSymbols()
        {
            System.Data.DataTable dt = new System.Data.DataTable(Path.GetFileNameWithoutExtension(FileTools.Instance.Currentfile));
            dt.Columns.Add("SYMBOLNAME");
            dt.Columns.Add("SYMBOLNUMBER", Type.GetType("System.Int32"));
            dt.Columns.Add("FLASHADDRESS", Type.GetType("System.Int32"));
            dt.Columns.Add("DESCRIPTION");

            string xmlfilename = System.Windows.Forms.Application.StartupPath + "\\repository\\" + Path.GetFileNameWithoutExtension(FileTools.Instance.Currentfile) + File.GetCreationTime(FileTools.Instance.Currentfile).ToString("yyyyMMddHHmmss") + ".xml";
            if (!Directory.Exists(System.Windows.Forms.Application.StartupPath + "\\repository"))
            {
                Directory.CreateDirectory(System.Windows.Forms.Application.StartupPath + "\\repository");
            }
            if (File.Exists(xmlfilename))
            {
                File.Delete(xmlfilename);
            }
            foreach (SymbolHelper sh in _workingFile.Symbols)
            {
                if (sh.UserDescription != "")
                {
                    dt.Rows.Add(sh.Varname, sh.Symbol_number, sh.Flash_start_address, sh.UserDescription);
                }
            }
            dt.WriteXml(xmlfilename);
        }

        private void ImportXMLDescriptor()
        {
            // ask user to point to XML document
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "XML documents|*.xml";
            ofd.Multiselect = false;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                /*_workingFile.Symbols = new SymbolCollection(); // Test
                SymbolTranslator st = new SymbolTranslator();
                System.Data.DataTable dt = new System.Data.DataTable(Path.GetFileNameWithoutExtension(ofd.FileName));
                dt.Columns.Add("SYMBOLNAME");
                dt.Columns.Add("SYMBOLNUMBER", Type.GetType("System.Int32"));
                dt.Columns.Add("FLASHADDRESS", Type.GetType("System.Int32"));
                dt.Columns.Add("LENGTH", Type.GetType("System.Int32"));
                dt.Columns.Add("DESCRIPTION");
                dt.Columns.Add("AXIS", Type.GetType("System.Boolean"));
                dt.Columns.Add("SIXTEENBIT", Type.GetType("System.Boolean"));

                string binname = GetFileDescriptionFromFile(ofd.FileName);
                if (binname != string.Empty)
                {
                    dt = new System.Data.DataTable(binname);
                    dt.Columns.Add("SYMBOLNAME");
                    dt.Columns.Add("SYMBOLNUMBER", Type.GetType("System.Int32"));
                    dt.Columns.Add("FLASHADDRESS", Type.GetType("System.Int32"));
                    dt.Columns.Add("LENGTH", Type.GetType("System.Int32"));
                    dt.Columns.Add("DESCRIPTION");
                    dt.Columns.Add("AXIS", Type.GetType("System.Boolean"));
                    dt.Columns.Add("SIXTEENBIT", Type.GetType("System.Boolean"));
                    if (File.Exists(ofd.FileName))
                    {
                        dt.ReadXml(ofd.FileName);
                    }
                }
                foreach (DataRow dr in dt.Rows)
                {
                    SymbolHelper sh = new SymbolHelper();
                    sh.Symbol_number = Convert.ToInt32(dr["SYMBOLNUMBER"]);
                    sh.Flash_start_address = Convert.ToInt32(dr["FLASHADDRESS"]);
                    sh.Length = Convert.ToInt32(dr["LENGTH"]);
                    sh.IsAxisSymbol = Convert.ToBoolean(dr["AXIS"]);
                    sh.IsSixteenbits = Convert.ToBoolean(dr["SIXTEENBIT"]);
                    sh.UserDescription = dr["DESCRIPTION"].ToString();
                    sh.Varname = sh.UserDescription;
                    string helptext = string.Empty;
                    string cat = string.Empty;
                    string sub = string.Empty;
                    sh.Description = st.TranslateSymbolToHelpText(sh.UserDescription, out helptext, out cat, out sub);
                    if (sh.Category == "Undocumented" || sh.Category == "")
                    {
                        if (sh.UserDescription.Contains("."))
                        {
                            try
                            {
                                sh.Category = sh.UserDescription.Substring(0, sh.UserDescription.IndexOf("."));
                            }
                            catch (Exception cE)
                            {
                                Console.WriteLine("Failed to assign category to symbol: " + sh.UserDescription + " err: " + cE.Message);
                            }
                        }

                    }
                    _workingFile.Symbols.Add(sh);
                }
                */
                TryToLoadAdditionalSymbols(ofd.FileName, false, _workingFile.Symbols);


                gridControl1.DataSource = _workingFile.Symbols;
                SetDefaultFilters();
                gridControl1.RefreshDataSource();
                // and save the data to the repository
                SaveAdditionalSymbols();

            }
        }

        private void TryToLoadAdditionalSymbols(string filename, bool ImportFromRepository, SymbolCollection symbols)
        {
            SymbolTranslator st = new SymbolTranslator();
            System.Data.DataTable dt = new System.Data.DataTable(Path.GetFileNameWithoutExtension(filename));
            dt.Columns.Add("SYMBOLNAME");
            dt.Columns.Add("SYMBOLNUMBER", Type.GetType("System.Int32"));
            dt.Columns.Add("FLASHADDRESS", Type.GetType("System.Int32"));
            dt.Columns.Add("DESCRIPTION");
            if (ImportFromRepository)
            {
                string xmlfilename = System.Windows.Forms.Application.StartupPath + "\\repository\\" + Path.GetFileNameWithoutExtension(filename) + File.GetCreationTime(filename).ToString("yyyyMMddHHmmss") + ".xml";
                if (!Directory.Exists(System.Windows.Forms.Application.StartupPath + "\\repository"))
                {
                    Directory.CreateDirectory(System.Windows.Forms.Application.StartupPath + "\\repository");
                }
                if (File.Exists(xmlfilename))
                {
                    dt.ReadXml(xmlfilename);
                }
            }
            else
            {
                string binname = GetFileDescriptionFromFile(filename);
                if (binname != string.Empty)
                {
                    dt = new System.Data.DataTable(binname);
                    dt.Columns.Add("SYMBOLNAME");
                    dt.Columns.Add("SYMBOLNUMBER", Type.GetType("System.Int32"));
                    dt.Columns.Add("FLASHADDRESS", Type.GetType("System.Int32"));
                    dt.Columns.Add("DESCRIPTION");
                    if (File.Exists(filename))
                    {
                        dt.ReadXml(filename);
                    }
                }
            }
            foreach (SymbolHelper sh in symbols)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    try
                    {
                        bool _fnd = false;
                        //if (dr["SYMBOLNUMBER"].ToString() == sh.Symbol_number.ToString())
                        {
                            if (sh.Symbol_number == Convert.ToInt32(dr["SYMBOLNUMBER"]))
                            {
                                if (sh.Flash_start_address == Convert.ToInt32(dr["FLASHADDRESS"]))
                                {
                                    _fnd = true;
                                    sh.UserDescription = dr["DESCRIPTION"].ToString();
                                    string helptext = string.Empty;
                                    string cat = string.Empty;
                                    string sub = string.Empty;
                                    sh.Description = st.TranslateSymbolToHelpText(sh.UserDescription, out helptext, out cat, out sub);
                                    if (sh.Category == "Undocumented" || sh.Category == "")
                                    {
                                        if (sh.UserDescription.Contains("."))
                                        {
                                            try
                                            {
                                                sh.Category = sh.UserDescription.Substring(0, sh.UserDescription.IndexOf("."));
                                            }
                                            catch (Exception cE)
                                            {
                                                Console.WriteLine("Failed to assign category to symbol: " + sh.UserDescription + " err: " + cE.Message);
                                            }
                                        }

                                    }

                                    break;
                                }
                            }
                        }
                        if (!_fnd)
                        {
                            // add the symbol
                            //symbols.Add(sh);
                        }
                    }
                    catch (Exception E)
                    {
                        Console.WriteLine(E.Message);
                    }
                }
            }
        }

        #endregion

        #region checksums

        private bool VerifyCRC()
        {
            return _workingFile.ValidateChecksum();
        }

        private void CalculateCRC(string filename)
        {
            if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC44)
            {
                FileStream fs = new FileStream(filename, FileMode.Open);
                fs.Position = 0;
                int volvocrc1 = 0;
                int volvocrc2 = 0;
                using (BinaryReader br = new BinaryReader(fs))
                {
                    while (fs.Position < 0xFF00)
                    {
                        volvocrc1 += (int)br.ReadByte();
                    }
                    fs.Position = 0x10000;
                    while (fs.Position < 0x1FF00)
                    {
                        volvocrc2 += (int)br.ReadByte();
                    }
                }
                fs.Close();
                fs.Dispose();
                frmInfoBox info = new frmInfoBox("CRC1: " + volvocrc1.ToString("X4") + " CRC2: " + volvocrc2.ToString("X4"));
            }
            else if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC43)
            {
                FileStream fs = new FileStream(filename, FileMode.Open);
                fs.Position = 0;
                int volvocrc1 = 0;
                using (BinaryReader br = new BinaryReader(fs))
                {
                    while (fs.Position < 0xFF00)
                    {
                        volvocrc1 += (int)br.ReadByte();
                    }
                }
                fs.Close();
                fs.Dispose();
                frmInfoBox info = new frmInfoBox("CRC: " + volvocrc1.ToString("X4"));
            }
            else if (FileTools.Instance.CurrentFiletype == FileType.LH242)
            {
                FileStream fs = new FileStream(filename, FileMode.Open);
                fs.Position = 0;
                int volvocrc1 = 0;
                using (BinaryReader br = new BinaryReader(fs))
                {
                    while (fs.Position < 0x7FFF)
                    {
                        byte b1 = br.ReadByte();
                        //byte b2 = br.ReadByte();
                        //UInt16 intr = br.ReadUInt16();
                        //Console.WriteLine(b.ToString("X2"));
                        volvocrc1 += (int)b1;
                    }
                }
                fs.Close();
                fs.Dispose();
                volvocrc1 &= 0x00FFFF;
                frmInfoBox info = new frmInfoBox("CRC: " + volvocrc1.ToString("X4"));
            }
        }

        private int CalculateLH242CRC(string filename)
        {
            FileStream fs = new FileStream(filename, FileMode.Open);
            fs.Position = 0;
            int volvocrc1 = 0;
            using (BinaryReader br = new BinaryReader(fs))
            {
                while (fs.Position <= 0x7FFF)
                {
                    byte b1 = br.ReadByte();
                    //byte b2 = br.ReadByte();
                    //UInt16 intr = br.ReadUInt16();
                    //Console.WriteLine(b.ToString("X2"));
                    volvocrc1 += (int)b1;
                }
            }
            fs.Close();
            fs.Dispose();
            volvocrc1 &= 0x00FFFF;
            return volvocrc1;
        }

        private M44CRC CalculateM44CRC(string filename)
        {
            M44CRC retval = new M44CRC();
            if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC44)
            {
                FileStream fs = new FileStream(filename, FileMode.Open);
                fs.Position = 0;
                retval.Volvocrc1 = 0;
                retval.Volvocrc2 = 0;
                using (BinaryReader br = new BinaryReader(fs))
                {
                    while (fs.Position < 0xFF00)
                    {
                        retval.Volvocrc1 += (uint)br.ReadByte();
                    }
                    fs.Position = 0x10000;
                    while (fs.Position < 0x1FF00)
                    {
                        retval.Volvocrc2 += (uint)br.ReadByte();
                    }
                }
                fs.Close();
                fs.Dispose();
            }
            retval.Volvocrc1 &= 0x00FFFF;
            retval.Volvocrc2 &= 0x00FFFF;
            return retval;
        }

        private void UpdateCRC(string filename)
        {
            if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC44)
            {
                FileStream fs = new FileStream(filename, FileMode.Open);
                fs.Position = 0;
                int volvocrc1 = 0;
                int volvocrc2 = 0;
                using (BinaryReader br = new BinaryReader(fs))
                {
                    while (fs.Position < 0xFF00)
                    {
                        volvocrc1 += (int)br.ReadByte();
                    }
                    fs.Position = 0x10000;
                    while (fs.Position < 0x1FF00)
                    {
                        volvocrc2 += (int)br.ReadByte();
                    }
                }
                fs.Close();
                fs.Dispose();
                // CRC1 is stored @ 0xFF00 and 0xFF01
                // CRC2 is stored @ 0x1FF00 and 0x1FF01
                FileStream fsi1 = File.OpenWrite(filename);
                using (BinaryWriter bw = new BinaryWriter(fsi1))
                {
                    fsi1.Position = 0xFF00;
                    byte b1 = (byte)((volvocrc1 & 0x00FF00) / 256);
                    byte b2 = (byte)(volvocrc1 & 0x0000FF);
                    bw.Write(b1);
                    bw.Write(b2);
                    fsi1.Position = 0x1FF00;
                    b1 = (byte)((volvocrc2 & 0x00FF00) / 256);
                    b2 = (byte)(volvocrc2 & 0x0000FF);
                    bw.Write(b1);
                    bw.Write(b2);
                }
                fsi1.Close();
                fsi1.Dispose();
            }
            else if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC43)
            {
                FileStream fs = new FileStream(filename, FileMode.Open);
                fs.Position = 0;
                int volvocrc = 0;
                using (BinaryReader br = new BinaryReader(fs))
                {
                    while (fs.Position < 0xFF00)
                    {
                        volvocrc += (int)br.ReadByte();
                    }
                }
                fs.Close();
                fs.Dispose();
                // CRC is stored @ 0xFF00 and 0xFF01
                FileStream fsi1 = File.OpenWrite(filename);
                using (BinaryWriter bw = new BinaryWriter(fsi1))
                {
                    fsi1.Position = 0xFF00;
                    byte b1 = (byte)((volvocrc & 0x00FF00) / 256);
                    byte b2 = (byte)(volvocrc & 0x0000FF);
                    bw.Write(b1);
                    bw.Write(b2);
                }
                fsi1.Close();
                fsi1.Dispose();
            }
            else if (FileTools.Instance.CurrentFiletype == FileType.LH24)
            {
                Console.WriteLine("Should update LH24 checksum here");
            }
            else if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC210)
            {
                _workingFile.UpdateChecksum();
            }
            else if (FileTools.Instance.CurrentFiletype == FileType.LH242)
            {
                Console.WriteLine("Should update LH242 checksum here");
                // 0x0000 - 0x7FFF
                // 0x3FF6 - 0x3FF7 = checksum
                // 0x3FF8 - 0x3FF9 = complement of checksum
                // the tricky bit.. the checksums are included in the data to be calculated
                // but since there is a complement, and the checksum is an addition, that does not matter
                int volvocrc  = CalculateLH242CRC(FileTools.Instance.Currentfile);
                int lh242checksumcomplement = (volvocrc ^ 0xFFFF) & 0x0000FFFF;

                // CRC is stored @ 0xFF00 and 0xFF01
                FileStream fsi1 = File.OpenWrite(filename);
                using (BinaryWriter bw = new BinaryWriter(fsi1))
                {
                    fsi1.Position = 0x3FF6;
                    byte b1 = (byte)((volvocrc & 0x00FF00) / 256);
                    byte b2 = (byte)(volvocrc & 0x0000FF);
                    bw.Write(b1);
                    bw.Write(b2);
                    b1 = (byte)((lh242checksumcomplement & 0x00FF00) / 256);
                    b2 = (byte)(lh242checksumcomplement & 0x0000FF);
                    bw.Write(b1);
                    bw.Write(b2);
                }
                fsi1.Close();
                fsi1.Dispose();
            }
        }

        #endregion

        #region helper functions

        private void OpenGridViewGroups(GridControl ctrl, int groupleveltoexpand)
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
                        if (grouplevel == groupleveltoexpand)
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

        private void WriteToLog(string item)
        {
            /*using (StreamWriter sw = new StreamWriter(@"C:\Documents and Settings\Guido.MOBICOACH\My Documents\Prive\Volvo\Motronic 4.3\Library\scan.txt", true))
            {
                sw.WriteLine(item);
            }*/
        }

        private void LoadLayoutFiles()
        {
            try
            {
                if (File.Exists(System.Windows.Forms.Application.StartupPath + "\\SymbolViewLayout.xml"))
                {
                    gridViewSymbols.RestoreLayoutFromXml(System.Windows.Forms.Application.StartupPath + "\\SymbolViewLayout.xml");
                }
            }
            catch (Exception E1)
            {
                Console.WriteLine(E1.Message);
            }
        }

        private void SaveLayoutFiles()
        {
            try
            {
                gridViewSymbols.SaveLayoutToXml(System.Windows.Forms.Application.StartupPath + "\\SymbolViewLayout.xml");
            }
            catch (Exception E)
            {
                Console.WriteLine(E.Message);
            }
        }

        private void DumpAxis(AxisHelper ah)
        {
            switch (ah.Addressinfile)
            {
                case 0xE4B6: ////LDR correctie (8*8) 
                    Console.WriteLine("Axis at address " + ah.Addressinfile.ToString("X4") + " has id: " + ah.Identifier.ToString("X4") + " ( " + ah.Descr + " ) and length: " + ah.Length.ToString() + "\t\tLDR correctie (8*8)");
                    break;
                case 0xE4F6: // LDR bias map (16*16)
                    Console.WriteLine("Axis at address " + ah.Addressinfile.ToString("X4") + " has id: " + ah.Identifier.ToString("X4") + " ( " + ah.Descr + " ) and length: " + ah.Length.ToString() + "\t\tLDR bias map (16*16)");
                    break;
                case 0xCC64: //// X voor LDR bias
                    Console.WriteLine("Axis at address " + ah.Addressinfile.ToString("X4") + " has id: " + ah.Identifier.ToString("X4") + " ( " + ah.Descr + " ) and length: " + ah.Length.ToString() + "\t\tX voor LDR bias");
                    break;
                case 0xCC77: //// Y voor LDR bias
                    Console.WriteLine("Axis at address " + ah.Addressinfile.ToString("X4") + " has id: " + ah.Identifier.ToString("X4") + " ( " + ah.Descr + " ) and length: " + ah.Length.ToString() + "\t\tY voor LDR bias");
                    break;
                case 0xCBFA: //// X voor LDR correctie 
                    Console.WriteLine("Axis at address " + ah.Addressinfile.ToString("X4") + " has id: " + ah.Identifier.ToString("X4") + " ( " + ah.Descr + " ) and length: " + ah.Length.ToString() + "\t\tX voor LDR correctie ");
                    break;
                case 0xF86C: //// Y voor LDR correctie 
                    Console.WriteLine("Axis at address " + ah.Addressinfile.ToString("X4") + " has id: " + ah.Identifier.ToString("X4") + " ( " + ah.Descr + " ) and length: " + ah.Length.ToString() + "\t\tY voor LDR correctie ");
                    break;
                default:
                    Console.WriteLine("Axis at address " + ah.Addressinfile.ToString("X4") + " has id: " + ah.Identifier.ToString("X4") + " ( " + ah.Descr + " ) and length: " + ah.Length.ToString());
                    break;
            }
            
            
           /* foreach (int value in ah.CalculcatedValues)
            {
                Console.WriteLine(" axis value: " + value.ToString("F2"));
            }*/
        }

        private void SetECUInfo(string ecuinfo)
        {
            if (ecuinfo == "")
            {
                barItemECUInfo.Caption = "Disconnected";
            }
            else
            {
                barItemECUInfo.Caption = "Connected - " + ecuinfo;
            }
        }

        private void SetProgressPercentage(string description, int percentage)
        {
            try
            {
                if (barEditItem1.Caption != description)
                {
                    barEditItem1.Caption = description;
                }
                if (percentage == 100) percentage = 0;
                if (Convert.ToInt32(barEditItem1.EditValue) != percentage)
                {
                    barEditItem1.EditValue = percentage;
                    Application.DoEvents();
                }
            }
            catch (Exception E)
            {
                Console.WriteLine("Error in SetProgressPercentage: " + E.Message + " - " + description + " " + percentage.ToString());
            }

        }

        private void OpenFile(string filename, bool silent)
        {

            FileInfo fitest = new FileInfo(filename);
            try
            {
                fitest.IsReadOnly = false;
                btnReadOnly.Caption = "File access OK";
            }
            catch (Exception E)
            {
                Console.WriteLine("Failed to remove read only flag: " + E.Message);
                btnReadOnly.Caption = "File is READ ONLY";
            }
            if (m_appSettings.DetermineCommunicationByFileType)
            {
                btnConnectECU.Enabled = false;
            }
            SetProgressPercentage("Loading file...", 0);
            btnCompressorMap.Enabled = false;
            this.Text = "MotronicSuite";
            FileInfo fi = new FileInfo(filename);
            btnCompareM44Halves.Enabled = false;
            if (fi.Length == 0x10000)
            {
                if (IsFileM210File(filename))
                {
                    // M2103
                    _workingFile = new M210File();
                    _workingFile.onDecodeProgress += new IECUFile.DecodeProgress(_workingFile_onDecodeProgress);
                    if (!silent)
                    {
                        frmInfoBox info = new frmInfoBox("M 2.10 support is still highly experimental! Some map definitions are hard coded and therefore might work only with some binaries. Note also that currently \"injector constant\" is valid only in custom modified Motronic binary.");
                    }
                    FileTools.Instance.CurrentFiletype = FileType.MOTRONIC210;
                    FileTools.Instance.Currentfile = filename;
                    FileTools.Instance.Currentfile_size = (int)fi.Length;

                    LoadSymbolTable(FileTools.Instance.Currentfile);
                    SetProgressPercentage("Load done...", 90);
                    m_appSettings.Lastfilename = FileTools.Instance.Currentfile;

                    this.Text = "MotronicSuite" + " [" + Path.GetFileName(FileTools.Instance.Currentfile) + "] [" + FileTools.Instance.CurrentFiletype.ToString() + "]";
                    SetProgressPercentage("Idle", 100);
                }
                else
                {
                    // MOTRONIC 4.3 file
                    _workingFile = new M43File();
                    _workingFile.onDecodeProgress += new IECUFile.DecodeProgress(_workingFile_onDecodeProgress);
                    FileTools.Instance.CurrentFiletype = FileType.MOTRONIC43;
                    btnCompressorMap.Enabled = true;
                    FileTools.Instance.Currentfile_size = (int)fi.Length;
                    FileTools.Instance.Currentfile = filename;
                    //BuildSymbolTable(FileTools.Instance.Currentfile);
                    SetProgressPercentage("Loading symbol table...", 10);
                    LoadSymbolTable(FileTools.Instance.Currentfile);
                    SetProgressPercentage("Load done...", 90);
                    m_appSettings.Lastfilename = FileTools.Instance.Currentfile;
                    if (!VerifyCRC())
                    {
                        if (m_appSettings.AutoChecksum)
                        {
                            UpdateCRC(FileTools.Instance.Currentfile);
                        }
                    }


                    this.Text = "MotronicSuite" + " [" + Path.GetFileName(FileTools.Instance.Currentfile) + "] [" + FileTools.Instance.CurrentFiletype.ToString() + "]";
                    if (m_appSettings.DetermineCommunicationByFileType)
                    {
                        btnConnectECU.Enabled = true;
                    }
                    barButtonItem8.Enabled = true;
                    SetProgressPercentage("Idle", 100);
                }
            }
            else if (fi.Length == 0x20000)
            {
                // MOTRONIC 4.4 file
                _workingFile = new M44File();
                _workingFile.onDecodeProgress += new IECUFile.DecodeProgress(_workingFile_onDecodeProgress);
                if (!silent)
                {
                    frmInfoBox info = new frmInfoBox("M4.4 support is still highly experimental!");
                }
                FileTools.Instance.CurrentFiletype = FileType.MOTRONIC44;
                FileTools.Instance.Currentfile = filename;
                FileTools.Instance.Currentfile_size = (int)fi.Length;

                SetProgressPercentage("Loading symbol table...", 10);

                LoadSymbolTable(FileTools.Instance.Currentfile);

                // check whether we should load the default damos file (info file)
                foreach (SymbolHelper sh in _workingFile.Symbols)
                {
                    if (sh.Flash_start_address == 0xE376 && sh.Length == 0x100 /* && sh.UserDescription == ""*/)
                    {
                        ExternalInformationSource source = new ExternalInformationSource();
                        // get dd.dat from a class that holds this damos file
                        //frmInfoBox info = new frmInfoBox("Loading additional data");
                        SetProgressPercentage("Loading additional information", 90);
                        source.FillSymbolCollection(Application.StartupPath + "\\dd.dat", SourceType.Damos, _workingFile.Symbols, _workingFile.Axis, false);
                        break;
                    }
                    /*else if (sh.Flash_start_address == 0xE454 && sh.Length == 0x100)
                    {
                        ExternalInformationSource source = new ExternalInformationSource();
                        // get dd.dat from a class that holds this damos file
                        //frmInfoBox info = new frmInfoBox("Loading additional data");
                        SetProgressPercentage("Loading additional information", 90);
                        source.FillSymbolCollection(Application.StartupPath + "\\dd.dat", SourceType.Damos, _workingFile.Symbols, _workingFile.Axis, true);
                        break;
                    }*/
                }

                SetProgressPercentage("Load done...", 95);
                m_appSettings.Lastfilename = FileTools.Instance.Currentfile;

                if (!VerifyCRC())
                {
                    if (m_appSettings.AutoChecksum)
                    {
                        UpdateCRC(FileTools.Instance.Currentfile);
                    }
                }
                SaveAdditionalSymbols(); // only for M4.4 because this load stuff from an external info file for now.

                this.Text = "MotronicSuite" + " [" + Path.GetFileName(FileTools.Instance.Currentfile) + "] [" + FileTools.Instance.CurrentFiletype.ToString() + "]";
                if (m_appSettings.DetermineCommunicationByFileType)
                {
                    btnConnectECU.Enabled = true;
                }
                barButtonItem8.Enabled = true;
                btnCompareM44Halves.Enabled = true;
                SetProgressPercentage("Idle", 100);

            }
            else if (fi.Length == 0x2000)
            {
                // LH2.2 OR ML1.1 ??
                FileTools.Instance.CurrentFiletype = FileType.LH22;
            }
            else if (fi.Length == 0x4000)
            {
                // LH2.4
                _workingFile = new LH24File();
                _workingFile.onDecodeProgress += new IECUFile.DecodeProgress(_workingFile_onDecodeProgress);
                if(!silent) 
                {
                    frmInfoBox info = new frmInfoBox("LH2.4 support is still highly experimental!");
                }
                FileTools.Instance.CurrentFiletype = FileType.LH24;
                FileTools.Instance.Currentfile = filename;
                FileTools.Instance.Currentfile_size = (int)fi.Length;

                LoadSymbolTable(FileTools.Instance.Currentfile);
                SetProgressPercentage("Load done...", 90);
                m_appSettings.Lastfilename = FileTools.Instance.Currentfile;

                this.Text = "MotronicSuite" + " [" + Path.GetFileName(FileTools.Instance.Currentfile) + "] [" + FileTools.Instance.CurrentFiletype.ToString() + "]";
                SetProgressPercentage("Idle", 100);
                // checksum last 2 bytes?
                // find 0x00 0x08 0x018 in file 0x20 0x28 0x2E 0x32 to find the addresstable
            }
            else if (fi.Length == 0x8000)
            {
                // LH2.4.2 OR Motronic M1.8
                // detect the type
                if (IsFileM18File(filename))
                {
                    _workingFile = new M18File();
                    _workingFile.onDecodeProgress += new IECUFile.DecodeProgress(_workingFile_onDecodeProgress);
                    //frmInfoBox info = new frmInfoBox("M1.8 support is still highly experimental!");
                    FileTools.Instance.CurrentFiletype = FileType.MOTRONIC18;
                    FileTools.Instance.Currentfile = filename;
                    FileTools.Instance.Currentfile_size = (int)fi.Length;
                    LoadSymbolTable(FileTools.Instance.Currentfile);
                    SetProgressPercentage("Load done...", 90);
                    m_appSettings.Lastfilename = FileTools.Instance.Currentfile;
                    this.Text = "MotronicSuite" + " [" + Path.GetFileName(FileTools.Instance.Currentfile) + "] [" + FileTools.Instance.CurrentFiletype.ToString() + "]";
                    SetProgressPercentage("Idle", 100);
                }
                else
                {
                    _workingFile = new LH242File();
                    _workingFile.onDecodeProgress += new IECUFile.DecodeProgress(_workingFile_onDecodeProgress);
                    if(!silent) 
                    {
                        frmInfoBox info = new frmInfoBox("LH2.4.2 support is still highly experimental!");
                    }
                    FileTools.Instance.CurrentFiletype = FileType.LH242;
                    FileTools.Instance.Currentfile = filename;
                    FileTools.Instance.Currentfile_size = (int)fi.Length;
                    LoadSymbolTable(FileTools.Instance.Currentfile);
                    SetProgressPercentage("Load done...", 90);
                    m_appSettings.Lastfilename = FileTools.Instance.Currentfile;
                    this.Text = "MotronicSuite" + " [" + Path.GetFileName(FileTools.Instance.Currentfile) + "] [" + FileTools.Instance.CurrentFiletype.ToString() + "]";
                    SetProgressPercentage("Idle", 100);
                }
            }
            else if (fi.Length == 0x100000 || fi.Length == 0x080000)
            {
                // ME7
                _workingFile = new ME7File();
                _workingFile.onDecodeProgress += new IECUFile.DecodeProgress(_workingFile_onDecodeProgress);
                frmInfoBox info = new frmInfoBox("ME7 support is only for experimental purposes!");
                FileTools.Instance.CurrentFiletype = FileType.MOTRONICME7;
                FileTools.Instance.Currentfile = filename;
                FileTools.Instance.Currentfile_size = (int)fi.Length;
                LoadSymbolTable(FileTools.Instance.Currentfile);
                SetProgressPercentage("Load done...", 90);
                m_appSettings.Lastfilename = FileTools.Instance.Currentfile;
                this.Text = "MotronicSuite" + " [" + Path.GetFileName(FileTools.Instance.Currentfile) + "] [" + FileTools.Instance.CurrentFiletype.ToString() + "]";
                SetProgressPercentage("Idle", 100);
                if (m_appSettings.DetermineCommunicationByFileType)
                {
                    btnConnectECU.Enabled = true;
                }
                barButtonItem8.Enabled = false;

            }
            else
            {
                SetProgressPercentage("Failed to open file", 100);
                if (!silent)
                {
                    frmInfoBox info = new frmInfoBox("File has incorrect length, maybe it is not a Motronic file?");
                }
            }
            SetMenuOptions();
            LoadLimiters();

            gridControl1.DataSource = _workingFile.Symbols;
            gridControl2.DataSource = _workingFile.Axis;
            OpenGridViewGroups(gridControl1, 0);
            gridViewSymbols.BestFitColumns();
            barFileTypeIndicator.Caption = FileTools.Instance.CurrentFiletype.ToString().Replace("FileType.", "");
            Application.DoEvents();
            /*
            gridViewSymbols.BeginSort();
            try
            {
                gridViewSymbols.ClearGrouping();
                gridViewSymbols.Columns["Category"].GroupIndex = 0;
            }
            finally
            {
                gridViewSymbols.EndSort();
            }
            Application.DoEvents();*/
            //LoadLayoutFiles(); // test
        }

        void _workingFile_onDecodeProgress(object sender, DecodeProgressEventArgs e)
        {
            SetProgressPercentage(e.Info, e.Progress);
        }

        private bool IsFileM210File(string filename)
        {
            byte[] alldata = File.ReadAllBytes(filename);
            for (int i = 0; i < alldata.Length - 5; i++)
            {
                if (alldata[i] == 'M' && alldata[i + 1] == '2' && alldata[i + 2] == '.' && alldata[i + 3] == '1' && alldata[i + 4] == '0') return true;
            }
            return false;
        }

        private bool IsFileM18File(string filename)
        {
            byte[] alldata = File.ReadAllBytes(filename);
            for (int i = 0; i < alldata.Length - 5; i++)
            {
                if (alldata[i] == 'M' && alldata[i + 1] == '1' && alldata[i + 2] == '.' && alldata[i + 3] == '8') return true;
            }
            return false;
        }

        private bool DoesSymbolExist(string symbolname)
        {
            foreach (SymbolHelper sh in _workingFile.Symbols)
            {
                if (sh.Varname == symbolname)
                {
                    return true;
                }
            }
            return false;
        }

        private void SetMenuOptions()
        {
            // check if a certain map exsist in the collection. If so, start it
            if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC43)
            {
                barButtonItem18.Enabled = DoesSymbolExist("VE map");
                btnWOTEnrichment.Enabled = DoesSymbolExist("WOT enrichment");
                btnWOTIgnition.Enabled = DoesSymbolExist("Ignition map: wide open throttle");
                btnOverboostMap.Enabled = DoesSymbolExist("Overboost map");
                btnMAFToLoad.Enabled = DoesSymbolExist("MAF to Load conversion map");
                btnMAFLimiter.Enabled = DoesSymbolExist("MAF limit");
                barButtonItem22.Enabled = false;
                barButtonItem19.Enabled = DoesSymbolExist("Ignition map: part throttle");
                barButtonItem20.Enabled = false;
                barButtonItem28.Enabled = false;
                barButtonItem30.Enabled = false;
                barButtonItem21.Enabled = DoesSymbolExist("Boost map");
                barButtonItem23.Enabled = false;
                barButtonItem24.Enabled = false;
                barButtonItem25.Enabled = false;
                barButtonItem26.Enabled = false;
                barButtonItem27.Enabled = false;

            }
            else if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC44)
            {
                btnWOTEnrichment.Enabled = false;
                btnWOTIgnition.Enabled = false;
                btnOverboostMap.Enabled = false;
                btnMAFToLoad.Enabled = false;
                btnMAFLimiter.Enabled = false;
                barButtonItem18.Enabled = DoesSymbolExist("VE map partload");
                barButtonItem22.Enabled = DoesSymbolExist("VE map (knock)");
                barButtonItem19.Enabled = DoesSymbolExist("Ignition map");
                barButtonItem20.Enabled = DoesSymbolExist("Catalyst safe factors");
                barButtonItem28.Enabled = DoesSymbolExist("Airmass increase for catalyst heating");
                barButtonItem30.Enabled = DoesSymbolExist("Increase of idle target rpm when catalyst heating");
                barButtonItem21.Enabled = DoesSymbolExist("Boost map");
                barButtonItem23.Enabled = DoesSymbolExist("Dutycycle bias for boost control");
                barButtonItem24.Enabled = DoesSymbolExist("Virtual throttle angle from bypass correction");
                barButtonItem25.Enabled = DoesSymbolExist("Load value from throttle position (angle) including bypass correction");
                barButtonItem26.Enabled = DoesSymbolExist("Knock detection threshold");
                barButtonItem27.Enabled = DoesSymbolExist("Max. enrichment for knock");
            }
            else
            {
                btnOverboostMap.Enabled = false;
                btnMAFToLoad.Enabled = false;
                btnMAFLimiter.Enabled = false;
                btnWOTEnrichment.Enabled = false;
                btnWOTIgnition.Enabled = false;
                barButtonItem18.Enabled = false;
                barButtonItem22.Enabled = false;
                barButtonItem19.Enabled = false;
                barButtonItem20.Enabled = false;
                barButtonItem28.Enabled = false;
                barButtonItem30.Enabled = false;
                barButtonItem21.Enabled = false;
                barButtonItem23.Enabled = false;
                barButtonItem24.Enabled = false;
                barButtonItem25.Enabled = false;
                barButtonItem26.Enabled = false;
                barButtonItem27.Enabled = false;
            }
        }

        private void OpenFile()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Binary files|*.bin";
            ofd.Multiselect = false;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                CloseProject();
                m_appSettings.LastOpenedType = 0;
                OpenFile(ofd.FileName, false);
                m_appSettings.Lastfilename = ofd.FileName;
            }
        }

        private void DumpSymbolTable(SymbolCollection symbols, string message)
        {
            foreach (SymbolHelper sh in symbols)
            {
                if (sh.Varname.StartsWith("VE"))
                {
                    Console.WriteLine(message + " " + sh.Varname + " " + sh.Flash_start_address.ToString("X4"));
                }
            }
        }

        private bool CompareSymbolToCurrentFile(string symbolname, int address, int length, string filename, out double diffperc, out int diffabs, out double diffavg)
        {
            diffperc = 0;
            diffabs = 0;
            diffavg = 0;

            double totalvalue1 = 0;
            double totalvalue2 = 0;
            bool retval = true;

            if (address > 0)
            {

                int curaddress = (int)Helpers.Instance.GetSymbolAddress(_workingFile.Symbols, symbolname);

                int curlength = Helpers.Instance.GetSymbolLength(_workingFile.Symbols, symbolname);
                byte[] curdata = FileTools.Instance.readdatafromfile(FileTools.Instance.Currentfile, curaddress, curlength, false);
                byte[] compdata = FileTools.Instance.readdatafromfile(filename, address, length, false);
                if (curdata.Length != compdata.Length)
                {
                    Console.WriteLine("Lengths didn't match: " + symbolname);
                    return false;
                }
                for (int offset = 0; offset < curdata.Length; offset++)
                {
                    if ((byte)curdata.GetValue(offset) != (byte)compdata.GetValue(offset))
                    {
                        retval = false;
                        //Console.WriteLine("Difference detected in: " + symbolname + " offset=" + offset.ToString() + " value1: " + curdata[offset].ToString("X2") + " value2: " + compdata[offset].ToString("X2"));
                        diffabs++;
                    }
                    totalvalue1 += (byte)curdata.GetValue(offset);
                    totalvalue2 += (byte)compdata.GetValue(offset);
                }
                if (curdata.Length > 0)
                {
                    totalvalue1 /= curdata.Length;
                    totalvalue2 /= compdata.Length;
                }
            }
            diffavg = totalvalue1;
            /*            if (isSixteenBitTable(symbolname))
                        {
                            diffabs /= 2;
                        }*/

            return retval;
        }

        private void CompareToFile(string filename)
        {
            if (FileTools.Instance.Currentfile != "")
            {
                if (_workingFile.Symbols.Count > 0)
                {
                    dockManager1.BeginUpdate();
                    try
                    {
                        DevExpress.XtraBars.Docking.DockPanel dockPanel = dockManager1.AddPanel(new System.Drawing.Point(-500, -500));
                        CompareResults tabdet = new CompareResults();
                        tabdet.ShowAddressesInHex = true;
                        tabdet.SetFilterMode(true);
                        tabdet.Dock = DockStyle.Fill;
                        tabdet.Filename = filename;
                        tabdet.onSymbolSelect += new CompareResults.NotifySelectSymbol(tabdet_onSymbolSelect);
                        dockPanel.Controls.Add(tabdet);
                        dockPanel.Text = "Compare results: " + Path.GetFileName(filename);
                        dockPanel.DockTo(dockManager1, DevExpress.XtraBars.Docking.DockingStyle.Left, 1);

                        dockPanel.Width = 500;


                        SymbolCollection compare_symbols = new SymbolCollection();
                        AxisCollection compare_axis = new AxisCollection();
                        FileInfo fi = new FileInfo(filename);
                        //DumpSymbolTable(_workingFile.Symbols, "BEFORE");
                        ExtractSymbolCollection(filename, out compare_symbols, out compare_axis, (int)fi.Length);
                        TryToLoadAdditionalSymbols(filename, true, compare_symbols);
                        //DumpSymbolTable(_workingFile.Symbols, "AFTER");
                        //DumpSymbolTable(compare_symbols, "COMPARE");
                        System.Windows.Forms.Application.DoEvents();
                        System.Data.DataTable dt = new System.Data.DataTable();
                        dt.Columns.Add("SYMBOLNAME");
                        dt.Columns.Add("SRAMADDRESS", Type.GetType("System.Int32"));
                        dt.Columns.Add("FLASHADDRESS", Type.GetType("System.Int32"));
                        dt.Columns.Add("LENGTHBYTES", Type.GetType("System.Int32"));
                        dt.Columns.Add("LENGTHVALUES", Type.GetType("System.Int32"));
                        dt.Columns.Add("DESCRIPTION");
                        dt.Columns.Add("ISCHANGED", Type.GetType("System.Boolean"));
                        dt.Columns.Add("CATEGORY", Type.GetType("System.Int32")); //0
                        dt.Columns.Add("DIFFPERCENTAGE", Type.GetType("System.Double"));
                        dt.Columns.Add("DIFFABSOLUTE", Type.GetType("System.Int32"));
                        dt.Columns.Add("DIFFAVERAGE", Type.GetType("System.Double"));
                        dt.Columns.Add("CATEGORYNAME");
                        dt.Columns.Add("SUBCATEGORYNAME");
                        dt.Columns.Add("SymbolNumber1", Type.GetType("System.Int32"));
                        dt.Columns.Add("SymbolNumber2", Type.GetType("System.Int32"));
                        string category = "";
                        string ht = string.Empty;
                        double diffperc = 0;
                        int diffabs = 0;
                        double diffavg = 0;
                        XDFCategories cat = XDFCategories.Undocumented;
                        XDFSubCategory subcat = XDFSubCategory.Undocumented;
                        if (compare_symbols.Count > 0)
                        {
                            CompareResults cr = new CompareResults();
                            cr.ShowAddressesInHex = true;
                            cr.SetFilterMode(true);

                            //SymbolTranslator st = new SymbolTranslator();
                            foreach (SymbolHelper sh_compare in compare_symbols)
                            {
                                foreach (SymbolHelper sh_org in _workingFile.Symbols)
                                {
                                    string varnameori = sh_org.Varname;
                                    string varnamecom = sh_compare.Varname;
                                    if (sh_org.UserDescription != string.Empty) varnameori = sh_org.UserDescription;
                                    if (sh_compare.UserDescription != string.Empty) varnamecom = sh_compare.UserDescription;

                                    if (varnameori == varnamecom && sh_compare.Length == sh_org.Length)
                                    {
                                        //Console.WriteLine("Comparing:  " + varnameori);
                                        // compare
                                        if (sh_compare.Flash_start_address > 0 && sh_compare.Flash_start_address < FileTools.Instance.Currentfile_size)
                                        {
                                            if (sh_org.Flash_start_address > 0 && sh_org.Flash_start_address < FileTools.Instance.Currentfile_size)
                                            {
                                                //[Flash_start_address] > 0 AND [Flash_start_address] < 524288
                                                //if(sh_compare.Symbol_number == 
                                                if (!CompareSymbolToCurrentFile(varnameori, (int)sh_compare.Flash_start_address, sh_compare.Length, filename, out diffperc, out diffabs, out diffavg))
                                                {
                                                    category = "";
                                                    if (varnameori.Contains("."))
                                                    {
                                                        try
                                                        {
                                                            category = varnameori.Substring(0, varnameori.IndexOf("."));
                                                        }
                                                        catch (Exception cE)
                                                        {
                                                            Console.WriteLine("Failed to assign category to symbol: " + varnameori + " err: " + cE.Message);
                                                        }
                                                    }

                                                    dt.Rows.Add(varnameori, sh_compare.Start_address, sh_compare.Flash_start_address, sh_compare.Length, sh_compare.Length, varnameori, false, 0, diffperc, diffabs, diffavg, category, "", sh_org.Symbol_number, sh_compare.Symbol_number);
                                                }
                                            }
                                        }
                                        break;
                                    }
                                }
                            }
                            tabdet.CompareSymbolCollection = compare_symbols;
                            tabdet.CompareAxisCollection = compare_axis;
                            tabdet.OpenGridViewGroups(tabdet.gridControl1, 1);
                            tabdet.gridControl1.DataSource = dt.Copy();
                        }
                    }
                    catch (Exception E)
                    {
                        Console.WriteLine(E.Message);
                    }
                    dockManager1.EndUpdate();
                }
            }
            SetProgressPercentage("Idle", 100);
        }

        private void ExtractSymbolCollection(string filename, out SymbolCollection compare_symbols, out AxisCollection compare_axis, int filelength)
        {
            compare_symbols = new SymbolCollection();
            compare_axis = new AxisCollection();
            IECUFile _compareFile;

            if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC43)
            {
                if (filelength == 0x10000)
                {
                    _compareFile = new M43File();
                    _compareFile.SelectFile(filename);
                    _compareFile.ParseFile();
                    compare_symbols = _compareFile.Symbols;
                    compare_axis = _compareFile.Axis;
                }
            }
            else if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC44)
            {
                if (filelength == 0x20000)
                {
                    _compareFile = new M44File();
                    _compareFile.SelectFile(filename);
                    _compareFile.ParseFile();
                    compare_symbols = _compareFile.Symbols;
                    compare_axis = _compareFile.Axis;
                }
            }
            else if (FileTools.Instance.CurrentFiletype == FileType.MOTRONICME7)
            {
                if (filelength == 0x80000 || filelength == 0x100000)
                {
                    _compareFile = new ME7File();
                    _compareFile.SelectFile(filename);
                    _compareFile.ParseFile();
                    compare_symbols = _compareFile.Symbols;
                    compare_axis = _compareFile.Axis;
                }
            }
        }

        private void SetFilterMode()
        {
            if (m_appSettings.ShowAddressesInHex)
            {
                gridColumn2.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                gridColumn2.DisplayFormat.FormatString = "X6";
                gridColumn2.FilterMode = DevExpress.XtraGrid.ColumnFilterMode.DisplayText;

                gridColumn3.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                gridColumn3.DisplayFormat.FormatString = "X6";
                gridColumn3.FilterMode = DevExpress.XtraGrid.ColumnFilterMode.DisplayText;

                gridColumn6.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                gridColumn6.DisplayFormat.FormatString = "X6";
                gridColumn6.FilterMode = DevExpress.XtraGrid.ColumnFilterMode.DisplayText;
                gridColumn8.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                gridColumn8.DisplayFormat.FormatString = "X6";
                gridColumn8.FilterMode = DevExpress.XtraGrid.ColumnFilterMode.DisplayText;


            }
            else
            {
                gridColumn2.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                gridColumn2.DisplayFormat.FormatString = "";
                gridColumn2.FilterMode = DevExpress.XtraGrid.ColumnFilterMode.Value;
                gridColumn3.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                gridColumn3.DisplayFormat.FormatString = "";
                gridColumn3.FilterMode = DevExpress.XtraGrid.ColumnFilterMode.Value;

                gridColumn6.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                gridColumn6.DisplayFormat.FormatString = "";
                gridColumn6.FilterMode = DevExpress.XtraGrid.ColumnFilterMode.Value;

                gridColumn8.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                gridColumn8.DisplayFormat.FormatString = "";
                gridColumn8.FilterMode = DevExpress.XtraGrid.ColumnFilterMode.Value;

            }
            SetDefaultFilters();
        }

        private void SetDefaultFilters()
        {
            if (m_appSettings.ShowAddressesInHex)
            {
                DevExpress.XtraGrid.Columns.ColumnFilterInfo fltr = new DevExpress.XtraGrid.Columns.ColumnFilterInfo(@"([Flash_start_address] <> '000000')", "Only symbols within binary");
                gridViewSymbols.ActiveFilter.Clear();
                gridViewSymbols.ActiveFilter.Add(gridColumn2, fltr);
                /*** set filter ***/
                gridViewSymbols.ActiveFilterEnabled = true;
            }
            else
            {
                DevExpress.XtraGrid.Columns.ColumnFilterInfo fltr = new DevExpress.XtraGrid.Columns.ColumnFilterInfo(@"([Flash_start_address] > 0)", "Only symbols within binary");
                gridViewSymbols.ActiveFilter.Clear();
                gridViewSymbols.ActiveFilter.Add(gridColumn2, fltr);
                /*** set filter ***/
                gridViewSymbols.ActiveFilterEnabled = true;
            }
        }

        private bool CollectionContainsMap(string symbol)
        {
            foreach (SymbolHelper sh in _workingFile.Symbols)
            {
                if (sh.Varname == symbol) return true;
            }
            return false;
        }

        private void OpenXDFFile(string filename)
        {

            // for test
            _workingFile.Symbols.Clear();
            gridControl1.DataSource = null;

            if (File.Exists(filename))
            {
                // read the data from XML format
                DataSet ds = new DataSet();
                try
                {
                    ds.ReadXml(filename);
                }
                catch (Exception E)
                {
                    Console.WriteLine(E.Message);
                    return;
                }
                /*
Table Column: title
Table Column: description
Table Column: XDFTABLE_Id
Table Column: uniqueid      // address in file
Table Column: flags         // length
Table Column: XDFFORMAT_Id
                 
Axis Column: units
Axis Column: address
Axis Column: indexcount
Axis Column: indexsizebits
Axis Column: decimalpl
Axis Column: min
Axis Column: max
Axis Column: outputtype
Axis Column: XDFAXIS_Id
Axis Column: datatype
Axis Column: unittype
Axis Column: id
Axis Column: flags
Axis Column: uniqueid
Axis Column: XDFTABLE_Id                 * */
                if (ds.Tables.Contains("XDFTABLE"))
                {
                    foreach (DataColumn dc in ds.Tables["XDFTABLE"].Columns)
                    {
                        Console.WriteLine("Table Column: " + dc.ColumnName);
                    }
                    foreach (DataRow drtable in ds.Tables["XDFTABLE"].Rows)
                    {
                        Console.WriteLine("Child table (1): " + ds.Tables["XDFTABLE"].ChildRelations["XDFTABLE_XDFAXIS"].ChildTable.TableName);
                        int columns = 0;
                        int rows = 0;
                        int address = 0;
                        int xaddress = 0;
                        int yaddress = 0;
                        string xdescr = "";
                        string ydescr = "";
                        string zdescr = "";
                        foreach (DataRow axisdr in ds.Tables["XDFTABLE"].ChildRelations["XDFTABLE_XDFAXIS"].ChildTable.Rows)
                        {
                            //Console.WriteLine(ds.Tables["XDFTABLE"].ChildRelations["XDFTABLE_XDFAXIS"].ChildColumns[0].ToString());
                            if (Convert.ToInt32(axisdr["XDFTABLE_Id"]) == Convert.ToInt32(drtable["XDFTABLE_Id"]))
                            {
                                //Console.WriteLine("TabID: " + axisdr["XDFTABLE_Id"].ToString());
                                //Console.WriteLine("TabID (2): " + drtable["XDFTABLE_Id"].ToString());
                                //Console.WriteLine(axisdr["embedinfo"].ToString());
                                if (axisdr["id"].ToString() == "x")
                                {
                                    columns = Convert.ToInt32(axisdr["indexcount"]);
                                    try
                                    {
                                        xdescr = axisdr["units"].ToString();
                                    }
                                    catch (Exception E)
                                    {
                                        Console.WriteLine(E.Message);
                                    }
                                    // zoek de bijbehorende x/y as adressen
                                    foreach (DataRow embeddr in ds.Tables["XDFAXIS"].ChildRelations["XDFAXIS_embedinfo"].ChildTable.Rows)
                                    {
                                        if (Convert.ToInt32(axisdr["XDFAXIS_Id"]) == Convert.ToInt32(embeddr["XDFAXIS_Id"]))
                                        {
                                            xaddress = Convert.ToInt32(embeddr["linkobjid"].ToString().Replace("0x", ""), 16);
                                            break;
                                        }
                                    }

                                }
                                else if (axisdr["id"].ToString() == "y")
                                {
                                    rows = Convert.ToInt32(axisdr["indexcount"]);
                                    try
                                    {
                                        ydescr = axisdr["units"].ToString();
                                    }
                                    catch (Exception E)
                                    {
                                        Console.WriteLine(E.Message);
                                    }

                                    // zoek de bijbehorende x/y as adressen
                                    foreach (DataRow embeddr in ds.Tables["XDFAXIS"].ChildRelations["XDFAXIS_embedinfo"].ChildTable.Rows)
                                    {
                                        if (Convert.ToInt32(axisdr["XDFAXIS_Id"]) == Convert.ToInt32(embeddr["XDFAXIS_Id"]))
                                        {
                                            if (embeddr.Table.Columns.Contains("linkobjid"))
                                            {
                                                yaddress = Convert.ToInt32(embeddr["linkobjid"].ToString().Replace("0x", ""), 16);
                                            }
                                            break;
                                        }
                                    }


                                }
                                else if (axisdr["id"].ToString() == "z")
                                {
                                    try
                                    {
                                        zdescr = axisdr["units"].ToString();
                                    }
                                    catch (Exception E)
                                    {
                                        Console.WriteLine(E.Message);
                                    }
                                    //mmedaddress
                                    if (axisdr.Table.Columns.Contains("address"))
                                    {
                                        address = Convert.ToInt32(axisdr["address"].ToString().Replace("0x", ""), 16);
                                    }
                                }
                            }
                        }
                        //Console.WriteLine("Child table (1): " + ds.Tables["XDFTABLE"].ChildRelations[0].ChildTable.TableName);
                        int tablesize = columns * rows;
                        if (tablesize > 0 && address > 0)
                        {
                            SymbolHelper sh = new SymbolHelper();
                            sh.Flash_start_address = /*Convert.ToInt32(drtable["uniqueid"].ToString().Replace("0x", ""), 16)*/ address;
                            sh.Varname = drtable["title"].ToString();
                            sh.Length = tablesize;
                            sh.Cols = columns;
                            sh.Rows = rows;
                            sh.X_axis_address = xaddress;
                            sh.Y_axis_address = yaddress;
                            sh.X_axis_length = columns;
                            sh.Y_axis_length = rows;
                            sh.XDescr = xdescr;
                            sh.YDescr = ydescr;
                            sh.ZDescr = zdescr;
                            _workingFile.Symbols.Add(sh);
                        }


                    }
                }
                if (ds.Tables.Contains("XDFAXIS"))
                {
                    foreach (DataColumn dc in ds.Tables["XDFAXIS"].Columns)
                    {
                        Console.WriteLine("Axis Column: " + dc.ColumnName);
                    }
                    /*foreach (DataRow drtable in ds.Tables["XDFTABLE"].Rows)
                    {
                            
                    }*/
                }
                gridControl1.DataSource = _workingFile.Symbols;
                //gridControl1.DataSource = ds;
            }
        }

        #endregion

        #region common event handlers

        private void btnCompressorMap_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            // add a new dock with a compressor map control
            if (FileTools.Instance.Currentfile != string.Empty)
            {
                if (File.Exists(FileTools.Instance.Currentfile))
                {
                    string mapName = "Boost map";
                    int cols = 16;
                    int rows = 8;

                    dockManager1.BeginUpdate();
                    DockPanel dp = dockManager1.AddPanel(DockingStyle.Left);
                    dp.ClosedPanel += new DockPanelEventHandler(dockPanel_ClosedPanel);
                    ctrlCompressorMapEx cm = new ctrlCompressorMapEx();
                    cm.onRefreshData += new ctrlCompressorMapEx.RefreshData(cm_onRefreshData);
                    cm.Dock = DockStyle.Fill;
                    // set boost map, rpm range and turbo type
                    double[] boost_req = new double[16];
                    byte[] tryck_mat = FileTools.Instance.readdatafromfile(FileTools.Instance.Currentfile, Helpers.Instance.GetSymbolAddress(_workingFile.Symbols, mapName), Helpers.Instance.GetSymbolLength(_workingFile.Symbols, mapName), false);
                    // now get the doubles from it
                    for (int i = 0; i < 16; i++)
                    {
                        double val = Convert.ToDouble(tryck_mat[7 * 16 + i]);
                        val /= 100;
                        val -= 1;
                        boost_req.SetValue(val, i);
                    }

                    cm.Boost_request = boost_req;
                    // set rpm range
                    int[] xvals = new int[16];
                    SymbolHelper shax = Helpers.Instance.GetXaxisSymbol(FileTools.Instance.Currentfile, _workingFile.Symbols, _workingFile.Axis, mapName, Helpers.Instance.GetSymbolAddress(_workingFile.Symbols, mapName));
                    //GetAxisValues(FileTools.Instance.Currentfile, _workingFile.Symbols, _workingFile.Axis, mapName, Helpers.Instance.GetSymbolAddress(_workingFile.Symbols, mapName), rows, cols, out xvals, out yvals, out xdescr, out ydescr);
                    foreach (AxisHelper ahx in _workingFile.Axis)
                    {
                        if (ahx.Addressinfile == shax.X_axis_address)
                        {
                            for (int i = 0; i < 16; i++)
                            {
                                float fval = (float)ahx.CalculcatedValues.GetValue(i);
                                xvals.SetValue(Convert.ToInt32(fval), i);
                            }
                            break;
                        }
                    }

                    cm.Rpm_points = xvals;

                    //PartNumberConverter pnc = new PartNumberConverter();
                    //ECUInformation ecuinfo = pnc.GetECUInfo(props.Partnumber, props.Enginetype);
                    cm.SetCompressorType(MotronicSuite.ctrlCompressorMapEx.CompressorMap.TD0418T);
                    cm.Current_engineType = ctrlCompressorMapEx.EngineType.Liter25;
                    //cm.Current_engineType = ctrlCompressorMapEx.EngineType.Liter2;
                    dp.Width = 600;
                    dp.Text = "Compressor map plotter";
                    dp.Controls.Add(cm);
                    dockManager1.EndUpdate();
                }
            }
        }

        private void btnWOTEnrichment_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC43)
            {
                StartTableViewerByName("WOT enrichment");
            }
        }

        private void btnWOTIgnition_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC43)
            {
                StartTableViewerByName("Ignition map: wide open throttle");
            }
        }

        private void gridView1_DoubleClick(object sender, EventArgs e)
        {
            int[] selectedrows = gridViewSymbols.GetSelectedRows();

            if (selectedrows.Length > 0)
            {
                int grouplevel = gridViewSymbols.GetRowLevel((int)selectedrows.GetValue(0));
                if (grouplevel >= gridViewSymbols.GroupCount)
                {
                    int[] selrows = gridViewSymbols.GetSelectedRows();
                    if (selrows.Length > 0)
                    {
                        SymbolHelper sh = (SymbolHelper)gridViewSymbols.GetRow((int)selrows.GetValue(0));
                        //DataRowView dr = (DataRowView)gridViewSymbols.GetRow((int)selrows.GetValue(0));
                        StartTableViewer(sh);//sh.Varname, sh.Flash_start_address, sh.Cols, sh.Rows, sh.IsSixteenbits);
                    }
                }
            }
        }

        private void barButtonItem1_ItemClick_1(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            OpenFile();
        }

        private void barButtonItem2_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            // show a new panel with all the detected axis
            // and information about to which map it is attached
            string symbolname = string.Empty;
            if (gridViewSymbols.SelectedRowsCount > 0)
            {
                int[] selrows = gridViewSymbols.GetSelectedRows();
                if (selrows.Length > 0)
                {
                    SymbolHelper dr = (SymbolHelper)gridViewSymbols.GetRow((int)selrows.GetValue(0));
                    if (dr != null)
                    {
                        symbolname = dr.Varname;
                    }
                }
            }
            DevExpress.XtraBars.Docking.DockPanel dockPanel = dockManager1.AddPanel(new System.Drawing.Point(-500, -500));
            AxisBrowser tabdet = new AxisBrowser();
            tabdet.onStartSymbolViewer += new AxisBrowser.StartSymbolViewer(tabdet_onStartSymbolViewer);
            tabdet.onStartAxisViewer += new AxisBrowser.StartAxisViewer(tabdet_onStartAxisViewer);
            tabdet.Axis = _workingFile.Axis;
            tabdet.Dock = DockStyle.Fill;
            dockPanel.Controls.Add(tabdet);

            FillAxisInfoInSymbols(_workingFile.Symbols, _workingFile.Axis);

            tabdet.ShowSymbolCollection(_workingFile.Symbols);
            tabdet.SetCurrentSymbol(symbolname);
            dockPanel.Text = "Axis browser: " + Path.GetFileName(FileTools.Instance.Currentfile);
            bool isDocked = false;
            foreach (DevExpress.XtraBars.Docking.DockPanel pnl in dockManager1.Panels)
            {
                if (pnl.Text.StartsWith("Axis browser: ") && pnl != dockPanel && (pnl.Visibility == DevExpress.XtraBars.Docking.DockVisibility.Visible))
                {
                    dockPanel.DockAsTab(pnl, 0);
                    isDocked = true;
                    break;
                }
            }
            if (!isDocked)
            {
                dockPanel.DockTo(dockManager1, DevExpress.XtraBars.Docking.DockingStyle.Left, 1);
                dockPanel.Width = 700;
            }
        }

        void tabdet_onStartAxisViewer(object sender, AxisBrowser.AxisViewerRequestedEventArgs e)
        {
            StartAxisEditor(FileTools.Instance.Currentfile, e.AxisAddress);
        }

        private void barButtonItem3_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            frmAbout about = new frmAbout();
            about.ShowDialog();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            InitSkins();
            try
            {
                MapViewer mv = new MapViewer();
            }
            catch (Exception E)
            {
                Console.WriteLine(E.Message);
            }
            if (m_appSettings.AutoLoadLastFile)
            {
                if (m_appSettings.LastOpenedType == 0)
                {
                    if (m_appSettings.Lastfilename != "")
                    {
                        if (File.Exists(m_appSettings.Lastfilename))
                        {
                            OpenFile(m_appSettings.Lastfilename, true);
                        }
                    }
                }
                else if (m_appSettings.Lastprojectname != "")
                {
                    OpenProject(m_appSettings.Lastprojectname);
                }
            }
            SetDefaultFilters();
            LoadLayoutFiles();
            if (File.Exists(System.Windows.Forms.Application.StartupPath + "\\rtsymbols.txt"))
            {
                LoadRealtimeTable(System.Windows.Forms.Application.StartupPath + "\\rtsymbols.txt");
            }
            splash.Close();
        }

        private void barButtonItem8_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (FileTools.Instance.Currentfile != null)
            {
                string outputfile = Path.GetDirectoryName(FileTools.Instance.Currentfile);
                outputfile = Path.Combine(outputfile, Path.GetFileNameWithoutExtension(FileTools.Instance.Currentfile) + ".asm");
                Application.DoEvents();
                //Console.WriteLine("Starting with " + outputfile);
                //Process.Start(Application.StartupPath + "\\TextEditor.exe", "\"" + outputfile + "\"");
                DockPanel panel = dockManager1.AddPanel(DockingStyle.Right);
                panel.Width = this.ClientSize.Width - dockPanel1.Width;
                ctrlDisassembler disasmcontrol = new ctrlDisassembler();
                disasmcontrol.MotronicFile = FileTools.Instance.Currentfile;
                disasmcontrol.Dock = DockStyle.Fill;
                panel.Controls.Add(disasmcontrol);
                panel.Text = "MotronicSuite Disassembler";
                Application.DoEvents();
                disasmcontrol.DisassembleFile();
            }
            /*
            if (FileTools.Instance.Currentfile != "")
            {
                Sim8051Dasm dasm = new Sim8051Dasm();
                frmProgress progress = new frmProgress();
                progress.SetProgress("Initializing disassembler");
                progress.SetProgressPercentage(10);
                progress.Show();
                try
                {
                    dasm.Initialize(readdatafromfile(FileTools.Instance.Currentfile, 0, 0x10000));
                    SimError err;
                    progress.SetProgress("Running disassembler");
                    progress.SetProgressPercentage(20);
                    string[] result = dasm.Disassemble(true, 0, out err);
                    progress.SetProgress("Outputting data");
                    progress.SetProgressPercentage(10);

                    int linecount = 0;
                    string filename = Path.GetDirectoryName(FileTools.Instance.Currentfile) + "\\" + Path.GetFileNameWithoutExtension(FileTools.Instance.Currentfile) + ".asm";
                    if (File.Exists(filename))
                    {
                        File.Delete(filename);
                    }
                    using (StreamWriter sw = new StreamWriter(filename))
                    {
                        foreach (string s in result)
                        {
                            progress.SetProgressPercentage(((linecount++ * 80) / result.Length) + 20);
                            sw.WriteLine(s);

                        }
                    }


                    dockManager1.BeginUpdate();
                    try
                    {
                        DevExpress.XtraBars.Docking.DockPanel dockPanel = dockManager1.AddPanel(DevExpress.XtraBars.Docking.DockingStyle.Right);
                        dockPanel.Text = "Assembler: " + Path.GetFileName(filename);
                        AsmViewer av = new AsmViewer();
                        av.Dock = DockStyle.Fill;
                        dockPanel.Width = 700;
                        dockPanel.Controls.Add(av);
                        progress.SetProgress("Loading assembler file ...");
                        av.LoadDataFromFile(filename, _workingFile.Symbols);
                    }
                    catch (Exception E)
                    {
                        Console.WriteLine(E.Message);
                    }
                    dockManager1.EndUpdate();


                }
                catch (Exception E)
                {
                    Console.WriteLine("Failed to run the disassembler: " + E.Message);
                }
                progress.Close();
            }*/
        }

        private void barButtonItem9_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            StartHexViewer();
        }

        private void barButtonItem10_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {

            if (FileTools.Instance.Currentfile != "")
            {
                FileTools.Instance.Speedlimit = 0;
                FileTools.Instance.Rpmlimit = 0;
                FileTools.Instance.Rpmlimit2 = 0;
                _workingFile.ParseFile();
                gridControl1.DataSource = _workingFile.Symbols;
                OpenGridViewGroups(gridControl1, 0);

                /*if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC43)
                {
                    LoadMotronic43File(FileTools.Instance.Currentfile, out _workingFile.Symbols, out _workingFile.Axis);
                    gridControl1.DataSource = _workingFile.Symbols;
                    OpenGridViewGroups(gridControl1, 0);
                }
                else if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC44)
                {
                    LoadMotronic44File(FileTools.Instance.Currentfile, out _workingFile.Symbols, out _workingFile.Axis);
                    gridControl1.DataSource = _workingFile.Symbols;
                    gridControl2.DataSource = _workingFile.Axis;
                    OpenGridViewGroups(gridControl1, 0);
                }
                else if (FileTools.Instance.CurrentFiletype == FileType.LH24)
                {
                    LoadLH24File(FileTools.Instance.Currentfile, out _workingFile.Symbols, out _workingFile.Axis);
                    gridControl1.DataSource = _workingFile.Symbols;
                    OpenGridViewGroups(gridControl1, 0);

                }
                else if (FileTools.Instance.CurrentFiletype == FileType.LH242)
                {
                    LoadLH242File(FileTools.Instance.Currentfile, out _workingFile.Symbols, out _workingFile.Axis);
                    gridControl1.DataSource = _workingFile.Symbols;
                    OpenGridViewGroups(gridControl1, 0);

                }*/

            }
        }

        private void barButtonItem12_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (FileTools.Instance.Currentfile != "")
            {
                OpenFileDialog openFileDialog1 = new OpenFileDialog();
                openFileDialog1.Filter = "Binary files|*.bin";
                openFileDialog1.Multiselect = false;
                openFileDialog1.FileName = "";

                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    frmBinCompare bincomp = new frmBinCompare();
                    bincomp.SetCurrentFilename(FileTools.Instance.Currentfile);
                    bincomp.SetCompareFilename(openFileDialog1.FileName);
                    bincomp.CompareFiles();
                    bincomp.ShowDialog();
                }
            }
            else
            {
                frmInfoBox info = new frmInfoBox("No file is currently opened, you need to open a binary file first to compare it to another one!");
            }

        }

        private void barButtonItem13_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (FileTools.Instance.Currentfile != "")
            {
                FileInfo fi = new FileInfo(FileTools.Instance.Currentfile);
                byte[] file = FileTools.Instance.readdatafromfile(FileTools.Instance.Currentfile, 0, 0x10000, false);
                for (int lastoffset = 0xff00; lastoffset <= 0xffff; lastoffset += 2)
                {
                    int checksum = 0;

                    for (int t = 0; t <= lastoffset; t++)
                    {
                        int value = Convert.ToInt32(file.GetValue(t)) * 256 + Convert.ToInt32(file.GetValue(t + 1));
                        checksum ^= value;
                    }
                    Console.WriteLine("XOR checksum (" + lastoffset.ToString("X4") + ") : " + checksum.ToString("X8"));
                }
                for (int lastoffset = 0xfeff; lastoffset <= 0xffff; lastoffset++)
                {
                    int checksum = 0;

                    for (int t = 0; t <= lastoffset; t++)
                    {
                        checksum += Convert.ToInt32(file.GetValue(t));
                    }
                    Console.WriteLine("Addition checksum (" + lastoffset.ToString("X4") + ") : " + checksum.ToString("X8"));
                }
            }
            else
            {
                frmInfoBox info = new frmInfoBox("No file is currently opened, you need to open a binary file first!");
            }
        }

        private void barButtonItem11_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            // load the second binary and compare known maps to it.
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Binary files|*.bin";
            openFileDialog1.Multiselect = false;
            openFileDialog1.FileName = "";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                CompareToFile(openFileDialog1.FileName);
            }

        }

        private void barButtonItem14_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (VerifyCRC())
            {
                frmInfoBox info = new frmInfoBox("Checksums verified ok!");
            }
            else
            {
                frmInfoBox info = new frmInfoBox("Checksums failed!");
            }
        }

        private void barButtonItem15_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (FileTools.Instance.Currentfile != "")
            {
                if (!VerifyCRC())
                {
                    if (MessageBox.Show("Checksums incorrect, do you want to update them?", "Warning!", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        UpdateCRC(FileTools.Instance.Currentfile);
                    }
                }
                else
                {
                    frmInfoBox info = new frmInfoBox("Checksum verified ok!");
                }
            }
            else
            {
                frmInfoBox info = new frmInfoBox("No file is currently opened, you need to open a binary file first!");
            }
        }

        private void barButtonItem16_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            frmSettings set = new frmSettings();
            set.AutoSizeNewWindows = m_appSettings.AutoSizeNewWindows;
            set.AutoSizeColumnsInViewer = m_appSettings.AutoSizeColumnsInWindows;
            set.AutoUpdateChecksum = m_appSettings.AutoChecksum;
            set.HideSymbolWindow = m_appSettings.HideSymbolTable;
            set.ShowGraphsInMapViewer = m_appSettings.ShowGraphs;
            set.UseRedAndWhiteMaps = m_appSettings.ShowRedWhite;
            set.ViewTablesInHex = m_appSettings.Viewinhex;
            set.AutoDockSameFile = m_appSettings.AutoDockSameFile;
            set.AutoDockSameSymbol = m_appSettings.AutoDockSameSymbol;
            set.DisableMapviewerColors = m_appSettings.DisableMapviewerColors;
            set.ShowMapViewersInWindows = m_appSettings.ShowViewerInWindows;
            set.NewPanelsFloating = m_appSettings.NewPanelsFloating;
            set.AutoLoadLastFile = m_appSettings.AutoLoadLastFile;
            set.DefaultViewType = m_appSettings.DefaultViewType;
            set.DefaultViewSize = m_appSettings.DefaultViewSize;
            set.SynchronizeMapviewers = m_appSettings.SynchronizeMapviewers;
            set.FancyDocking = m_appSettings.FancyDocking;
            set.ShowTablesUpsideDown = m_appSettings.ShowTablesUpsideDown;
            set.ShowAddressesInHex = m_appSettings.ShowAddressesInHex;
            set.UseNewMapviewer = m_appSettings.UseNewMapviewer;
            set.ProjectFolder = m_appSettings.ProjectFolder;
            set.RequestProjectNotes = m_appSettings.RequestProjectNotes;
            set.Comport = m_appSettings.Comport;
            set.EnableCommLogging = m_appSettings.EnableCommLogging;
            set.DetermineCommunicationByFileType = m_appSettings.DetermineCommunicationByFileType;
            if (set.ShowDialog() == DialogResult.OK)
            {
                m_appSettings.AutoSizeNewWindows = set.AutoSizeNewWindows;
                m_appSettings.AutoSizeColumnsInWindows = set.AutoSizeColumnsInViewer;
                m_appSettings.AutoChecksum = set.AutoUpdateChecksum;
                m_appSettings.HideSymbolTable = set.HideSymbolWindow;
                m_appSettings.ShowGraphs = set.ShowGraphsInMapViewer;
                m_appSettings.ShowRedWhite = set.UseRedAndWhiteMaps;
                m_appSettings.Viewinhex = set.ViewTablesInHex;
                m_appSettings.DisableMapviewerColors = set.DisableMapviewerColors;
                m_appSettings.AutoDockSameFile = set.AutoDockSameFile;
                m_appSettings.AutoDockSameSymbol = set.AutoDockSameSymbol;
                m_appSettings.ShowViewerInWindows = set.ShowMapViewersInWindows;
                m_appSettings.NewPanelsFloating = set.NewPanelsFloating;
                m_appSettings.DefaultViewType = set.DefaultViewType;
                m_appSettings.DefaultViewSize = set.DefaultViewSize;
                m_appSettings.AutoLoadLastFile = set.AutoLoadLastFile;
                m_appSettings.FancyDocking = set.FancyDocking;
                m_appSettings.ShowTablesUpsideDown = set.ShowTablesUpsideDown;
                m_appSettings.SynchronizeMapviewers = set.SynchronizeMapviewers;
                m_appSettings.ShowAddressesInHex = set.ShowAddressesInHex;
                m_appSettings.UseNewMapviewer = set.UseNewMapviewer;
                m_appSettings.ProjectFolder = set.ProjectFolder;
                m_appSettings.RequestProjectNotes = set.RequestProjectNotes;
                m_appSettings.Comport = set.Comport;
                m_appSettings.DetermineCommunicationByFileType = set.DetermineCommunicationByFileType;
                m_appSettings.EnableCommLogging = set.EnableCommLogging;

                if (!m_appSettings.FancyDocking)
                {
                    dockManager1.DockMode = DevExpress.XtraBars.Docking.Helpers.DockMode.Standard;
                }
                else
                {
                    dockManager1.DockMode = DevExpress.XtraBars.Docking.Helpers.DockMode.VS2005;
                }
            }
            SetFilterMode();

        }

        private void barButtonItem17_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (FileTools.Instance.Currentfile != "")
            {
                frmFirmwareInfo info = new frmFirmwareInfo();
                if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC43)
                {
                    string checksum = "Ok";
                    if (!_workingFile.ValidateChecksum()) checksum = "Failed";
                    info.Checksum = checksum;//CalculateM43CRC(FileTools.Instance.Currentfile).ToString("X4");
                    string hardwareID = string.Empty;
                    string softwareID = string.Empty;
                    string partnumber = string.Empty;
                    string damosinfo = string.Empty;
                    //DecodeM43FileInformation(FileTools.Instance.Currentfile, out hardwareID, out softwareID, out partnumber, out damosinfo);
                    info.PartNumber = _workingFile.GetHardwareID();
                    info.SoftwareID = _workingFile.GetSoftwareVersion();
                    info.HardwareID = _workingFile.GetPartnumber();
                    info.DamosInfo = _workingFile.GetDamosInfo();
                    info.SpeedLimit = FileTools.Instance.Speedlimit;
                    info.RpmLimit = FileTools.Instance.Rpmlimit;

                }
                else if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC210)
                {
                    string checksum = "Ok";
                    if (!_workingFile.ValidateChecksum()) checksum = "Failed";
                    info.Checksum = checksum;
                    string hardwareID = string.Empty;
                    string softwareID = string.Empty;
                    string partnumber = string.Empty;
                    string damosinfo = string.Empty;
                    //DecodeM43FileInformation(FileTools.Instance.Currentfile, out hardwareID, out softwareID, out partnumber, out damosinfo);
                    //info.PartNumber = _workingFile.GetHardwareID();
                    //info.SoftwareID = _workingFile.GetSoftwareVersion();
                    //info.HardwareID = _workingFile.GetPartnumber();
                    //info.DamosInfo = _workingFile.GetDamosInfo();
                    //info.SpeedLimit = FileTools.Instance.Speedlimit;
                    //info.RpmLimit = FileTools.Instance.Rpmlimit;

                }
                else if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC44)
                {
                    M44CRC crcs = CalculateM44CRC(FileTools.Instance.Currentfile);
                    info.Checksum = crcs.Volvocrc1.ToString("X4") + " / " + crcs.Volvocrc2.ToString("X4");
                    info.SpeedLimit = FileTools.Instance.Speedlimit;
                    if (FileTools.Instance.Rpmlimit != -1)
                    {
                        info.RpmLimit = FileTools.Instance.Rpmlimit; // ?
                    }

                    bool found = false;
                    bool autoTransmission = _workingFile.IsAutomaticTransmission(out found);
                    if (found) info.AutomaticTransmission = autoTransmission;
                    string hardwareID = string.Empty;
                    string softwareID = string.Empty;
                    string partnumber = string.Empty;
                    string damosinfo = string.Empty;
                    //DecodeM43FileInformation(FileTools.Instance.Currentfile, out hardwareID, out softwareID, out partnumber, out damosinfo);
                    info.PartNumber = _workingFile.GetHardwareID();
                    info.SoftwareID = _workingFile.GetSoftwareVersion();
                    info.HardwareID = _workingFile.GetPartnumber();
                    info.DamosInfo = _workingFile.GetDamosInfo();
                }
                else if (FileTools.Instance.CurrentFiletype == FileType.LH242)
                {
                    // LH242 calculation
                }
                DialogResult dr = info.ShowDialog();
                if (dr == DialogResult.OK)
                {
                    // in future write new info
                    if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC43)
                    {
                        // write speed & rpm limiter if known
                        if (info.SpeedLimit != 0 && info.SpeedLimiterEnabled())
                        {
                            if (info.SpeedLimit > 0 && info.SpeedLimit <= 255)
                            {
                                WriteSpeedLimiter(info.SpeedLimit);
                                FileTools.Instance.Speedlimit = info.SpeedLimit;
                            }
                        }
                        if (info.RpmLimit != 0 && info.RpmLimiterEnabled())
                        {
                            if (info.RpmLimit > 4000 && info.RpmLimit < 10000)
                            {
                                WriteRpmLimiter(info.RpmLimit);
                                FileTools.Instance.Rpmlimit = info.RpmLimit;
                            }
                        }
                        LoadLimiters();
                    }
                    else if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC44)
                    {
                        // only speed limiter support for now
                        if (info.SpeedLimit != 0 && info.SpeedLimiterEnabled())
                        {
                            if (info.SpeedLimit > 0 && info.SpeedLimit <= 255)
                            {
                                WriteSpeedLimiter(info.SpeedLimit);
                                FileTools.Instance.Speedlimit = info.SpeedLimit;
                            }
                        }
                        if (info.RpmLimit != 0 && info.RpmLimiterEnabled())
                        {
                            if (info.RpmLimit > 4000 && info.RpmLimit <= 7650)
                            {
                                WriteRpmLimiter(info.RpmLimit);
                                FileTools.Instance.Rpmlimit = info.RpmLimit;
                            }
                        }
                        LoadLimiters();
                    }
                }
                else if (dr == DialogResult.Abort)
                {
                    // compare to another file
                    if (info.Compare_file)
                    {
                        CompareToFile(info.FiletoOpen);
                    }
                    else if (info.Open_file)
                    {
                        OpenFile(info.FiletoOpen, true);
                    }
                }
            }
        }

        private void gridControl1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                int[] selectedrows = gridViewSymbols.GetSelectedRows();

                if (selectedrows.Length > 0)
                {
                    int grouplevel = gridViewSymbols.GetRowLevel((int)selectedrows.GetValue(0));
                    if (grouplevel >= gridViewSymbols.GroupCount)
                    {
                        int[] selrows = gridViewSymbols.GetSelectedRows();
                        if (selrows.Length > 0)
                        {
                            SymbolHelper sh = (SymbolHelper)gridViewSymbols.GetRow((int)selrows.GetValue(0));
                            //DataRowView dr = (DataRowView)gridViewSymbols.GetRow((int)selrows.GetValue(0));
                            StartTableViewer(sh);//sh.Varname, sh.Flash_start_address, sh.Cols, sh.Rows, sh.IsSixteenbits);
                        }
                    }
                }
            }
        }

        private void barButtonItem18_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            // check if a certain map exsist in the collection. If so, start it
            if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC43)
            {
                StartTableViewerByName("VE map");
            }
            else if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC44)
            {
                StartTableViewerByName("VE map partload");
            }
        }

        private void barButtonItem22_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            // ve map for knock
            if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC44)
            {
                StartTableViewerByName("VE map (knock)");
            }
        }

        private void barButtonItem19_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC43)
            {
                if (CollectionContainsMap("Ignition map: part throttle"))
                {
                    StartTableViewerByName("Ignition map: part throttle");
                }
                else
                {
                    StartTableViewerByName("Ignition map");
                }
            }
            else if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC44)
            {
                StartTableViewerByName("Ignition map");
            }
        }

        private void barButtonItem21_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            StartTableViewerByName("Boost map");
        }

        private void barButtonItem23_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC44)
            {
                StartTableViewerByName("Dutycycle bias for boost control");
            }
        }

        private void barButtonItem24_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC44)
            {
                StartTableViewerByName("Virtual throttle angle from bypass correction");
            }
        }

        private void barButtonItem26_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC44)
            {
                StartTableViewerByName("Knock detection threshold");
            }
        }

        private void barButtonItem27_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC44)
            {
                StartTableViewerByName("Max. enrichment for knock");
            }

        }

        private void barButtonItem20_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC44)
            {
                StartTableViewerByName("Catalyst safe factors");
            }
        }

        private void barButtonItem28_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC44)
            {
                StartTableViewerByName("Airmass increase for catalyst heating");
            }

        }

        private void barButtonItem30_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC44)
            {
                StartTableViewerByName("Increase of idle target rpm when catalyst heating");
            }

        }

        private void barButtonItem25_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC44)
            {
                StartTableViewerByName("Load value from throttle position (angle) including bypass correction");
            }

        }

        private void barButtonItem29_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "XDF files|*.xdf";
            ofd.Multiselect = false;
            ofd.FileName = "";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                OpenXDFFile(ofd.FileName);
            }
        }

        private void barButtonItem6_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            try
            {
                if (File.Exists(System.Windows.Forms.Application.StartupPath + "//Volvo Motronic 4.3.pdf"))
                {
                    System.Diagnostics.Process.Start(System.Windows.Forms.Application.StartupPath + "//Volvo Motronic 4.3.pdf");
                }
            }
            catch (Exception E2)
            {
                Console.WriteLine(E2.Message);
            }
        }

        private void btnCreateBackupFile_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (FileTools.Instance.Currentfile != string.Empty)
            {
                UpdateCRC(FileTools.Instance.Currentfile);
                if (File.Exists(FileTools.Instance.Currentfile))
                {
                    if (FileTools.Instance.CurrentWorkingProject != "")
                    {
                        if (!Directory.Exists(m_appSettings.ProjectFolder + "\\" + FileTools.Instance.CurrentWorkingProject + "\\Backups")) Directory.CreateDirectory(m_appSettings.ProjectFolder + "\\" + FileTools.Instance.CurrentWorkingProject + "\\Backups");
                        string filename = m_appSettings.ProjectFolder + "\\" + FileTools.Instance.CurrentWorkingProject + "\\Backups\\" + Path.GetFileNameWithoutExtension(GetBinaryForProject(FileTools.Instance.CurrentWorkingProject)) + "-backup-" + DateTime.Now.ToString("MMddyyyyHHmmss") + ".BIN";
                        File.Copy(GetBinaryForProject(FileTools.Instance.CurrentWorkingProject), filename);
                    }
                    else
                    {
                        File.Copy(FileTools.Instance.Currentfile, Path.GetDirectoryName(FileTools.Instance.Currentfile) + "\\" + Path.GetFileNameWithoutExtension(FileTools.Instance.Currentfile) + DateTime.Now.ToString("yyyyMMddHHmmss") + ".binarybackup", true);
                        frmInfoBox info = new frmInfoBox("Backup created: " + Path.GetDirectoryName(FileTools.Instance.Currentfile) + "\\" + Path.GetFileNameWithoutExtension(FileTools.Instance.Currentfile) + DateTime.Now.ToString("yyyyMMddHHmmss") + ".binarybackup");
                    }
                }
            }
        }

        private void btnSearchMaps_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            // ask the user for which value to search and if searching should include symbolnames and/or symbol description
            if (FileTools.Instance.Currentfile != string.Empty)
            {
                SymbolCollection result_Collection = new SymbolCollection();
                frmSearchMaps searchoptions = new frmSearchMaps();
                if (searchoptions.ShowDialog() == DialogResult.OK)
                {
                    frmProgress progress = new frmProgress();
                    progress.SetProgress("Start searching data...");
                    progress.SetProgressPercentage(0);
                    progress.Show();
                    System.Windows.Forms.Application.DoEvents();
                    int cnt = 0;
                    foreach (SymbolHelper sh in _workingFile.Symbols)
                    {
                        progress.SetProgress("Searching " + sh.Varname);
                        progress.SetProgressPercentage((cnt * 100) / _workingFile.Symbols.Count);
                        bool hit_found = false;
                        if (searchoptions.IncludeSymbolNames)
                        {
                            if (searchoptions.SearchForNumericValues)
                            {
                                if (sh.Varname.Contains(searchoptions.NumericValueToSearchFor.ToString()))
                                {
                                    hit_found = true;
                                }
                            }
                            if (searchoptions.SearchForStringValues)
                            {
                                if (searchoptions.StringValueToSearchFor != string.Empty)
                                {
                                    if (sh.Varname.Contains(searchoptions.StringValueToSearchFor))
                                    {
                                        hit_found = true;
                                    }
                                }
                            }
                        }
                        if (searchoptions.IncludeSymbolDescription)
                        {
                            if (searchoptions.SearchForNumericValues)
                            {
                                if (sh.Description.Contains(searchoptions.NumericValueToSearchFor.ToString()))
                                {
                                    hit_found = true;
                                }
                            }
                            if (searchoptions.SearchForStringValues)
                            {
                                if (searchoptions.StringValueToSearchFor != string.Empty)
                                {
                                    if (sh.Description.Contains(searchoptions.StringValueToSearchFor))
                                    {
                                        hit_found = true;
                                    }
                                }
                            }
                        }
                        // now search the symbol data
                        if (sh.Flash_start_address < FileTools.Instance.Currentfile_size)
                        {
                            byte[] symboldata = FileTools.Instance.readdatafromfile(FileTools.Instance.Currentfile, (int)sh.Flash_start_address, sh.Length, sh.IsSixteenbits);
                            if (searchoptions.SearchForNumericValues)
                            {

                                for (int i = 0; i < symboldata.Length; i++)
                                {
                                    float value = Convert.ToInt32(symboldata.GetValue(i));
                                    value *= (float)Helpers.Instance.GetMapCorrectionFactor(sh.Varname);
                                    value += (float)Helpers.Instance.GetMapCorrectionOffset(sh.Varname);
                                    if (value == (float)searchoptions.NumericValueToSearchFor)
                                    {
                                        hit_found = true;
                                    }
                                }
                            }
                            if (searchoptions.SearchForStringValues)
                            {
                                if (searchoptions.StringValueToSearchFor.Length > symboldata.Length)
                                {
                                    // possible...
                                    string symboldataasstring = System.Text.Encoding.ASCII.GetString(symboldata);
                                    if (symboldataasstring.Contains(searchoptions.StringValueToSearchFor))
                                    {
                                        hit_found = true;
                                    }
                                }
                            }
                        }

                        if (hit_found)
                        {
                            // add to collection
                            result_Collection.Add(sh);
                        }
                        cnt++;
                    }
                    progress.Close();
                    if (result_Collection.Count == 0)
                    {
                        frmInfoBox info = new frmInfoBox("No results found...");
                    }
                    else
                    {
                        // start result screen
                        dockManager1.BeginUpdate();
                        try
                        {
                            SymbolTranslator st = new SymbolTranslator();
                            DevExpress.XtraBars.Docking.DockPanel dockPanel = dockManager1.AddPanel(new System.Drawing.Point(-500, -500));
                            CompareResults tabdet = new CompareResults();
                            tabdet.ShowAddressesInHex = m_appSettings.ShowAddressesInHex;
                            tabdet.SetFilterMode(m_appSettings.ShowAddressesInHex);
                            tabdet.Dock = DockStyle.Fill;
                            tabdet.UseForFind = true;
                            tabdet.Filename = FileTools.Instance.Currentfile;
                            tabdet.onSymbolSelect += new CompareResults.NotifySelectSymbol(tabdet_onSymbolSelectForFind);
                            dockPanel.Controls.Add(tabdet);
                            dockPanel.Text = "Search results: " + Path.GetFileName(FileTools.Instance.Currentfile);
                            dockPanel.DockTo(dockManager1, DevExpress.XtraBars.Docking.DockingStyle.Left, 1);

                            dockPanel.Width = 700;

                            System.Data.DataTable dt = new System.Data.DataTable();
                            dt.Columns.Add("SYMBOLNAME");
                            dt.Columns.Add("SRAMADDRESS", Type.GetType("System.Int32"));
                            dt.Columns.Add("FLASHADDRESS", Type.GetType("System.Int32"));
                            dt.Columns.Add("LENGTHBYTES", Type.GetType("System.Int32"));
                            dt.Columns.Add("LENGTHVALUES", Type.GetType("System.Int32"));
                            dt.Columns.Add("DESCRIPTION");
                            dt.Columns.Add("ISCHANGED", Type.GetType("System.Boolean"));
                            dt.Columns.Add("CATEGORY"); //0
                            dt.Columns.Add("DIFFPERCENTAGE", Type.GetType("System.Double"));
                            dt.Columns.Add("DIFFABSOLUTE", Type.GetType("System.Int32"));
                            dt.Columns.Add("DIFFAVERAGE", Type.GetType("System.Double"));
                            dt.Columns.Add("CATEGORYNAME");
                            dt.Columns.Add("SUBCATEGORYNAME");
                            dt.Columns.Add("SymbolNumber1", Type.GetType("System.Int32"));
                            dt.Columns.Add("SymbolNumber2", Type.GetType("System.Int32"));
                            //string category = "";
                            string ht = string.Empty;
                            //double diffperc = 0;
                            //int diffabs = 0;
                            //double diffavg = 0;
                            string cat = string.Empty;
                            string subcat = string.Empty;
                            foreach (SymbolHelper shfound in result_Collection)
                            {
                                string helptext = st.TranslateSymbolToHelpText(shfound.Varname, out ht, out cat, out subcat);
                                if (shfound.Varname.Contains("."))
                                {
                                    try
                                    {
                                        shfound.Category = shfound.Varname.Substring(0, shfound.Varname.IndexOf("."));
                                    }
                                    catch (Exception cE)
                                    {
                                        Console.WriteLine("Failed to assign category to symbol: " + shfound.Varname + " err: " + cE.Message);
                                    }
                                }
                                dt.Rows.Add(shfound.Varname, shfound.Start_address, shfound.Flash_start_address, shfound.Length, shfound.Length, helptext, false, 0, 0, 0, 0, shfound.Category, "", shfound.Symbol_number, shfound.Symbol_number);
                            }
                            tabdet.CompareSymbolCollection = result_Collection;
                            tabdet.OpenGridViewGroups(tabdet.gridControl1, 1);
                            tabdet.gridControl1.DataSource = dt.Copy();

                        }
                        catch (Exception E)
                        {
                            Console.WriteLine(E.Message);
                        }
                        dockManager1.EndUpdate();
                    }
                }
            }
        }

        void tabdet_onSymbolSelectForFind(object sender, CompareResults.SelectSymbolEventArgs e)
        {
            foreach (SymbolHelper sh in _workingFile.Symbols)
            {
                if (sh.Varname == e.SymbolName || sh.UserDescription == e.SymbolName)
                {
                    StartTableViewer(sh);
                    break;
                }
            }
        }

        void tabdet_onStartSymbolViewer(object sender, AxisBrowser.SymbolViewerRequestedEventArgs e)
        {
            foreach (SymbolHelper sh in _workingFile.Symbols)
            {
                if (sh.Varname == e.Mapname || sh.UserDescription == e.Mapname)
                {
                    StartTableViewer(sh);
                    break;
                }
            }
        }

        private void btnExportToS19_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            SaveFileDialog sfd1 = new SaveFileDialog();
            sfd1.Filter = "S19 files|*.S19";
            sfd1.AddExtension = true;
            sfd1.DefaultExt = "S19";
            sfd1.OverwritePrompt = true;
            sfd1.Title = "Export file to motorola S19 format...";
            if (sfd1.ShowDialog() == DialogResult.OK)
            {
                srec2bin srec = new srec2bin();
                srec.ConvertBinToSrec(FileTools.Instance.Currentfile, sfd1.FileName);
            }
        }

        private void btnLookupPartnumber_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            frmPartnumberLookup lookup = new frmPartnumberLookup();
            lookup.ShowDialog();
            if (lookup.Open_File)
            {
                string filename = lookup.GetFileToOpen();
                if (filename != string.Empty)
                {
                    CloseProject();
                    m_appSettings.Lastprojectname = "";
                    OpenFile(filename, false);
                    m_appSettings.LastOpenedType = 0;

                }
            }
            else if (lookup.Compare_File)
            {
                string filename = lookup.GetFileToOpen();
                if (filename != string.Empty)
                {
                    CompareToFile(filename);
                }
            }
            else if (lookup.CreateNewFile)
            {
                string filename = lookup.GetFileToOpen();
                if (filename != string.Empty)
                {
                    CloseProject();
                    m_appSettings.Lastprojectname = "";
                    File.Copy(filename, lookup.FileNameToSave);
                    OpenFile(lookup.FileNameToSave, true);
                    m_appSettings.LastOpenedType = 0;
                }
            }
        }

        private void btnOverboostMap_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            StartTableViewerByName("Overboost map");
        }

        private void btnMAFToLoad_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            StartTableViewerByName("MAF to Load conversion map");
        }

        private void btnMAFLimiter_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            StartTableViewerByName("MAF limit");
        }

        void tabdet_onSymbolSelect(object sender, CompareResults.SelectSymbolEventArgs e)
        {
            if (!e.ShowDiffMap)
            {
                float[] xaxis = new float[1];
                float[] yaxis = new float[1];
                xaxis.SetValue(0, 0);
                yaxis.SetValue(0, 0);
                string xdescr = string.Empty;
                string ydescr = string.Empty;
                StartCompareMapViewer(e.SymbolName, e.Filename, e.SymbolAddress, e.SymbolLength, e.Symbols, e.Axis, e.Symbolnumber2);
                Helpers.Instance.GetAxisValues(FileTools.Instance.Currentfile, _workingFile.Symbols, _workingFile.Axis, e.SymbolName, Helpers.Instance.GetSymbolAddress(_workingFile.Symbols, e.SymbolName), 1, 1, out xaxis, out yaxis, out xdescr, out ydescr);
                foreach (SymbolHelper sh in _workingFile.Symbols)
                {
                    if (sh.Varname == e.SymbolName)
                    {
                        StartTableViewer(sh);
                        break;
                        //StartTableViewer(e.SymbolName, Helpers.Instance.GetSymbolAddress(_workingFile.Symbols, e.SymbolName), xaxis.Length, yaxis.Length, false);
                    }
                }
                //StartTableViewer(e.SymbolName);
            }
            else
            {
                // show difference map
                //TODO: op symbolnumbers doen
                StartCompareDifferenceViewer(e.SymbolName, e.Filename, /*Helpers.Instance.GetSymbolAddress(_workingFile.Symbols, e.SymbolName)*/ e.SymbolAddress, e.SymbolLength);
            }
        }

        private void gridViewSymbols_CustomDrawCell(object sender, DevExpress.XtraGrid.Views.Base.RowCellCustomDrawEventArgs e)
        {
            /* if (e.Column.Name == gridColumn2.Name)
             {
                 try
                 {
                     if (e.CellValue != null)
                     {
                         if (e.CellValue != DBNull.Value)
                         {
                             int value = Convert.ToInt32(e.CellValue);
                             e.DisplayText = value.ToString("X4");
                         }
                     }
                 }
                 catch (Exception E)
                 {
                     Console.WriteLine("Failed to display address in hex: " + E.Message);
                 }
             }*/
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            // check for updates
            try
            {
                m_msiUpdater = new msiupdater(new Version(System.Windows.Forms.Application.ProductVersion));
                m_msiUpdater.Apppath = System.Windows.Forms.Application.UserAppDataPath;
                m_msiUpdater.onDataPump += new msiupdater.DataPump(m_msiUpdater_onDataPump);
                m_msiUpdater.onUpdateProgressChanged += new msiupdater.UpdateProgressChanged(m_msiUpdater_onUpdateProgressChanged);
                m_msiUpdater.CheckForUpdates("Global", "http://trionic.mobixs.eu/Motronic/", "", "", false);
            }
            catch (Exception E)
            {
                Console.WriteLine(E.Message);
            }
        }

        void dockPanel_ClosedPanel(object sender, DockPanelEventArgs e)
        {
            // force close of the file that the hexviewer had open!
            if (sender is DockPanel)
            {
                DockPanel pnl = (DockPanel)sender;

                foreach (Control c in pnl.Controls)
                {
                    if (c is HexViewer)
                    {
                        HexViewer vwr = (HexViewer)c;
                        vwr.CloseFile();
                    }
                    else if (c is ctrlCompressorMapEx)
                    {
                        ctrlCompressorMapEx plot = (ctrlCompressorMapEx)c;
                        plot.ReleaseResources();
                    }
                    else if (c is DevExpress.XtraBars.Docking.DockPanel)
                    {
                        DevExpress.XtraBars.Docking.DockPanel tpnl = (DevExpress.XtraBars.Docking.DockPanel)c;
                        foreach (Control c2 in tpnl.Controls)
                        {
                            if (c2 is HexViewer)
                            {
                                HexViewer vwr2 = (HexViewer)c2;
                                vwr2.CloseFile();
                            }
                            else if (c2 is ctrlCompressorMapEx)
                            {
                                ctrlCompressorMapEx plot = (ctrlCompressorMapEx)c2;
                                plot.ReleaseResources();
                            }

                        }
                    }
                    else if (c is DevExpress.XtraBars.Docking.ControlContainer)
                    {
                        DevExpress.XtraBars.Docking.ControlContainer cntr = (DevExpress.XtraBars.Docking.ControlContainer)c;
                        foreach (Control c3 in cntr.Controls)
                        {
                            if (c3 is HexViewer)
                            {
                                HexViewer vwr3 = (HexViewer)c3;
                                vwr3.CloseFile();
                            }
                            else if (c3 is ctrlCompressorMapEx)
                            {
                                ctrlCompressorMapEx plot = (ctrlCompressorMapEx)c3;
                                plot.ReleaseResources();
                            }

                        }
                    }
                }

                // remove the panel from the dockmanager
                dockManager1.RemovePanel(pnl);
            }

        }

        void cm_onRefreshData(object sender, EventArgs e)
        {
            if (sender is ctrlCompressorMapEx)
            {
                ctrlCompressorMapEx cm = (ctrlCompressorMapEx)sender;
                string mapName = "Boost map";
                double[] boost_req = new double[16];
                byte[] tryck_mat = FileTools.Instance.readdatafromfile(FileTools.Instance.Currentfile, Helpers.Instance.GetSymbolAddress(_workingFile.Symbols, mapName), Helpers.Instance.GetSymbolLength(_workingFile.Symbols, mapName), false);
                // now get the doubles from it
                for (int i = 0; i < 16; i++)
                {
                    double val = Convert.ToDouble(tryck_mat[7 * 16 + i]);
                    val /= 100;
                    val -= 1;
                    boost_req.SetValue(val, i);
                }

                cm.Boost_request = boost_req;
                cm.Redraw();
            }
        }

        private void btnScanLibrary_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            //ScanLibrary(@"C:\Documents and Settings\Guido.MOBICOACH\My Documents\Prive\Volvo\Motronic 4.3\Library", FileType.MOTRONIC43);
            //BuildInfoMaps(@"C:\Documents and Settings\Guido.MOBICOACH\My Documents\Prive\Volvo\Motronic 4.3\Library\out");

        }

        private void BuildInfoMaps(string path2build)
        {
            string[] binfiles = Directory.GetFiles(path2build, "*.bin", SearchOption.AllDirectories);
            foreach(string file in binfiles)
            {

            }
        }

        private void ScanLibrary(string path2scan, FileType type)
        {

            string[] binfiles = Directory.GetFiles(path2scan, "*.bin", SearchOption.AllDirectories);

            foreach (string binfile in binfiles)
            {
                try
                {
                    bool _supportsDamos = false;
                    FileInfo fi = new FileInfo(binfile);
                    if(fi.Length != 0x10000 && type == FileType.MOTRONIC43) continue;
                    if (fi.Length != 0x20000 && type == FileType.MOTRONIC44) continue;
                    OpenFile(binfile, true);

                    if (type == FileType.MOTRONIC44)
                    {
                        // check wether this supports the dam file
                        foreach (SymbolHelper sh in _workingFile.Symbols)
                        {
                            if (sh.Flash_start_address == 0xE376 && sh.Length == 0x100 /* && sh.UserDescription == ""*/)
                            {
                                // yes 
                                _supportsDamos = true;
                            }
                        }
                    }

                    Application.DoEvents();
                    bool _logFile = false;
                    if (type == FileType.MOTRONIC43)
                    {
                        if (!DoesSymbolExist("Boost map")) _logFile = true;
                        if (!DoesSymbolExist("Overboost map")) _logFile = true;
                        if (!DoesSymbolExist("WOT enrichment")) _logFile = true;
                        if (!DoesSymbolExist("WOT ignition")) _logFile = true;
                        if (!DoesSymbolExist("VE map")) _logFile = true;
                        if (_logFile)
                        {
                            WriteToLog("Handling file: " + Path.GetFileName(binfile));
                            if (!DoesSymbolExist("Boost map")) WriteToLog("No boost map found: " + Path.GetFileName(binfile));
                            if (!DoesSymbolExist("Overboost map")) WriteToLog("No overboost map found: " + Path.GetFileName(binfile));
                            if (!DoesSymbolExist("WOT enrichment")) WriteToLog("No WOT enrichment map found: " + Path.GetFileName(binfile));
                            if (!DoesSymbolExist("WOT ignition")) WriteToLog("No WOT ignition map found: " + Path.GetFileName(binfile));
                            if (!DoesSymbolExist("VE map")) WriteToLog("No VE map found: " + Path.GetFileName(binfile));
                            WriteToLog("Rpm limiter 1: " + FileTools.Instance.Rpmlimit.ToString());
                            WriteToLog("Rpm limiter 2: " + FileTools.Instance.Rpmlimit2.ToString());
                            WriteToLog("Speed limiter: " + FileTools.Instance.Speedlimit.ToString());
                        }
                    }
                    // send it to the subfolder "out" with the correct fileformat
                    string hwid = string.Empty;
                    string swid = string.Empty;
                    string partnumber = string.Empty;
                    string damosinfo = string.Empty;
                    damosinfo = _workingFile.GetDamosInfo();
                    partnumber = _workingFile.GetPartnumber();
                    hwid = _workingFile.GetHardwareID();
                    swid = _workingFile.GetSoftwareVersion();
                    string outfile = Path.Combine(path2scan + "\\out", hwid.Trim() + "_" + swid.Trim() + "_0" + ".BIN");
                    if (_supportsDamos)
                    {
                        outfile = Path.Combine(path2scan + "\\out", hwid.Trim() + "_" + swid.Trim() + "_1" + ".BIN");
                    }
                    if (!File.Exists(outfile))
                    {
                        WriteToLog("Adding to output: " + binfile + " partnumber: " + hwid.Trim());
                        File.Copy(binfile, outfile);
                        //<GS-04082011>
                        //SaveCodeForLibrary(Path.GetDirectoryName(outfile) + "\\" + Path.GetFileNameWithoutExtension(outfile) + ".cs", partnumber, swid);
                        SaveCodeForLibrary(Path.GetDirectoryName(outfile) + "\\code.cs", partnumber, swid);
                    }
                    else
                    {
                        WriteToLog("File already existed with partnumber: " + hwid.Trim() + " " + binfile);
                    }
                }
                catch (Exception E)
                {
                    Console.WriteLine("Failed to handle binary (" + E.Message + ") : " + Path.GetFileName(binfile));
                }
            }
        }

        private void SaveCodeForLibrary(string filename, string ptnr, string swid)
        {
            if (ptnr.Length > 0 || swid.Length > 0)
            {
                using (StreamWriter sw = new StreamWriter(filename, true))
                {
                    //sw.WriteLine("\t\t" + "SymbolHelper sh = new SymbolHelper();");
                    //sw.WriteLine("\t\t" + "AxisHelper ah = new AxisHelper();");
                    sw.WriteLine("\t\t" + "else if (partnumber == \"" + ptnr + "\" && swid == \"" + swid + "\")");
                    sw.WriteLine("\t\t" + "{");
                    foreach (SymbolHelper sh in _workingFile.Symbols)
                    {
                        sw.WriteLine("\t\t" + "sh = new SymbolHelper();");
                        sw.WriteLine("\t\t" + "sh.Varname = \"" + sh.Varname + "\";");
                        sw.WriteLine("\t\t" + "sh.Category = \"" + sh.Category + "\";");
                        sw.WriteLine("\t\t" + "sh.Flash_start_address = 0x" + sh.Flash_start_address.ToString("X8") + ";");
                        sw.WriteLine("\t\t" + "sh.Length = 0x" + sh.Length.ToString("X8") + ";");
                        sw.WriteLine("\t\t" + "sh.Start_address = 0x" + sh.Start_address.ToString("X8") + ";");
                        sw.WriteLine("\t\t" + "sh.Symbol_number = 0x" + sh.Symbol_number.ToString("X8") + ";");
                        sw.WriteLine("\t\t" + "sh.X_axis_address = 0x" + sh.X_axis_address.ToString("X8") + ";");
                        sw.WriteLine("\t\t" + "sh.X_axis_length = 0x" + sh.X_axis_length.ToString("X8") + ";");
                        sw.WriteLine("\t\t" + "sh.Y_axis_address = 0x" + sh.Y_axis_address.ToString("X8") + ";");
                        sw.WriteLine("\t\t" + "sh.Y_axis_length = 0x" + sh.Y_axis_length.ToString("X8") + ";");
                        sw.WriteLine("\t\t" + "sh.IsAxisSymbol = " + sh.IsAxisSymbol.ToString().ToLower() + ";");
                        sw.WriteLine("\t\t" + "sh.IsSixteenbits = " + sh.IsSixteenbits.ToString().ToLower() + ";");
                        sw.WriteLine("\t\t" + "sc.Add(sh);");
                    }
                    foreach (AxisHelper ah in _workingFile.Axis)
                    {
                        sw.WriteLine("\t\t" + "ah = new AxisHelper();");
                        sw.WriteLine("\t\t" + "ah.Addressinfile = 0x" + ah.Addressinfile.ToString("X8") + ";");
                        sw.WriteLine("\t\t" + "ah.Identifier = 0x" + ah.Identifier.ToString("X8") + ";");
                        sw.WriteLine("\t\t" + "ah.Length = 0x" + ah.Length.ToString("X8") + ";");
                        sw.WriteLine("\t\t" + "ac.Add(ah);");
                    }
                    sw.WriteLine("\t\t" + "}");
                }
            }
        }

        private void btnImportXMLDescriptor_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            ImportXMLDescriptor();
        }

        private void gridViewSymbols_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            if (e.Column.Name == gcUserDescription.Name)
            {
                // save a new repository item
                SaveAdditionalSymbols();

            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_ecucomms != null)
            {
                if (_ecucomms.CommunicationRunning)
                {
                    _ecucomms.StopCommunication();
                    _ecucomms.EnableLogging = false;
                }
            }

            if (FileTools.Instance.CurrentWorkingProject != "")
            {
                CloseProject();
            }
            SaveLayoutFiles();

            SaveRealtimeTable(Application.StartupPath + "\\rtsymbols.txt");
        }

        private void btnVINDecoder_ItemClick(object sender, ItemClickEventArgs e)
        {
            frmDecodeVIN decode = new frmDecodeVIN();
            decode.ShowDialog();

        }

        private void gridView1_DoubleClick_1(object sender, EventArgs e)
        {
            // start axis browser
            int[] selectedrows = gridView1.GetSelectedRows();

            if (selectedrows.Length > 0)
            {
                int grouplevel = gridView1.GetRowLevel((int)selectedrows.GetValue(0));
                if (grouplevel >= gridView1.GroupCount)
                {
                    int[] selrows = gridView1.GetSelectedRows();
                    if (selrows.Length > 0)
                    {
                        AxisHelper ah = (AxisHelper)gridView1.GetRow((int)selrows.GetValue(0));
                        //DataRowView dr = (DataRowView)gridViewSymbols.GetRow((int)selrows.GetValue(0));
                        StartAxisViewer(ah);
                    }
                }
            }
        }

        private void barButtonItem5_ItemClick(object sender, ItemClickEventArgs e)
        {
            try
            {
                if (File.Exists(System.Windows.Forms.Application.StartupPath + "//MotronicSuite user manual.pdf"))
                {
                    System.Diagnostics.Process.Start(System.Windows.Forms.Application.StartupPath + "//MotronicSuite user manual.pdf");
                }
            }
            catch (Exception E2)
            {
                Console.WriteLine(E2.Message);
            }
        }


        #endregion

        #region skinning
        private void SetToolstripTheme()
        {
            //Console.WriteLine("Rendermode was: " + ToolStripManager.RenderMode.ToString());
            //Console.WriteLine("Visual styles: " + ToolStripManager.VisualStylesEnabled.ToString());
            //Console.WriteLine("Skinname: " + appSettings.SkinName);
            //Console.WriteLine("Backcolor: " + defaultLookAndFeel1.LookAndFeel.Painter.Button.DefaultAppearance.BackColor.ToString());
            //Console.WriteLine("Backcolor2: " + defaultLookAndFeel1.LookAndFeel.Painter.Button.DefaultAppearance.BackColor2.ToString());
            try
            {
                Skin currentSkin = CommonSkins.GetSkin(defaultLookAndFeel1.LookAndFeel);
                Color c = currentSkin.TranslateColor(SystemColors.Control);
                ToolStripManager.RenderMode = ToolStripManagerRenderMode.Professional;
                ProfColorTable profcolortable = new ProfColorTable();
                profcolortable.CustomToolstripGradientBegin = c;
                profcolortable.CustomToolstripGradientMiddle = c;
                profcolortable.CustomToolstripGradientEnd = c;
                ToolStripManager.Renderer = new ToolStripProfessionalRenderer(profcolortable);
            }
            catch (Exception)
            {

            }

        }

        void InitSkins()
        {
            ribbonControl1.ForceInitialize();
            //barManager1.ForceInitialize();
            BarButtonItem item;

            DevExpress.Skins.SkinManager.Default.RegisterAssembly(typeof(DevExpress.UserSkins.BonusSkins).Assembly);
            DevExpress.Skins.SkinManager.Default.RegisterAssembly(typeof(DevExpress.UserSkins.OfficeSkins).Assembly);

            foreach (DevExpress.Skins.SkinContainer cnt in DevExpress.Skins.SkinManager.Default.Skins)
            {
                item = new BarButtonItem();
                item.Caption = cnt.SkinName;
                //iPaintStyle.AddItem(item);
                rbnPageSkins.ItemLinks.Add(item);
                item.ItemClick += new ItemClickEventHandler(OnSkinClick);
            }

            try
            {
                if (IsChristmasTime())
                {
                    // set chrismas skin
                    DevExpress.LookAndFeel.UserLookAndFeel.Default.SetSkinStyle("Xmas 2008 Blue"); // don't save
                }
                else if (IsHalloweenTime())
                {
                    // set Halloween skin
                    DevExpress.LookAndFeel.UserLookAndFeel.Default.SetSkinStyle("Pumpkin"); // don't save
                }
                else if (IsValetineTime())
                {
                    DevExpress.LookAndFeel.UserLookAndFeel.Default.SetSkinStyle("Valentine"); // don't save
                }
                else
                {
                    if (m_appSettings.Skinname == "")
                    {
                        m_appSettings.Skinname = "Office 2007 Blue";
                    }
                    DevExpress.LookAndFeel.UserLookAndFeel.Default.SetSkinStyle(m_appSettings.Skinname);
                }
            }
            catch (Exception E)
            {
                Console.WriteLine(E.Message);
            }
            SetToolstripTheme();
        }

        private void ShowChristmasWish()
        {
            int newyear = DateTime.Now.Year + 1;
            frmInfoBox info = new frmInfoBox("Merry christmas and a happy " + newyear.ToString("D4") + "\rDilemma");
        }

        private bool IsChristmasTime()
        {
            // test, return true
            if (DateTime.Now.Month == 12 && DateTime.Now.Day >= 20 && DateTime.Now.Day <= 26)
            {
                return true;
            }
            return false;
        }

        private bool IsHalloweenTime()
        {
            // test, return true
            if (DateTime.Now.Month == 10 && DateTime.Now.Day >= 30 && DateTime.Now.Day <= 31)
            {
                return true;
            }
            return false;
        }

        private bool IsValetineTime()
        {
            // test, return true
            if (DateTime.Now.Month == 2 && DateTime.Now.Day >= 13 && DateTime.Now.Day <= 14)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// OnSkinClick: Als er een skin gekozen wordt door de gebruiker voer deze
        /// dan door in de user interface
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnSkinClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            string skinName = e.Item.Caption;
            DevExpress.LookAndFeel.UserLookAndFeel.Default.SetSkinStyle(skinName);
            m_appSettings.Skinname = skinName;
            SetToolstripTheme();
        }

        #endregion

        #region automatic updates

        private void StartReleaseNotesViewer(string xmlfilename, string version)
        {
            dockManager1.BeginUpdate();
            DockPanel dp = dockManager1.AddPanel(DockingStyle.Right);
            dp.ClosedPanel += new DockPanelEventHandler(dockPanel_ClosedPanel);
            dp.Tag = xmlfilename;
            ctrlReleaseNotes mv = new ctrlReleaseNotes();
            mv.LoadXML(xmlfilename);
            mv.Dock = DockStyle.Fill;
            dp.Width = 500;
            dp.Text = "Release notes: " + version;
            dp.Controls.Add(mv);
            dockManager1.EndUpdate();
        }

        void m_msiUpdater_onUpdateProgressChanged(msiupdater.MSIUpdateProgressEventArgs e)
        {

        }

        public void GetRSSFeeds(Version newversion)
        {
            try
            {
                RSS2HTMLScoutLib.RSS2HTMLScout RSS2HTML = new RSS2HTMLScoutLib.RSS2HTMLScout();
                //RSS2HTML.ForceRefresh = true;
                RSS2HTML.ItemsPerFeed = 10; // limit 5 latest items per feed
                RSS2HTML.MainHeader = "<html><head><title>Motronic Suite changelog</title><!-- CSS source code will be inserted here -->{CSS}<!-- HTML page encoding. please change if needed --><!-- <meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\"> --></head><body>";
                //RSS2HTML.ChannelHeader = "<div class=\"ChannelHeader\"><table width=\"100%\" border=\"0\"><tr><td valign=\"middle\" align=\"left\"><a href=\"{LINK}\">{IMAGE}</a></td>      <td width=\"99%\" vAlign=middle align=middle><br><h3>{TITLE}</h3></td></tr></table></div>";
                RSS2HTML.ChannelHeader = "<div class=\"ChannelHeader\"><table width=\"100%\" border=\"0\"><tr><td valign=\"middle\" align=\"left\"><a href=\"{LINK}\"> {IMAGE}</a></td>      <td width=\"99%\" vAlign=middle align=middle><br><h2>{TITLE}</h2></td></tr></table>{DESCRIPTION}</div>";
                RSS2HTML.EnclosureTemplate = "<a href=\"{LINK}\">Image: {TITLE} ({LENGTH})</a>";
                RSS2HTML.ErrorMessageTemplate = "<p>Following feeds can not be displayed:<br>{FAILEDFEEDS}<br></p>";
                RSS2HTML.ItemTemplate = "<div class=\"ItemHeader\"><a href=\"{LINK}\">{TITLE}</a></div><div class=\"ItemDescription\">{DESCRIPTION}</div><div class=\"ItemFooter\">{AUTHOR} {DATE} {TIME} <a href=\"{COMMENTS}\">{COMMENTS} {ENCLOSURE}</a></div>";
                RSS2HTML.NewItemTemplate = "<div style=\"font-style: italic; background-color: #ead2d9\" class=\"NewItemHeader\"><a href=\"{LINK}\">{TITLE}</a></div><div class=\"NewItemDescription\">{DESCRIPTION}</div><div class=\"NewItemFooter\">{AUTHOR} {DATE} {TIME} <a href=\"{COMMENTS}\">{COMMENTS} {ENCLOSURE}</a></div>";
                RSS2HTML.MainFooter = "</body></html>";
                RSS2HTML.AddFeed("http://trionic.mobixs.eu/motronic/" + newversion.ToString() + "/Notes.xml", 180); // ' update every 180 minutes (3 hours)
                RSS2HTML.Execute();
                RSS2HTML.SaveOutputToFile(System.Windows.Forms.Application.UserAppDataPath + "\\Motronic.html");
            }
            catch (Exception E)
            {
                Console.WriteLine("Error getting RSS feeds: " + E.Message);
            }

        }

        private void SetStatusText(string text)
        {
            barStaticItem1.Caption = text;
            System.Windows.Forms.Application.DoEvents();
            //Console.WriteLine(text);
        }

        private void ShowChangeLog(Version newversion)
        {
            try
            {
                if (File.Exists(System.Windows.Forms.Application.UserAppDataPath + "\\Motronic.html"))
                {
                    File.Delete(System.Windows.Forms.Application.UserAppDataPath + "\\Motronic.html");
                }
                GetRSSFeeds(newversion);
                if (File.Exists(System.Windows.Forms.Application.UserAppDataPath + "\\Motronic.html"))
                {
                    DevExpress.XtraBars.Docking.DockPanel panel = dockManager1.AddPanel(DevExpress.XtraBars.Docking.DockingStyle.Right);
                    panel.Text = "Change history";
                    WebBrowser wb = new WebBrowser();
                    panel.Width = 600;
                    wb.Dock = DockStyle.Fill;
                    panel.Controls.Add(wb);
                    panel.Show();
                    //Console.WriteLine("WebPanel Shown");
                    wb.Navigate(System.Windows.Forms.Application.UserAppDataPath + "\\Motronic.html");
                }
            }
            catch (Exception E)
            {
                Console.WriteLine(E.Message);
            }

        }

        void m_msiUpdater_onDataPump(msiupdater.MSIUpdaterEventArgs e)
        {
            SetStatusText(e.Data);
            if (e.UpdateAvailable)
            {
                if (e.XMLFile != "" && e.Version.ToString() != "0.0")
                {
                    if (!this.IsDisposed)
                    {
                        try
                        {
                            this.Invoke(m_DelegateStartReleaseNotePanel, e.XMLFile, e.Version.ToString());
                        }
                        catch (Exception E)
                        {
                            Console.WriteLine(E.Message);
                        }
                    }
                }

                //this.Invoke(m_DelegateShowChangeLog, e.Version);
                frmUpdateAvailable frmUpdate = new frmUpdateAvailable();
                frmUpdate.SetVersionNumber(e.Version.ToString());
                if (m_msiUpdater != null)
                {
                    m_msiUpdater.Blockauto_updates = false;
                }
                if (frmUpdate.ShowDialog() == DialogResult.OK)
                {
                    if (m_msiUpdater != null)
                    {
                        m_msiUpdater.ExecuteUpdate(e.Version);
                        System.Windows.Forms.Application.Exit();
                    }
                }
                else
                {
                    // gebruiker heeft nee gekozen, niet meer lastig vallen
                    if (m_msiUpdater != null)
                    {
                        m_msiUpdater.Blockauto_updates = false;
                    }
                }
            }
            // test
            //frmUpdateAvailable frmUpdatetest = new frmUpdateAvailable();
            //frmUpdatetest.ShowDialog();
            // test
        }

        private void barButtonItem4_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            try
            {
                if (m_msiUpdater != null)
                {
                    m_msiUpdater.CheckForUpdates("Global", "http://trionic.mobixs.eu/Motronic/", "", "", false);
                }
            }
            catch (Exception E)
            {
                Console.WriteLine(E.Message);
            }

        }

        #endregion

        #region project management

        private string MakeDirName(string dirname)
        {
            string retval = dirname;
            retval = retval.Replace(@"\", "");
            retval = retval.Replace(@"/", "");
            retval = retval.Replace(@":", "");
            retval = retval.Replace(@"*", "");
            retval = retval.Replace(@"?", "");
            retval = retval.Replace(@">", "");
            retval = retval.Replace(@"<", "");
            retval = retval.Replace(@"|", "");
            return retval;
        }

        private void btnCreateProject_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            // show the project properties screen for the user to fill in
            // if a bin file is loaded, ask the user whether this should be the new projects binary file
            // the project XML should contain a reference to this binfile as well as a lot of other stuff
            frmProjectProperties projectprops = new frmProjectProperties();
            if (FileTools.Instance.Currentfile != string.Empty)
            {
                projectprops.BinaryFile = FileTools.Instance.Currentfile;
                string hardwareID = _workingFile.GetHardwareID();
                string softwareID = _workingFile.GetSoftwareVersion();
                string partnumber = _workingFile.GetPartnumber();
                string damosinfo = _workingFile.GetDamosInfo();
                projectprops.CarModel = "Volvo " + hardwareID.Trim();
                projectprops.ProjectName = partnumber.Trim() + " " + softwareID.Trim();
            }
            if (projectprops.ShowDialog() == DialogResult.OK)
            {
                if (!Directory.Exists(m_appSettings.ProjectFolder)) Directory.CreateDirectory(m_appSettings.ProjectFolder);
                // create a new folder with these project properties.
                // also copy the binary file into the subfolder for this project
                if (Directory.Exists(m_appSettings.ProjectFolder + "\\" + MakeDirName(projectprops.ProjectName)))
                {
                    frmInfoBox info = new frmInfoBox("The chosen projectname already exists, please choose another one");
                    //TODO: reshow the dialog
                }
                else
                {
                    // create the project
                    Directory.CreateDirectory(m_appSettings.ProjectFolder + "\\" + MakeDirName(projectprops.ProjectName));
                    // copy the selected binary file to this folder
                    string binfilename = m_appSettings.ProjectFolder + "\\" + MakeDirName(projectprops.ProjectName) + "\\" + Path.GetFileName(projectprops.BinaryFile);
                    File.Copy(projectprops.BinaryFile, binfilename);
                    // now create the projectproperties.xml in this new folder
                    System.Data.DataTable dtProps = new System.Data.DataTable("T5PROJECT");
                    dtProps.Columns.Add("CARMAKE");
                    dtProps.Columns.Add("CARMODEL");
                    dtProps.Columns.Add("CARMY");
                    dtProps.Columns.Add("CARVIN");
                    dtProps.Columns.Add("NAME");
                    dtProps.Columns.Add("BINFILE");
                    dtProps.Columns.Add("VERSION");
                    dtProps.Rows.Add(projectprops.CarMake, projectprops.CarModel, projectprops.CarMY, projectprops.CarVIN, MakeDirName(projectprops.ProjectName), binfilename, projectprops.Version);
                    dtProps.WriteXml(m_appSettings.ProjectFolder + "\\" + MakeDirName(projectprops.ProjectName) + "\\projectproperties.xml");
                    OpenProject(projectprops.ProjectName); //?
                }
            }
        }

        

        private void OpenProject(string projectname)
        {
            //TODO: Are there pending changes in the optionally currently opened binary file / project?

            //TODO: open a selected project
            //frmInfoBox info = new frmInfoBox("Opening project: " + projectname);
            if (Directory.Exists(m_appSettings.ProjectFolder + "\\" + projectname))
            {
                m_appSettings.LastOpenedType = 1;
                FileTools.Instance.CurrentWorkingProject = projectname;
                FileTools.Instance.ProjectLog.OpenProjectLog(m_appSettings.ProjectFolder + "\\" + projectname);
                //Load the binary file that comes with this project
                LoadBinaryForProject(projectname);
                //LoadAFRMapsForProject(projectname); // <GS-27072010> TODO: nog bekijken voor T7
                if (FileTools.Instance.Currentfile != string.Empty)
                {
                    // transaction log <GS-15032010>
                    FileTools.Instance.ProjectTransactionLog = new TransactionLog();
                    if (FileTools.Instance.ProjectTransactionLog.OpenTransActionLog(m_appSettings.ProjectFolder, projectname))
                    {
                        FileTools.Instance.ProjectTransactionLog.ReadTransactionFile();
                        if (FileTools.Instance.ProjectTransactionLog.TransCollection.Count > 2000)
                        {
                            frmProjectTransactionPurge frmPurge = new frmProjectTransactionPurge();
                            frmPurge.SetNumberOfTransactions(FileTools.Instance.ProjectTransactionLog.TransCollection.Count);
                            if (frmPurge.ShowDialog() == DialogResult.OK)
                            {
                                FileTools.Instance.ProjectTransactionLog.Purge();
                            }
                        }
                    }
                    // transaction log <GS-15032010>
                    btnCloseProject.Enabled = true;
                    btnAddNoteToProject.Enabled = true;
                    btnEditProject.Enabled = true;
                    btnShowProjectLogbook.Enabled = true;
                    btnProduceLatestBinary.Enabled = true;
                    //btncreateb                    
                    btnRebuildFile.Enabled = true;
                    CreateProjectBackupFile();
                    UpdateRollbackForwardControls();
                    m_appSettings.Lastprojectname = FileTools.Instance.CurrentWorkingProject;
                    this.Text = "MotronicSuite [Project: " + projectname + "]";
                }
            }
        }

        private void UpdateRollbackForwardControls()
        {
            btnRollback.Enabled = false;
            btnRollforward.Enabled = false;
            btnShowTransactionLog.Enabled = false;

            for (int t = FileTools.Instance.ProjectTransactionLog.TransCollection.Count - 1; t >= 0; t--)
            {
                if (!btnShowTransactionLog.Enabled) btnShowTransactionLog.Enabled = true;
                if (FileTools.Instance.ProjectTransactionLog.TransCollection[t].IsRolledBack)
                {
                    btnRollforward.Enabled = true;
                }
                else
                {
                    btnRollback.Enabled = true;
                }
            }
        }

        private void CreateProjectBackupFile()
        {
            // create a backup file automatically! <GS-16032010>
            if (!Directory.Exists(m_appSettings.ProjectFolder + "\\" + FileTools.Instance.CurrentWorkingProject + "\\Backups")) Directory.CreateDirectory(m_appSettings.ProjectFolder + "\\" + FileTools.Instance.CurrentWorkingProject + "\\Backups");
            string filename = m_appSettings.ProjectFolder + "\\" + FileTools.Instance.CurrentWorkingProject + "\\Backups\\" + Path.GetFileNameWithoutExtension(GetBinaryForProject(FileTools.Instance.CurrentWorkingProject)) + "-backup-" + DateTime.Now.ToString("MMddyyyyHHmmss") + ".BIN";
            File.Copy(GetBinaryForProject(FileTools.Instance.CurrentWorkingProject), filename);
            if (FileTools.Instance.CurrentWorkingProject != string.Empty)
            {
                FileTools.Instance.ProjectLog.WriteLogbookEntry(LogbookEntryType.BackupfileCreated, filename);
            }


        }

        private void LoadBinaryForProject(string projectname)
        {
            if (File.Exists(m_appSettings.ProjectFolder + "\\" + projectname + "\\projectproperties.xml"))
            {
                System.Data.DataTable projectprops = new System.Data.DataTable("T5PROJECT");
                projectprops.Columns.Add("CARMAKE");
                projectprops.Columns.Add("CARMODEL");
                projectprops.Columns.Add("CARMY");
                projectprops.Columns.Add("CARVIN");
                projectprops.Columns.Add("NAME");
                projectprops.Columns.Add("BINFILE");
                projectprops.Columns.Add("VERSION");
                projectprops.ReadXml(m_appSettings.ProjectFolder + "\\" + projectname + "\\projectproperties.xml");
                // valid project, add it to the list
                if (projectprops.Rows.Count > 0)
                {
                    OpenFile(projectprops.Rows[0]["BINFILE"].ToString(), false);
                }
            }
        }

        private string GetBinaryForProject(string projectname)
        {
            string retval = FileTools.Instance.Currentfile;
            if (File.Exists(m_appSettings.ProjectFolder + "\\" + projectname + "\\projectproperties.xml"))
            {
                System.Data.DataTable projectprops = new System.Data.DataTable("T5PROJECT");
                projectprops.Columns.Add("CARMAKE");
                projectprops.Columns.Add("CARMODEL");
                projectprops.Columns.Add("CARMY");
                projectprops.Columns.Add("CARVIN");
                projectprops.Columns.Add("NAME");
                projectprops.Columns.Add("BINFILE");
                projectprops.Columns.Add("VERSION");
                projectprops.ReadXml(m_appSettings.ProjectFolder + "\\" + projectname + "\\projectproperties.xml");
                // valid project, add it to the list
                if (projectprops.Rows.Count > 0)
                {
                    retval = projectprops.Rows[0]["BINFILE"].ToString();
                }
            }
            return retval;
        }

        private string GetBackupOlderThanDateTime(string project, DateTime mileDT)
        {
            string retval = FileTools.Instance.Currentfile; // default = current file
            string BackupPath = m_appSettings.ProjectFolder + "\\" + project + "\\Backups";
            DateTime MaxDateTime = DateTime.MinValue;
            string foundBackupfile = string.Empty;
            if (Directory.Exists(BackupPath))
            {
                string[] backupfiles = Directory.GetFiles(BackupPath, "*.bin");
                foreach (string backupfile in backupfiles)
                {
                    FileInfo fi = new FileInfo(backupfile);
                    if (fi.LastAccessTime > MaxDateTime && fi.LastAccessTime <= mileDT)
                    {
                        MaxDateTime = fi.LastAccessTime;
                        foundBackupfile = backupfile;
                    }
                }
            }
            if (foundBackupfile != string.Empty)
            {
                retval = foundBackupfile;
            }
            return retval;
        }

        private void btnOpenProject_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            //TODO let the user select a project from the Project folder. If none are present, let the user know
            if (!Directory.Exists(m_appSettings.ProjectFolder)) Directory.CreateDirectory(m_appSettings.ProjectFolder);
            System.Data.DataTable ValidProjects = new System.Data.DataTable();
            ValidProjects.Columns.Add("Projectname");
            ValidProjects.Columns.Add("NumberBackups");
            ValidProjects.Columns.Add("NumberTransactions");
            ValidProjects.Columns.Add("DateTimeModified");
            ValidProjects.Columns.Add("Version");
            string[] projects = Directory.GetDirectories(m_appSettings.ProjectFolder);
            // filter for folders with a projectproperties.xml file
            foreach (string project in projects)
            {
                string[] projectfiles = Directory.GetFiles(project, "projectproperties.xml");

                if (projectfiles.Length > 0)
                {
                    System.Data.DataTable projectprops = new System.Data.DataTable("T5PROJECT");
                    projectprops.Columns.Add("CARMAKE");
                    projectprops.Columns.Add("CARMODEL");
                    projectprops.Columns.Add("CARMY");
                    projectprops.Columns.Add("CARVIN");
                    projectprops.Columns.Add("NAME");
                    projectprops.Columns.Add("BINFILE");
                    projectprops.Columns.Add("VERSION");
                    projectprops.ReadXml((string)projectfiles.GetValue(0));
                    // valid project, add it to the list
                    if (projectprops.Rows.Count > 0)
                    {
                        string projectName = projectprops.Rows[0]["NAME"].ToString();
                        ValidProjects.Rows.Add(projectName, GetNumberOfBackups(projectName), GetNumberOfTransactions(projectName), GetLastAccessTime(projectprops.Rows[0]["BINFILE"].ToString()), projectprops.Rows[0]["VERSION"].ToString());
                    }
                }
            }
            if (ValidProjects.Rows.Count > 0)
            {
                frmProjectSelection projselection = new frmProjectSelection();
                projselection.SetDataSource(ValidProjects);
                if (projselection.ShowDialog() == DialogResult.OK)
                {
                    string selectedproject = projselection.GetProjectName();
                    if (selectedproject != "")
                    {
                        OpenProject(selectedproject);
                    }

                }
            }
            else
            {
                frmInfoBox info = new frmInfoBox("No projects were found, please create one first!");
            }
        }

        private int GetNumberOfBackups(string project)
        {
            int retval = 0;
            string dirname = m_appSettings.ProjectFolder + "\\" + project + "\\Backups";
            if (!Directory.Exists(dirname)) Directory.CreateDirectory(dirname);
            string[] backupfiles = Directory.GetFiles(dirname, "*.bin");
            retval = backupfiles.Length;
            return retval;
        }

        private int GetNumberOfTransactions(string project)
        {
            int retval = 0;
            string filename = m_appSettings.ProjectFolder + "\\" + project + "\\TransActionLogV2.ttl";
            if (File.Exists(filename))
            {
                TransactionLog translog = new TransactionLog();
                translog.OpenTransActionLog(m_appSettings.ProjectFolder, project);
                translog.ReadTransactionFile();
                retval = translog.TransCollection.Count;
            }
            return retval;
        }

        private DateTime GetLastAccessTime(string filename)
        {
            DateTime retval = DateTime.MinValue;
            if (File.Exists(filename))
            {
                FileInfo fi = new FileInfo(filename);
                retval = fi.LastAccessTime;
            }
            return retval;
        }

        private void btnCloseProject_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            CloseProject();
            m_appSettings.Lastprojectname = "";
        }

        private void CloseProject()
        {
            /*if (_ecuConnection.Opened) StopOnlineMode();// StopECUConnection();
            if (FileTools.Instance.CurrentWorkingProject != "")
            {
                if (m_AFRMaps != null)
                {
                    m_AFRMaps.SaveMaps();
                }
            }*/

            FileTools.Instance.CurrentWorkingProject = string.Empty;
            // unload the current file
            FileTools.Instance.Currentfile = string.Empty;
            gridControl1.DataSource = null;
            barFilenameText.Caption = "No file";
            //barButtonItem4.Enabled = false;
            m_appSettings.Lastfilename = string.Empty;
            btnCloseProject.Enabled = false;
            btnShowProjectLogbook.Enabled = false;
            btnProduceLatestBinary.Enabled = false;
            btnAddNoteToProject.Enabled = false;
            btnEditProject.Enabled = false;

            btnRebuildFile.Enabled = false;
            btnRollback.Enabled = false;
            btnRollforward.Enabled = false;
            btnShowTransactionLog.Enabled = false;
            this.Text = "MotronicSuite";
        }

        private void btnShowTransactionLog_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            // show new form
            if (FileTools.Instance.CurrentWorkingProject != string.Empty)
            {
                frmTransactionLog translog = new frmTransactionLog();
                translog.onRollBack += new frmTransactionLog.RollBack(translog_onRollBack);
                translog.onRollForward += new frmTransactionLog.RollForward(translog_onRollForward);
                translog.onNoteChanged += new frmTransactionLog.NoteChanged(translog_onNoteChanged);
                foreach (TransactionEntry entry in FileTools.Instance.ProjectTransactionLog.TransCollection)
                {
                    entry.SymbolName = Helpers.Instance.GetSymbolNameByAddress(_workingFile.Symbols, entry.SymbolAddress);

                }
                translog.SetTransactionLog(FileTools.Instance.ProjectTransactionLog);
                translog.Show();
            }
        }


        void translog_onNoteChanged(object sender, frmTransactionLog.RollInformationEventArgs e)
        {
            FileTools.Instance.ProjectTransactionLog.SetEntryNote(e.Entry);
        }

        void translog_onRollForward(object sender, frmTransactionLog.RollInformationEventArgs e)
        {
            // alter the log!
            // rollback the transaction
            // now reload the list
            RollForward(e.Entry);
            if (sender is frmTransactionLog)
            {
                frmTransactionLog logfrm = (frmTransactionLog)sender;
                logfrm.SetTransactionLog(FileTools.Instance.ProjectTransactionLog);
            }
        }

        private void RollForward(TransactionEntry entry)
        {
            int addressToWrite = entry.SymbolAddress;
            
            while (addressToWrite > FileTools.Instance.Currentfile_size) addressToWrite -= FileTools.Instance.Currentfile_size;
            FileTools.Instance.savedatatobinary(addressToWrite, entry.SymbolLength, entry.DataAfter, FileTools.Instance.Currentfile, false);
            UpdateCRC(FileTools.Instance.Currentfile);
            FileTools.Instance.ProjectTransactionLog.SetEntryRolledForward(entry.TransactionNumber);
            if (FileTools.Instance.CurrentWorkingProject != string.Empty)
            {

                FileTools.Instance.ProjectLog.WriteLogbookEntry(LogbookEntryType.TransactionRolledforward, Helpers.Instance.GetSymbolNameByAddress(_workingFile.Symbols, entry.SymbolAddress) + " " + entry.Note + " " + entry.TransactionNumber.ToString());
            }

            UpdateRollbackForwardControls();
        }

        void translog_onRollBack(object sender, frmTransactionLog.RollInformationEventArgs e)
        {
            // alter the log!
            // rollback the transaction
            RollBack(e.Entry);
            // now reload the list
            if (sender is frmTransactionLog)
            {
                frmTransactionLog logfrm = (frmTransactionLog)sender;
                logfrm.SetTransactionLog(FileTools.Instance.ProjectTransactionLog);
            }
        }

        private void SignalTransactionLogChanged(int SymbolAddress, string Note)
        {
            UpdateRollbackForwardControls();
            
        }

        private void RollBack(TransactionEntry entry)
        {
            int addressToWrite = entry.SymbolAddress;
            while (addressToWrite > FileTools.Instance.Currentfile_size) addressToWrite -= FileTools.Instance.Currentfile_size;
            FileTools.Instance.savedatatobinary(addressToWrite, entry.SymbolLength, entry.DataBefore, FileTools.Instance.Currentfile, false);
            UpdateCRC(FileTools.Instance.Currentfile);
            FileTools.Instance.ProjectTransactionLog.SetEntryRolledBack(entry.TransactionNumber);
            if (FileTools.Instance.CurrentWorkingProject != string.Empty)
            {
                FileTools.Instance.ProjectLog.WriteLogbookEntry(LogbookEntryType.TransactionRolledback, Helpers.Instance.GetSymbolNameByAddress(_workingFile.Symbols, entry.SymbolAddress) + " " + entry.Note + " " + entry.TransactionNumber.ToString());
            }

            UpdateRollbackForwardControls();
        }

        private void btnRollback_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            //roll back last entry in the log that has not been rolled back
            for (int t = FileTools.Instance.ProjectTransactionLog.TransCollection.Count - 1; t >= 0; t--)
            {
                if (!FileTools.Instance.ProjectTransactionLog.TransCollection[t].IsRolledBack)
                {
                    RollBack(FileTools.Instance.ProjectTransactionLog.TransCollection[t]);

                    break;
                }
            }
        }

        private void btnRollforward_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            //roll back last entry in the log that has not been rolled back
            for (int t = 0; t < FileTools.Instance.ProjectTransactionLog.TransCollection.Count; t++)
            {
                if (FileTools.Instance.ProjectTransactionLog.TransCollection[t].IsRolledBack)
                {
                    RollForward(FileTools.Instance.ProjectTransactionLog.TransCollection[t]);

                    break;
                }
            }
        }

        private void btnRebuildFile_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            // show the transactionlog again and ask the user upto what datetime he wants to rebuild the file
            // first ask a datetime
            frmRebuildFileParameters filepar = new frmRebuildFileParameters();
            if (filepar.ShowDialog() == DialogResult.OK)
            {

                // get the last backup that is older than the selected datetime
                string file2Process = GetBackupOlderThanDateTime(FileTools.Instance.CurrentWorkingProject, filepar.SelectedDateTime);
                // now rebuild the file
                // first create a copy of this file
                string tempRebuildFile = m_appSettings.ProjectFolder + "\\" + FileTools.Instance.CurrentWorkingProject + "rebuild.bin";
                if (File.Exists(tempRebuildFile))
                {
                    File.Delete(tempRebuildFile);
                }
                // CREATE A BACKUP FILE HERE
                CreateProjectBackupFile();
                File.Copy(file2Process, tempRebuildFile);
                // now do all the transactions newer than this file and older than the selected date time
                FileInfo fi = new FileInfo(file2Process);
                foreach (TransactionEntry te in FileTools.Instance.ProjectTransactionLog.TransCollection)
                {
                    if (te.EntryDateTime >= fi.LastAccessTime && te.EntryDateTime <= filepar.SelectedDateTime)
                    {
                        // apply this change
                        RollForwardOnFile(tempRebuildFile, te);
                    }
                }
                // rename/copy file
                if (filepar.UseAsNewProjectFile)
                {
                    // just delete the current file
                    File.Delete(FileTools.Instance.Currentfile);
                    File.Copy(tempRebuildFile, FileTools.Instance.Currentfile);
                    File.Delete(tempRebuildFile);
                    // done
                }
                else
                {
                    // ask for destination file
                    SaveFileDialog sfd = new SaveFileDialog();
                    sfd.Title = "Save rebuild file as...";
                    sfd.Filter = "Binary files|*.bin";
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        if (File.Exists(sfd.FileName)) File.Delete(sfd.FileName);
                        File.Copy(tempRebuildFile, sfd.FileName);
                        File.Delete(tempRebuildFile);
                    }
                }
                if (FileTools.Instance.CurrentWorkingProject != string.Empty)
                {
                    FileTools.Instance.ProjectLog.WriteLogbookEntry(LogbookEntryType.ProjectFileRecreated, "Reconstruct upto " + filepar.SelectedDateTime.ToString("dd/MM/yyyy") + " selected file " + file2Process);
                }
                UpdateRollbackForwardControls();
            }
        }

        private void RollForwardOnFile(string file2Rollback, TransactionEntry entry)
        {
            FileInfo fi = new FileInfo(file2Rollback);
            int addressToWrite = entry.SymbolAddress;
            while (addressToWrite > fi.Length) addressToWrite -= (int)fi.Length;
            FileTools.Instance.savedatatobinary(addressToWrite, entry.SymbolLength, entry.DataAfter, file2Rollback, false);
            UpdateCRC(FileTools.Instance.Currentfile);
        }

        private void btnEditProject_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (FileTools.Instance.CurrentWorkingProject != string.Empty)
            {
                EditProjectProperties(FileTools.Instance.CurrentWorkingProject);
            }
        }

        private void EditProjectProperties(string project)
        {
            // edit current project properties
            System.Data.DataTable projectprops = new System.Data.DataTable("T5PROJECT");
            projectprops.Columns.Add("CARMAKE");
            projectprops.Columns.Add("CARMODEL");
            projectprops.Columns.Add("CARMY");
            projectprops.Columns.Add("CARVIN");
            projectprops.Columns.Add("NAME");
            projectprops.Columns.Add("BINFILE");
            projectprops.Columns.Add("VERSION");
            projectprops.ReadXml(m_appSettings.ProjectFolder + "\\" + project + "\\projectproperties.xml");

            frmProjectProperties projectproperties = new frmProjectProperties();
            projectproperties.Version = projectprops.Rows[0]["VERSION"].ToString();
            projectproperties.ProjectName = projectprops.Rows[0]["NAME"].ToString();
            projectproperties.CarMake = projectprops.Rows[0]["CARMAKE"].ToString();
            projectproperties.CarModel = projectprops.Rows[0]["CARMODEL"].ToString();
            projectproperties.CarVIN = projectprops.Rows[0]["CARVIN"].ToString();
            projectproperties.CarMY = projectprops.Rows[0]["CARMY"].ToString();
            projectproperties.BinaryFile = projectprops.Rows[0]["BINFILE"].ToString();
            bool _reopenProject = false;
            if (projectproperties.ShowDialog() == DialogResult.OK)
            {
                // delete the original XML file
                if (project != projectproperties.ProjectName)
                {
                    Directory.Move(m_appSettings.ProjectFolder + "\\" + project, m_appSettings.ProjectFolder + "\\" + projectproperties.ProjectName);
                    project = projectproperties.ProjectName;
                    FileTools.Instance.CurrentWorkingProject = project;
                    // set the working file to the correct folder
                    projectproperties.BinaryFile = Path.Combine(m_appSettings.ProjectFolder + "\\" + project, Path.GetFileName(projectprops.Rows[0]["BINFILE"].ToString()));
                    _reopenProject = true;
                    // open this project

                }

                File.Delete(m_appSettings.ProjectFolder + "\\" + project + "\\projectproperties.xml");
                System.Data.DataTable dtProps = new System.Data.DataTable("T5PROJECT");
                dtProps.Columns.Add("CARMAKE");
                dtProps.Columns.Add("CARMODEL");
                dtProps.Columns.Add("CARMY");
                dtProps.Columns.Add("CARVIN");
                dtProps.Columns.Add("NAME");
                dtProps.Columns.Add("BINFILE");
                dtProps.Columns.Add("VERSION");
                dtProps.Rows.Add(projectproperties.CarMake, projectproperties.CarModel, projectproperties.CarMY, projectproperties.CarVIN, MakeDirName(projectproperties.ProjectName), projectproperties.BinaryFile, projectproperties.Version);
                dtProps.WriteXml(m_appSettings.ProjectFolder + "\\" + MakeDirName(projectproperties.ProjectName) + "\\projectproperties.xml");
                if (_reopenProject)
                {
                    OpenProject(FileTools.Instance.CurrentWorkingProject);
                }
                FileTools.Instance.ProjectLog.WriteLogbookEntry(LogbookEntryType.PropertiesEdited, projectproperties.Version);

            }

        }

        private void btnAddNoteToProject_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            frmChangeNote newNote = new frmChangeNote();
            newNote.ShowDialog();
            if (newNote.Note != string.Empty)
            {
                if (FileTools.Instance.CurrentWorkingProject != string.Empty)
                {
                    FileTools.Instance.ProjectLog.WriteLogbookEntry(LogbookEntryType.Note, newNote.Note);
                }
            }
        }

        private void btnShowProjectLogbook_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (FileTools.Instance.CurrentWorkingProject != string.Empty)
            {
                frmProjectLogbook logb = new frmProjectLogbook();

                logb.LoadLogbookForProject(m_appSettings.ProjectFolder, FileTools.Instance.CurrentWorkingProject);
                logb.Show();
            }
        }

        private void btnProduceLatestBinary_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            // save binary as
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Binary files|*.bin";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                // copy the current project file to the selected destination
                File.Copy(FileTools.Instance.Currentfile, sfd.FileName, true);
            }
        }

        #endregion

        private void btnFlashECU_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (_workingFile is ME7File)
            {
                 OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "Binary files|*.bin";
                ofd.Multiselect = false;
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    Application.DoEvents();
                    if (_ecucomms == null)
                    {
                        _ecucomms = new ME7Communication();
                        _ecucomms.setCANDevice("LAWICEL"); //TODO: For now, make this a setting!
                        _ecucomms.onStatusChanged += new ICommunication.StatusChanged(_ecucomms_onStatusChanged);
                        _ecucomms.onDTCInfo += new ICommunication.DTCInfo(_ecucomms_onDTCInfo);
                        _ecucomms.onECUInfo += new ICommunication.ECUInfo(_ecucomms_onECUInfo);
                        _ecucomms.LogFolder = Application.StartupPath;
                        _ecucomms.EnableLogging = false;
                    }
                    if (!_ecucomms.CommunicationRunning)
                    {
                        _ecucomms.StartCommunication("", false);  //TODO: Make highspeed support for ME7 MY2005+
                    }
                    Console.WriteLine("Start upgrade procedure");
                    _ecucomms.WriteEprom(ofd.FileName, 1000);
                    Console.WriteLine("Upgrade procedure done");

                }
            }
            else
            {
                frmInfoBox info = new frmInfoBox("Make sure you have started the ECU in bootmode! See the manual for more information");
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
                        flasher.FlashFile(ofd.FileName, m_appSettings.Comport);
                    }
                    else if (fi.Length == 0x10000) // M4.3 file
                    {
                        flasher = new M43Flasher();
                        flasher.onStatusChanged += new IFlasher.StatusChanged(flasher_onStatusChanged);
                        flasher.FlashFile(ofd.FileName, m_appSettings.Comport);
                    }
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

        private void UpdateECUInfo(string ecuinfo)
        {
            SetECUInfo(ecuinfo);
        }

        private void UpdateProgress(string information, int percentage)
        {
            Console.WriteLine(information + " " + percentage.ToString() + " %");
            SetProgressPercentage(information, percentage);
            if (!information.StartsWith("Flashing..."))
            {
                Console.WriteLine("Progress: " + information + " " + percentage + "%");
            }
            if (information == "Flashing completed" || information == "Erase failed" || information == "Flashing failed")
            {
                frmInfoBox info = new frmInfoBox(information);
            }
        }

        private bool _stopRealtimeComms = false;

        private void btnConnectECU_ItemClick(object sender, ItemClickEventArgs e)
        {
             // connect to m_appSettings.Comport @ 12700 baud
            if (_ecucomms == null)
            {
                if (m_appSettings.DetermineCommunicationByFileType)
                {
                    if (_workingFile is M43File)
                    {
                        _ecucomms = new M43Communication();
                    }
                    else if (_workingFile is M44File)
                    {
                        _ecucomms = new M44Communication();

                    }
                    else if (_workingFile is ME7File)
                    {
                        _ecucomms = new ME7Communication();
                        _ecucomms.setCANDevice("LAWICEL"); //TODO: For now, make this a setting!
                    }
                }
                else
                {
                    frmCommTypeChoice commchoice = new frmCommTypeChoice();
                    if (commchoice.ShowDialog() == DialogResult.OK)
                    {
                        if (commchoice.CommType == 1)
                        {
                            _ecucomms = new M43Communication();
                        }
                        else if (commchoice.CommType == 2)
                        {
                            _ecucomms = new M44Communication();
                        }
                        else if (commchoice.CommType == 3)
                        {
                            _ecucomms = new ME7Communication();
                            _ecucomms.setCANDevice("LAWICEL"); //TODO: For now, make this a setting!
                        }
                        else if (commchoice.CommType == 4)
                        {
                            _ecucomms = new M2103Communication();
                        }
                    }
                }
                if (_ecucomms != null)
                {
                    try
                    {
                        _ecucomms.onStatusChanged += new ICommunication.StatusChanged(_ecucomms_onStatusChanged);
                        if (_ecucomms is M2103Communication)
                        {
                            _ecucomms.onDTCInfo += new ICommunication.DTCInfo(_ecucomms_onDTCInfoM2103);
                        }
                        else
                        {
                            _ecucomms.onDTCInfo += new ICommunication.DTCInfo(_ecucomms_onDTCInfo);
                        }
                        _ecucomms.onECUInfo += new ICommunication.ECUInfo(_ecucomms_onECUInfo);
                        _ecucomms.LogFolder = Application.StartupPath;
                        _ecucomms.EnableLogging = false;
                    }
                    catch (Exception E)
                    {

                    }
                }
            }
            if (_ecucomms != null)
            {
                if (_ecucomms.CommunicationRunning)
                {
                    _ecucomms.StopCommunication();
                    _ecucomms.EnableLogging = false;
                    btnConnectECU.Caption = "Connect ECU";
                    /* Test */

                    if (btnReadDTC.Enabled) btnReadDTC.Enabled = false;
                    if (btnReadEprom.Enabled) btnReadEprom.Enabled = false;
                    if (btnSRAMSnapshot.Enabled) btnSRAMSnapshot.Enabled = false;
                    if (btnToggleRealtimePanel.Enabled) btnToggleRealtimePanel.Enabled = false;
                    _stopRealtimeComms = true;
                    tmrRealtime.Enabled = false;
                    dockRealtime.Visibility = DockVisibility.Hidden;
                    Application.DoEvents();

                    /* Test end */

                    SetProgressPercentage("Idle", 0);
                    if (!this.IsDisposed)
                    {
                        try
                        {
                            this.Invoke(m_DelegateUpdateECUInfo, "");
                        }
                        catch (Exception E)
                        {
                            Console.WriteLine(E.Message);
                        }
                    }
                }
                else
                {
                    _ecucomms.EnableLogging = m_appSettings.EnableCommLogging;
                    _ecucomms.StartCommunication(m_appSettings.Comport, false);
                    SetProgressPercentage("Initializing communication...", 0);
                    btnConnectECU.Caption = "Disconnect ECU";
                }
            }
        }

        void _ecucomms_onStatusChanged(object sender, ICommunication.StatusEventArgs e)
        {
            //Console.WriteLine(e.State.ToString() + " " + e.Info + " " + e.Percentage.ToString());
            if (e.State == MotronicCommunication.ICommunication.ECUState.CommunicationRunning)
            {
                //if (!btnReadDTC.Enabled) btnReadDTC.Enabled = true;
                //if (!btnReadEprom.Enabled) btnReadEprom.Enabled = true;
                _connectedECUIdentification = string.Empty;
                if (e.Info == "") Application.DoEvents();
                else
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
                    //Console.WriteLine(e.Info + " % done: " + e.Percentage.ToString());
                }
            }
            else if (e.State == MotronicCommunication.ICommunication.ECUState.NotInitialized)
            {

                if (e.Info == "Unable to connect to ECU")
                {
                    frmInfoBox info = new frmInfoBox("Unable to connect to ECU, please check connections and ECU type");
                    _ecucomms.EnableLogging = false;
                    btnConnectECU.Caption = "Connect ECU";
                }

                if (btnReadDTC.Enabled) btnReadDTC.Enabled = false;
                if (btnReadEprom.Enabled) btnReadEprom.Enabled = false;
                if (btnSRAMSnapshot.Enabled) btnSRAMSnapshot.Enabled = false;
                if (btnToggleRealtimePanel.Enabled) btnToggleRealtimePanel.Enabled = false;
                tmrRealtime.Enabled = false;
                _stopRealtimeComms = true;
                dockRealtime.Visibility = DockVisibility.Hidden;
                Application.DoEvents();
                //TODO: HIDE the realtime panel
                // and stop the timer
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
                //Console.WriteLine(e.Info);
                if (!this.IsDisposed)
                {
                    try
                    {
                        this.Invoke(m_DelegateUpdateECUInfo, "");
                    }
                    catch (Exception E)
                    {
                        Console.WriteLine(E.Message);
                    }
                }
            }
            else if (e.State == ICommunication.ECUState.Initialized)
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
                
            }
            //Console.WriteLine(e.Info);
            Application.DoEvents();
        }

        private string _connectedECUIdentification = string.Empty;

        void _ecucomms_onDTCInfoM2103(object sender, ICommunication.DTCEventArgs e)
        {
            // inform user of DTC codes
            if (e.Strcode == "EMPTY")
            {
                frmInfoBox info = new frmInfoBox("No DTC codes active");
            }
            else
            {
                // translate DTC code
                DTCCodeTranslator translator = new DTCCodeTranslator();
                string dtcdescription = translator.TranslateDTCCode(e.Strcode);
                frmFaultcodes faults = new frmFaultcodes();
                faults.onClearCurrentDTC += new frmFaultcodes.onClearDTC(faults_onClearCurrentDTC);
                faults.addFault(e.Strcode, dtcdescription);
                faults.Show();
            }
        }

        void _ecucomms_onDTCInfo(object sender, ICommunication.DTCEventArgs e)
        {
            // inform user of DTC codes
            if (e.Dtccode == 0)
            {
                frmInfoBox info = new frmInfoBox("No DTC codes active");
            }
            else
            {
                // translate DTC code
                DTCCodeTranslator translator = new DTCCodeTranslator();
                string dtcdescription = translator.TranslateDTCCode(e.Dtccode);
                frmFaultcodes faults = new frmFaultcodes();
                faults.onClearCurrentDTC += new frmFaultcodes.onClearDTC(faults_onClearCurrentDTC);
                faults.addFault(e.Dtccode, dtcdescription);
                faults.Show();
            }
        }

        void _ecucomms_onECUInfo(object sender, ICommunication.ECUInfoEventArgs e)
        {
            // display information in the statusbar
            Console.WriteLine("ID" + e.IDNumber.ToString() + " = " + e.Info);
            if (e.IDNumber < 3)
            {
                _connectedECUIdentification += e.Info + "_";
            }
            if (e.IDNumber == 3)
            {
                _connectedECUIdentification += e.Info;
                if (!btnReadDTC.Enabled) btnReadDTC.Enabled = true;
                if (!btnReadEprom.Enabled && _ecucomms is M43Communication) btnReadEprom.Enabled = true;
                if (!btnSRAMSnapshot.Enabled) btnSRAMSnapshot.Enabled = true;
                if (!btnToggleRealtimePanel.Enabled) btnToggleRealtimePanel.Enabled = true;

                if (!this.IsDisposed)
                {
                    try
                    {
                        this.Invoke(m_DelegateUpdateECUInfo, _connectedECUIdentification);
                    }
                    catch (Exception E)
                    {
                        Console.WriteLine(E.Message);
                    }
                }
            }

        }


        void faults_onClearCurrentDTC(object sender, frmFaultcodes.ClearDTCEventArgs e)
        {
            _ecucomms.ClearDTC(1000);
        }


        private void btnReadDTC_ItemClick(object sender, ItemClickEventArgs e)
        {
            _ecucomms.ReadDTCCodes(1000);
        }

        private void btnReadEprom_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (MessageBox.Show("Reading the flash content will take a long time (30-60 minutes), do you want to proceed", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "Binary files|*.bin";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    _ecucomms.ReadEprom(sfd.FileName, 1000);
                }
            }
        }

        private void btnSRAMSnapshot_ItemClick(object sender, ItemClickEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "SRAM files|*.ram";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                byte[] snapshot = new byte[0x100];
                for (int i = 0; i < 0x20; i++)
                {
                    bool success = false;
                    byte[] part = _ecucomms.readSRAM(8 * i, 8, 1000, out success);
                    for (int j = 0; j < 8; j++)
                    {
                        snapshot[8 * i + j] = part[j];
                    }
                }
                // write snapshot
                FileStream fs = new FileStream(sfd.FileName, FileMode.OpenOrCreate);
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    bw.Write(snapshot);
                }
                fs.Close();
                fs.Dispose();
                frmInfoBox info = new frmInfoBox("Snapshot created: " + Path.GetFileName(sfd.FileName));
            }

        }

        private void btnToggleRealtimePanel_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (_ecucomms is M2103Communication)
            {
                if (dockRealtimeM2103.Visibility == DevExpress.XtraBars.Docking.DockVisibility.Visible)
                {
                    dockRealtimeM2103.Visibility = DevExpress.XtraBars.Docking.DockVisibility.Hidden;
                    tmrRealtime.Enabled = false;
                    _stopRealtimeComms = true;
                }
                else
                {
                    _realtimeSymbolsM2103 =_ecucomms.ReadSupportedSensors();

                    m_DelegateUpdateRealTimeValue = new DelegateUpdateRealTimeValue(this.UpdateRealtimeInformationValueM2103);

                    dockRealtimeM2103.Visibility = DevExpress.XtraBars.Docking.DockVisibility.Visible;
                    int width = dockManager1.Form.ClientSize.Width - dockPanel1.Width;
                    int height = dockManager1.Form.ClientSize.Height;
                    if (width > 462) width = 462;
                    dockRealtimeM2103.Dock = DockingStyle.Left;
                    dockRealtimeM2103.Width = width;
                    tmrRealtime.Enabled = true;
                    _stopRealtimeComms = false;
                }
            }
            else
            {
                if (dockRealtime.Visibility == DevExpress.XtraBars.Docking.DockVisibility.Visible)
                {
                    dockRealtime.Visibility = DevExpress.XtraBars.Docking.DockVisibility.Hidden;
                    tmrRealtime.Enabled = false;
                    //if (_ecucomms.CommunicationRunning) _ecucomms.StopCommunication();
                    _stopRealtimeComms = true;
                }
                else
                {
                    if (gridControl3.DataSource == null)
                    {
                        FillRealtimeTable();
                    }

                    dockRealtime.Visibility = DevExpress.XtraBars.Docking.DockVisibility.Visible;
                    int width = dockManager1.Form.ClientSize.Width - dockPanel1.Width;
                    int height = dockManager1.Form.ClientSize.Height;
                    if (width > 462) width = 462;
                    dockRealtime.Dock = DockingStyle.Left;
                    dockRealtime.Width = width;
                    tmrRealtime.Enabled = true;
                    _stopRealtimeComms = false;
                }
            }
        }

        private enum ReadIDS : int
        {
            ReadBatteryVoltage,
            ReadInternalLoad,
            ReadEngineSpeed,
            ReadIgnitionAdvance
        }

        private void FillRealtimeTable()
        {
            // only if there are no symbols in the list yet
            if (gridControl3.DataSource == null)
            {
                SymbolCollection rt_symbolCollection = new SymbolCollection();
                SymbolHelper shrpm = new SymbolHelper();
                shrpm.Varname = "Engine speed";
                shrpm.Description = "Engine speed";
                shrpm.Start_address = 0x3B;
                shrpm.Length = 1;
                shrpm.CorrectionFactor = 40;
                shrpm.MaxValue = 8000;
                if (_ecucomms is M44Communication)
                {
                    shrpm.CorrectionFactor = 30;
                }
                shrpm.CorrectionOffset = 0;
                shrpm.Units = "Rpm";
                shrpm.MinValue = 0;
                shrpm.Color = Color.Green;
                rt_symbolCollection.Add(shrpm);

                SymbolHelper shbatt = new SymbolHelper();
                shbatt.Varname = "Battery voltage";
                shbatt.Description = "Battery voltage";
                shbatt.Start_address = 0x36;
                shbatt.Length = 1;
                shbatt.Units = "Volt";
                shbatt.MaxValue = 16;
                shbatt.MinValue = 0;
                shbatt.CorrectionFactor = 0.0704F;
                shbatt.CorrectionOffset = 0;
                shbatt.Color = Color.Ivory;
                rt_symbolCollection.Add(shbatt);

                SymbolHelper shload = new SymbolHelper();
                shload.Varname = "Internal load";
                shload.Description = "Internal load";
                shload.Start_address = 0x40;
                shload.Length = 1;
                shload.Units = "ms";
                shload.MinValue = 0;
                shload.MaxValue = 20;
                shload.CorrectionFactor = 0.05F;
                shload.CorrectionOffset = 0;
                shload.Color = Color.Red;
                rt_symbolCollection.Add(shload);

                SymbolHelper shignadv = new SymbolHelper();
                shignadv.Varname = "Ignition advance";
                shignadv.Description = "Ignition advance";
                shignadv.Start_address = 0x55;
                shignadv.Length = 1;
                shignadv.MinValue = -10;
                shignadv.MaxValue = 40;
                shignadv.Units = "Degrees";
                shignadv.CorrectionFactor = -0.75F;
                //shignadv.CorrectionFactor = 0.75F;
                //shignadv.CorrectionOffset = -22.5F;
                shignadv.CorrectionOffset = 78;
                shignadv.Color = Color.LightBlue;
                if (_ecucomms is M44Communication)
                {
                    //shignadv.CorrectionFactor = -0.75F;
                    shignadv.Start_address = 0x54;
                    //shignadv.CorrectionOffset = 78;
                }
                rt_symbolCollection.Add(shignadv);


                SymbolHelper shcoolant = new SymbolHelper();
                shcoolant.Varname = "Engine temperature";
                shcoolant.Description = "Engine temperature";
                shcoolant.Start_address = 0x38;
                shcoolant.Length = 1;
                shcoolant.MinValue = -40;
                shcoolant.MaxValue = 120;
                shcoolant.Units = "Degrees";
                shcoolant.CorrectionFactor = 1F;
                shcoolant.CorrectionOffset = -80F;
                shcoolant.Color = Color.Orange;
                rt_symbolCollection.Add(shcoolant);

                SymbolHelper shtps = new SymbolHelper();
                shtps.Varname = "Throttle position";
                shtps.Description = "Throttle position";
                shtps.Start_address = 0x22;
                shtps.Length = 1;
                shtps.MinValue = 0;
                shtps.MaxValue = 100;
                shtps.Units = "TPS %";
                shtps.CorrectionFactor = 0.41667F;
                shtps.CorrectionOffset = -5.34F;
                shtps.Color = Color.DimGray;
                rt_symbolCollection.Add(shtps);


                //0x38: coolant temp (-80)
                //0x30: tps (0.41667F)
                gridControl3.DataSource = rt_symbolCollection;
            }
        }


        //private ReadIDS readIDState = ReadIDS.ReadBatteryVoltage;


        private Stopwatch sw = new Stopwatch();

        private void tmrRealtimeGeneric()
        {
            if (_ecucomms.CommunicationRunning && !_stopRealtimeComms)
            {
                // get the list from the realtime panel
                SymbolCollection sc = (SymbolCollection)gridControl3.DataSource;
                // sort it by start address
                sc.SortColumn = "Start_address";
                sc.SortingOrder = GenericComparer.SortOrder.Ascending;
                sc.Sort();
                // now, determine how many blocks we need to read
                SymbolBlockCollection sbc = determineSymbolBlocks(sc);
                foreach (SymbolBlock sb in sbc)
                {
                    //Console.WriteLine("Block: " + sb.Start_Address.ToString("X4") + "-" + sb.End_Address.ToString("X4") + " " + sb.Length.ToString("X2"));
                    bool success = false;
                    // Console.WriteLine("enter read");
                    byte[] rxbytes = _ecucomms.readSRAM(sb.Start_Address, sb.Length, 1000, out success);
                    // now update the relevant data
                    if (success)
                    {
                        // Console.WriteLine("Read OK");
                        foreach (SymbolHelper sh in sc)
                        {
                            if (sh.Start_address >= sb.Start_Address && sh.Start_address <= sb.End_Address)
                            {
                                double value = Convert.ToDouble(rxbytes[sh.Start_address - sb.Start_Address]) * sh.CorrectionFactor;
                                value += sh.CorrectionOffset;
                                UpdateRealtimeInformation(sh.Varname, (float)value);
                                onlineGraphControl1.AddMeasurement(sh.Units, sh.Varname, DateTime.Now, (float)value, sh.MinValue, sh.MaxValue, sh.Color);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Read failed");
                    }
                }
                if (sbc.Count > 0)
                {
                    onlineGraphControl1.ForceRepaint(0);
                    sw.Stop();
                    float secs = sw.ElapsedMilliseconds / 1000F;
                    secs = 1 / secs;
                    if (float.IsInfinity(secs)) secs = 1;
                    UpdateRealtimeInformation("FPSCounter", secs);
                    sw.Reset();
                    sw.Start();
                }
                else
                {
                    UpdateRealtimeInformation("FPSCounter", 0);
                }
            }
        }

        private void tmrRealtimeM2103()
        {
            if (_ecucomms.CommunicationRunning && !_stopRealtimeComms)
            {
                foreach (SymbolHelper sh in _realtimeSymbolsM2103)
                {
                    Console.WriteLine("Reading " + sh.Varname);
                    bool success = false;
                    List<byte> rcv_list = _ecucomms.ReadSensor(sh.Start_address, out success);

                    // now update the relevant data
                    if (success)
                    {
                        int rcv_value = 0;

                        for (int i = 0; i < sh.Length; ++i)
                        {
                            Console.Write(rcv_list[i].ToString("X2") + " ");
                            rcv_value |= rcv_list[i] << (((sh.Length - 1) - i) * 8);
                        }
                        Console.WriteLine("total:  " + rcv_value.ToString("X4"));

                        float value = (float)rcv_value * sh.CorrectionFactor;
                        value += sh.CorrectionOffset;
                        Console.WriteLine("Read " + sh.Varname + " value: " + value);

                        UpdateRealtimeInformation(sh.Varname, (float)value);
                        //onlineGraphControl1.AddMeasurement(sh.Units, sh.Varname, DateTime.Now, (float)value, sh.MinValue, sh.MaxValue, sh.Color);
                    }
                    else
                    {
                        Console.WriteLine("Read failed");
                    }
                }
            }
        }

        private void tmrRealtime_Tick(object sender, EventArgs e)
        {
            tmrRealtime.Enabled = false;
            try
            {
                if (_ecucomms != null && !_ecucomms.IsWaitingForResponse)
                {
                    if (_ecucomms is M2103Communication)
                    {
                        tmrRealtimeM2103();
                    }
                    else
                    {
                        tmrRealtimeGeneric();
                    }
                }
                else
                {
                    if (_ecucomms.IsWaitingForResponse) Console.WriteLine("Waiting for response");
                }
            }
            catch (Exception E)
            {
                Console.WriteLine(E.Message);
            }
            if (_ecucomms.CommunicationRunning && !_stopRealtimeComms) tmrRealtime.Enabled = true;
            _stopRealtimeComms = false;
        }

        private SymbolBlockCollection determineSymbolBlocks(SymbolCollection sc)
        {
            SymbolBlockCollection sbc = new SymbolBlockCollection();
            foreach (SymbolHelper sh in sc)
            {
                bool _ok = false;
                foreach (SymbolBlock sb in sbc)
                {
                    if (sb.Start_Address <= sh.Start_address && sb.End_Address >= sh.Start_address)
                    {
                        _ok = true;
                        break;
                    }
                    else if (sb.Start_Address <= sh.Start_address && sb.Length < 0x0B)
                    {
                        // try to strech the block to a maximum of 0x0B characters
                        if (sb.Start_Address + 0x0a >= sh.Start_address)
                        {
                            // it will fit
                            sb.End_Address = sh.Start_address;
                            sb.Length = sb.End_Address - sb.Start_Address + 1;
                            _ok = true;
                            break;
                        }
                    }

                }
                if (!_ok)
                {
                    SymbolBlock sb = new SymbolBlock();
                    sb.Start_Address = sh.Start_address;
                    sb.End_Address = sh.Start_address + sh.Length - 1;
                    sb.Length = sh.Length;
                    sbc.Add(sb);
                }
            }

            return sbc;
        }

        private void UpdateRealtimeInformation(string symbolname, float value)
        {
            if (!this.IsDisposed)
            {
                try
                {
                    this.Invoke(m_DelegateUpdateRealTimeValue, symbolname, value);
                }
                catch (Exception E)
                {
                    Console.WriteLine(E.Message);
                }
            }

        }

        private void btnCompareM44Halves_ItemClick(object sender, ItemClickEventArgs e)
        {
            // open file 1
            // open file 2
            
                OpenFileDialog openFileDialog1 = new OpenFileDialog();
                openFileDialog1.Filter = "Binary files|*.bin";
                openFileDialog1.Multiselect = false;
                openFileDialog1.FileName = "";
                OpenFileDialog openFileDialog2 = new OpenFileDialog();
                openFileDialog2.Filter = "Binary files|*.bin";
                openFileDialog2.Multiselect = false;
                openFileDialog2.FileName = "";
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    FileInfo fi = new FileInfo(openFileDialog1.FileName);
                    if (fi.Length == 0x20000)
                    {
                        // split into halves
                        byte[] allBytes = File.ReadAllBytes(openFileDialog1.FileName);
                        string fileName1 = Path.Combine(Path.GetDirectoryName(openFileDialog1.FileName), Path.GetFileNameWithoutExtension(openFileDialog1.FileName) + "-bank0.bin");
                        string fileName2 = Path.Combine(Path.GetDirectoryName(openFileDialog1.FileName), Path.GetFileNameWithoutExtension(openFileDialog1.FileName) + "-bank1.bin");
                        CreateFile(fileName1, allBytes, 0, 0x10000);
                        CreateFile(fileName2, allBytes, 0x10000, 0x10000);
                        frmBinCompare bincomp = new frmBinCompare();
                        bincomp.SetCurrentFilename(fileName1);
                        bincomp.SetCompareFilename(fileName2);
                        bincomp.CompareFiles();
                        bincomp.ShowDialog();

                    }
                    else
                    {
                        if (openFileDialog2.ShowDialog() == DialogResult.OK)
                        {
                            frmBinCompare bincomp = new frmBinCompare();
                            bincomp.SetCurrentFilename(openFileDialog1.FileName);
                            bincomp.SetCompareFilename(openFileDialog2.FileName);
                            bincomp.CompareFiles();
                            bincomp.ShowDialog();
                        }
                    }
                }
            
        }

        private void CreateFile(string fileName, byte[] allBytes, int offset, int length)
        {
            FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate);
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                bw.Write(allBytes, offset, length);
            }
        }

        private void btnOpenLogFile_ItemClick(object sender, ItemClickEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "MotronicSuite logfiles|*.msl";
            ofd.Multiselect = false;
            if(ofd.ShowDialog() == DialogResult.OK)
            {
                OpenAndDisplayLogFile(ofd.FileName);
            }
        }

        private void OpenAndDisplayLogFile(string filename)
        {
            // create a new dock with a graph view in it
            DevExpress.XtraBars.Docking.DockPanel dp = dockManager1.AddPanel(DevExpress.XtraBars.Docking.DockingStyle.Left);
            dp.Size = new Size(dockManager1.Form.ClientSize.Width - dockPanel1.Width, dockPanel1.Height);
            dp.Hide();
            dp.Text = "Live data logfile: " + Path.GetFileName(filename);
            RealtimeGraphControl lfv = new RealtimeGraphControl();
            LogFilters lfhelper = new LogFilters();
            lfv.SetFilters(lfhelper.GetFiltersFromRegistry());
            dp.Controls.Add(lfv);
            lfv.ImportMSLogfile(filename);
            lfv.Dock = DockStyle.Fill;
            dp.Show();
        }

        private void btnExportToLogWorks_ItemClick(object sender, ItemClickEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "MotronicSuite logfiles|*.msl";
            ofd.Title = "Open Live data logfile";
            ofd.Multiselect = false;
            string logworksstring = GetLogWorksFromRegistry();
            if (logworksstring == string.Empty)
            {
                if (MessageBox.Show("Logworks is not installed on this computer, do you wish to install it now?", "Question", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    Process.Start(System.Windows.Forms.Application.StartupPath + "\\LogWorks\\LogWorks3Setup.exe");
                }
            }
            else if (ofd.ShowDialog() == DialogResult.OK)
            {
                ConvertFileToDif(ofd.FileName, false); 
            }
        }

        private string GetLogWorksFromRegistry()
        {
            RegistryKey TempKey = null;
            string foundvalue = string.Empty;
            TempKey = Registry.LocalMachine;

            using (RegistryKey Settings = TempKey.OpenSubKey("SOFTWARE\\Classes\\d32FileHandler\\Shell\\Open\\Command"))
            {
                if (Settings != null)
                {
                    string[] vals = Settings.GetValueNames();
                    foreach (string a in vals)
                    {
                        try
                        {
                            foundvalue = Settings.GetValue(a).ToString();
                        }
                        catch (Exception E)
                        {
                            Console.WriteLine(E.Message);
                        }
                    }
                }
            }

            if (foundvalue == string.Empty)
            {
                using (RegistryKey Settings = TempKey.OpenSubKey("SOFTWARE\\Classes\\Applications\\LogWorks2.exe\\shell\\Open\\Command"))
                {
                    if (Settings != null)
                    {
                        string[] vals = Settings.GetValueNames();
                        foreach (string a in vals)
                        {
                            try
                            {
                                foundvalue = Settings.GetValue(a).ToString();
                            }
                            catch (Exception E)
                            {
                                Console.WriteLine(E.Message);
                            }
                        }
                    }
                }
            }
            return foundvalue;
        }

        private void ConvertFileToDif(string filename, bool AutoExport)
        {
            System.Windows.Forms.Application.DoEvents();
            DateTime startDate = DateTime.MaxValue;
            DateTime endDate = DateTime.MinValue;
            try
            {
                SymbolCollection sc = new SymbolCollection();
                string[] alllines = File.ReadAllLines(filename);
                //string line = string.Empty;
                char[] sep = new char[1];
                char[] sep2 = new char[1];
                //int linecount = 0;
                sep.SetValue('|', 0);
                sep2.SetValue('=', 0);
                //while ((line = sr.ReadLine()) != null)
                foreach (string line in alllines)
                {
                    string[] values = line.Split(sep);
                    if (values.Length > 0)
                    {
                        try
                        {
                            string dtstring = (string)values.GetValue(0);
                            DateTime dt = new DateTime(Convert.ToInt32(dtstring.Substring(6, 4)), Convert.ToInt32(dtstring.Substring(3, 2)), Convert.ToInt32(dtstring.Substring(0, 2)), Convert.ToInt32(dtstring.Substring(11, 2)), Convert.ToInt32(dtstring.Substring(14, 2)), Convert.ToInt32(dtstring.Substring(17, 2)));
                            if (dt > endDate) endDate = dt;
                            if (dt < startDate) startDate = dt;
                            for (int t = 1; t < values.Length; t++)
                            {
                                string subvalue = (string)values.GetValue(t);
                                string[] subvals = subvalue.Split(sep2);
                                if (subvals.Length == 2)
                                {
                                    string varname = (string)subvals.GetValue(0);
                                    bool sfound = false;
                                    foreach (SymbolHelper sh in sc)
                                    {
                                        if (sh.Varname == varname)
                                        {
                                            sfound = true;
                                        }
                                    }
                                    SymbolHelper nsh = new SymbolHelper();
                                    nsh.Varname = varname;
                                    if (!sfound) sc.Add(nsh);
                                }
                            }
                        }
                        catch (Exception pE)
                        {
                            Console.WriteLine(pE.Message);
                        }
                    }
                }


                if (AutoExport)
                {
                    foreach (SymbolHelper sh in sc)
                    {
                        sh.Color = GetColorFromRegistry(sh.Varname);
                    }
                    DifGenerator difgen = new DifGenerator();

                    difgen.LowAFR = m_appSettings.WidebandLowAFR / 1000;
                    difgen.HighAFR = m_appSettings.WidebandHighAFR / 1000;
                    difgen.MaximumVoltageWideband = m_appSettings.WidebandHighVoltage / 1000;
                    difgen.MinimumVoltageWideband = m_appSettings.WidebandLowVoltage / 1000;
                    difgen.WidebandSymbol = m_appSettings.WidebandLambdaSymbol;
                    difgen.UseWidebandInput = m_appSettings.UseWidebandLambdaThroughSymbol;

                    difgen.onExportProgress += new DifGenerator.ExportProgress(difgen_onExportProgress);
                    System.Windows.Forms.Application.DoEvents();
                    try
                    {
                        difgen.ConvertFileToDif(filename, sc, startDate, endDate, true, true);
                    }
                    catch (Exception expE1)
                    {
                        Console.WriteLine(expE1.Message);
                    }
                }
                else
                {

                    // show selection screen
                    frmPlotSelection plotsel = new frmPlotSelection();
                    foreach (SymbolHelper sh in sc)
                    {
                        plotsel.AddItemToList(sh.Varname);
                    }
                    plotsel.Startdate = startDate;
                    plotsel.Enddate = endDate;
                    plotsel.SelectAllSymbols();
                    if (plotsel.ShowDialog() == DialogResult.OK)
                    {
                        sc = plotsel.Sc;
                        endDate = plotsel.Enddate;
                        startDate = plotsel.Startdate;
                        DifGenerator difgen = new DifGenerator();
                        LogFilters filterhelper = new LogFilters();
                        difgen.SetFilters(filterhelper.GetFiltersFromRegistry());
                        difgen.LowAFR = m_appSettings.WidebandLowAFR / 1000;
                        difgen.HighAFR = m_appSettings.WidebandHighAFR / 1000;
                        difgen.MaximumVoltageWideband = m_appSettings.WidebandHighVoltage / 1000;
                        difgen.MinimumVoltageWideband = m_appSettings.WidebandLowVoltage / 1000;
                        difgen.WidebandSymbol = m_appSettings.WidebandLambdaSymbol;
                        difgen.UseWidebandInput = m_appSettings.UseWidebandLambdaThroughSymbol;
                        difgen.onExportProgress += new DifGenerator.ExportProgress(difgen_onExportProgress);
                        try
                        {
                            if (difgen.ConvertFileToDif(filename, sc, startDate, endDate, m_appSettings.InterpolateLogWorksTimescale, true))
                            {
                                //difgen.ConvertFileToDif(filename, sc, startDate, endDate, false, false);
                                StartLogWorksWithCurrentFile(Path.GetDirectoryName(filename) + "\\" + Path.GetFileNameWithoutExtension(filename) + ".dif");
                            }
                            else
                            {
                                frmInfoBox info = new frmInfoBox("No data was found to export!");
                            }
                        }
                        catch (Exception expE2)
                        {
                            Console.WriteLine(expE2.Message);
                        }
                    }
                    TimeSpan ts = new TimeSpan(endDate.Ticks - startDate.Ticks);
                }
            }
            catch (Exception E)
            {
                Console.WriteLine(E.Message);
            }
        }

        private void StartLogWorksWithCurrentFile(string filename)
        {
            try
            {
                string logworksstring = GetLogWorksFromRegistry();
                if (logworksstring != string.Empty)
                {
                    logworksstring = logworksstring.Substring(1, logworksstring.Length - 1);
                    int idx = logworksstring.IndexOf('\"');
                    if (idx > 0)
                    {
                        logworksstring = logworksstring.Substring(0, idx);
                        //string parameterstring = "\"" + Path.GetDirectoryName(m_trionicFile.GetFileInfo().Filename) + "\\" + DateTime.Now.ToString("yyyyMMdd") + "-CanTraceExt.dif" + "\"";
                        //                        Console.WriteLine(logworksstring);
                        //Console.WriteLine(parameterstring);

                        System.Diagnostics.Process.Start(logworksstring, "\"" + filename + "\"");
                    }
                }
            }
            catch (Exception E)
            {
                Console.WriteLine(E.Message);
            }
        }


        private Color GetColorFromRegistry(string symbolname)
        {
            Color c = Color.Black;
            Int32 win32color = GetValueFromRegistry(symbolname);
            c = Color.FromArgb((int)win32color);
            //c = System.Drawing.ColorTranslator.FromWin32(win32color);
            return c;
        }

        private Int32 GetValueFromRegistry(string symbolname)
        {
            RegistryKey TempKey = null;
            Int32 win32color = 0;
            TempKey = Registry.CurrentUser.CreateSubKey("Software");
            using (RegistryKey Settings = TempKey.CreateSubKey("MotronicSuite\\SymbolColors"))
            {
                if (Settings != null)
                {
                    string[] vals = Settings.GetValueNames();
                    foreach (string a in vals)
                    {
                        try
                        {
                            if (a == symbolname)
                            {
                                string value = Settings.GetValue(a).ToString();
                                win32color = Convert.ToInt32(value);
                            }
                        }
                        catch (Exception E)
                        {
                            Console.WriteLine(E.Message);
                        }
                    }
                }
            }
            return win32color;
        }

        void difgen_onExportProgress(object sender, DifGenerator.ProgressEventArgs e)
        {
            //frmProgressLogWorks.SetProgressPercentage(e.Percentage);
            SetProgressPercentage("Exporting...", e.Percentage);
            /*if (e.Percentage == 100)
            {
                SetStatusText("Export done");
                SetTaskProgress(0, false);
            }*/
        }

        private void barButtonItem7_ItemClick(object sender, ItemClickEventArgs e)
        {
            try
            {
                if (File.Exists(System.Windows.Forms.Application.StartupPath + "//Volvo Motronic 4.4.pdf"))
                {
                    System.Diagnostics.Process.Start(System.Windows.Forms.Application.StartupPath + "//Volvo Motronic 4.4.pdf");
                }
            }
            catch (Exception E2)
            {
                Console.WriteLine(E2.Message);
            }
        }

        private void barButtonItem31_ItemClick(object sender, ItemClickEventArgs e)
        {
            //ScanLibrary(@"C:\Documents and Settings\Guido.MOBICOACH\My Documents\Visual Studio 2008\Projects\Prive\ECU\VolvoMot43\VolvoMot43\bin\Debug\bin", FileType.MOTRONIC44);
        }

        private void btnImportExtraInformation_ItemClick(object sender, ItemClickEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Info files|*.csv;*.dam;*.damos";
            ofd.Multiselect = false;
            if (ofd.ShowDialog() == DialogResult.OK)
            {

                ExternalInformationSource source = new ExternalInformationSource();
                if (ofd.FileName.ToUpper().EndsWith(".CSV"))
                {
                    source.FillSymbolCollection(ofd.FileName, SourceType.CSV, _workingFile.Symbols, _workingFile.Axis, false);
                }
                else
                {
                    source.FillSymbolCollection(ofd.FileName, SourceType.Damos, _workingFile.Symbols, _workingFile.Axis, false);
                }
                gridControl1.DataSource = null;
                Application.DoEvents();
                gridControl1.DataSource = _workingFile.Symbols;
                Application.DoEvents();
                SaveAdditionalSymbols();
                /*gridControl1.RefreshDataSource();
                gridViewSymbols.RefreshData();
                gridControl1.Refresh();*/
                frmInfoBox info = new frmInfoBox("Import of additional information done");

            }
        }

        private void btnReleaseNotes_ItemClick(object sender, ItemClickEventArgs e)
        {
            StartReleaseNotesViewer(m_msiUpdater.GetReleaseNotes(), Application.ProductVersion.ToString());
        }

        private void btnVerifyChecksum_ItemClick(object sender, ItemClickEventArgs e)
        {
        }

        private void gridView2_CustomDrawCell(object sender, DevExpress.XtraGrid.Views.Base.RowCellCustomDrawEventArgs e)
        {
            // draw value filling
            if (e.Column.Name == gcRealtimeValue.Name)
            {
                // get maximum and minumum  value
                //Console.WriteLine("Drawing realtime value");
                try
                {

                    object o = gridView2.GetRow(e.RowHandle);
                    
                    if (o is SymbolHelper)
                    {
                        SymbolHelper sh = (SymbolHelper)o;
                        double maximum = sh.MaxValue;
                        double minimum = sh.MinValue;
                        double actualvalue = sh.CurrentValue;
                        double range = maximum - minimum;

                        double percentage = (actualvalue - minimum) / range;
                        if (percentage < 0) percentage = 0;
                        if (percentage > 1) percentage = 1;
                        double xwidth = percentage * (double)(e.Bounds.Width - 2);
                        if (xwidth > 0)
                        {
                            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(e.Bounds.X - 1, e.Bounds.Y, e.Bounds.Width + 1, e.Bounds.Height);
                            Brush brush = new System.Drawing.Drawing2D.LinearGradientBrush(rect, Color.LightGreen, Color.OrangeRed, System.Drawing.Drawing2D.LinearGradientMode.Horizontal);
                            e.Graphics.FillRectangle(brush, e.Bounds.X + 1, e.Bounds.Y + 1, (float)xwidth, e.Bounds.Height - 2);
                        }
                    }
                    else
                    {
                        Console.WriteLine("dr = null");
                    }
                }
                catch (Exception E)
                {
                    Console.WriteLine(E.Message);
                }
            }
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            EditCurrentSymbol();
            
        }


        private void EditCurrentSymbol()
        {
            //TODO: edit symbol parameters
            if (gridControl3.DataSource is SymbolCollection)
            {
                object o = gridView2.GetRow(gridView2.FocusedRowHandle);

                if (o is SymbolHelper)
                {
                    SymbolHelper shsel = (SymbolHelper)o;
                    frmEditRealtimeSymbol frmeditsymbol = new frmEditRealtimeSymbol();
                    frmeditsymbol.Symbols = GetRealtimeCollection();
                    frmeditsymbol.Symbolname = shsel.Varname;
                    frmeditsymbol.Varname = shsel.Varname;
                    frmeditsymbol.Description = shsel.Description;
                    frmeditsymbol.MinimumValue = shsel.MinValue;
                    frmeditsymbol.MaximumValue = shsel.MaxValue;
                    frmeditsymbol.OffsetValue = shsel.CorrectionOffset;
                    frmeditsymbol.CorrectionValue = shsel.CorrectionFactor;
                    frmeditsymbol.Address = shsel.Start_address;
                    frmeditsymbol.Length = shsel.Length;
                    frmeditsymbol.SymbolColor = shsel.Color;
                    frmeditsymbol.Units = shsel.Units;

                    if (frmeditsymbol.ShowDialog() == DialogResult.OK)
                    {
                        // UPDATE the edited symbol
                        Application.DoEvents();
                        AddSymbolToRealTimeList(frmeditsymbol.Varname, frmeditsymbol.Symbolnumber, frmeditsymbol.MinimumValue, frmeditsymbol.MaximumValue, frmeditsymbol.OffsetValue, frmeditsymbol.CorrectionValue, frmeditsymbol.Description, frmeditsymbol.Address, frmeditsymbol.Length, frmeditsymbol.SymbolColor, frmeditsymbol.Units, true);
                    }
                    gridControl3.Invalidate();
                    gridView2.Invalidate();
                    Application.DoEvents();

                }
            }
        }

        private void AddSymbolToRealTimeList(string symbolname, int symbolnumber, double minvalue, double maxvalue, double offset, double correction, string description, int sramaddress, int length, Color symColor, string Units, bool isUserDefined)
        {
            try
            {
                SymbolCollection sc = new SymbolCollection();
                if (gridControl3.DataSource != null)
                {
                    sc = (SymbolCollection)gridControl3.DataSource;
                }
                bool symbolfound = false;
                foreach (SymbolHelper sh in sc)
                {
                    if (sh.Varname == symbolname)
                    {
                        symbolfound = true;
                        // overwrite other  values
                        sh.Description = description;
                        sh.Symbol_number = symbolnumber;
                        sh.MinValue = (float)minvalue;
                        sh.MaxValue = (float)maxvalue;
                        sh.CorrectionOffset = (float)offset;
                        sh.CorrectionFactor = (float)correction;
                        sh.CurrentValue = 0;
                        sh.PeakValue = sh.MinValue;
                        sh.Start_address = (int)sramaddress;
                        sh.Color = symColor;
                        sh.Length = length;
                        sh.Units = Units;
                    }
                }
                if (!symbolfound)
                {
                    // create new one
                    SymbolHelper shnew = new SymbolHelper();
                    shnew.Varname = symbolname;
                    shnew.Description = description;
                    shnew.Symbol_number = symbolnumber;
                    shnew.MinValue = (float)minvalue;
                    shnew.MaxValue = (float)maxvalue;
                    shnew.CorrectionOffset = (float)offset;
                    shnew.CorrectionFactor = (float)correction;
                    shnew.CurrentValue = 0;
                    shnew.PeakValue = shnew.MinValue;
                    shnew.Start_address = (int)sramaddress;
                    shnew.Length = length;
                    shnew.Color = symColor;
                    shnew.Units = Units;
                    sc.Add(shnew);
                }
                gridControl3.DataSource = null;
                Application.DoEvents();
                gridControl3.DataSource = sc;
                Application.DoEvents();
                onlineGraphControl1.ClearData();
            }
            catch (Exception E)
            {
                Console.WriteLine("Failed to add symbol to realtime list: " + E.Message);
            }
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            if (gridControl3.DataSource is SymbolCollection)
            {
                object o = gridView2.GetRow(gridView2.FocusedRowHandle);

                if (o is SymbolHelper)
                {
                    SymbolHelper shsel = (SymbolHelper)o;
                    SymbolCollection sc = (SymbolCollection)gridControl3.DataSource;
                    foreach (SymbolHelper sh in sc)
                    {
                        if (sh.Varname == shsel.Varname)
                        {
                            sc.Remove(sh);
                            break;
                        }
                    }
                    onlineGraphControl1.ClearData();
                    gridControl3.DataSource = null;
                    Application.DoEvents();
                    gridControl3.DataSource = sc;
                }
                gridControl3.Invalidate();
                Application.DoEvents();
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (gridControl3.DataSource is SymbolCollection)
            {
                frmEditRealtimeSymbol frmeditsymbol = new frmEditRealtimeSymbol();
                // add all known symbols to the list... depending on M4.3 or M4.4
                frmeditsymbol.Symbols = GetRealtimeCollection();
                if (frmeditsymbol.ShowDialog() == DialogResult.OK)
                {
                    Application.DoEvents();
                    AddSymbolToRealTimeList(frmeditsymbol.Varname, frmeditsymbol.Symbolnumber, frmeditsymbol.MinimumValue, frmeditsymbol.MaximumValue, frmeditsymbol.OffsetValue, frmeditsymbol.CorrectionValue, frmeditsymbol.Description, frmeditsymbol.Address, frmeditsymbol.Length, frmeditsymbol.SymbolColor, frmeditsymbol.Units, true);
                }
                gridControl3.Invalidate();
                gridView2.Invalidate();
                Application.DoEvents();
            }
        }

        private void AddToSymbolCollection(SymbolCollection sc, string varname, string description, string units, int address, int length, float minvalue, float maxvalue, float correctionfactor, float correctionoffset, Color clr)
        {
            SymbolHelper newsh = new SymbolHelper();
            newsh.Varname = varname;
            newsh.Description = description;
            newsh.Units = units;
            newsh.Start_address = address;
            newsh.Length = length;
            newsh.MinValue = minvalue;
            newsh.MaxValue = maxvalue;
            newsh.CorrectionFactor = correctionfactor;
            newsh.CorrectionOffset = correctionoffset;
            newsh.Color = clr;
            sc.Add(newsh);
        }

        private SymbolCollection GetRealtimeCollection()
        {
            SymbolCollection sc = new SymbolCollection();
            if (_ecucomms != null)
            {

                if (_ecucomms is M43Communication)
                {
                    // fill with all known M43 symbols
                    AddToSymbolCollection(sc, "TPS", "TPS", "TPS degrees", 0x22, 1, 0, 100, 0.4167F, -5.34F, Color.DimGray);
                    AddToSymbolCollection(sc, "Battery voltage", "Battery voltage", "Volt", 0x36, 1, 0, 16, 0.0704F, 0, Color.Ivory);
                    AddToSymbolCollection(sc, "Engine temp", "Engine temp", "CT", 0x38, 1, -40, 120, 1, -80, Color.LimeGreen);
                    AddToSymbolCollection(sc, "Engine speed", "Engine speed", "Rpm", 0x3B, 1, 0, 8000, 40, 0, Color.Green);
                    AddToSymbolCollection(sc, "Internal load", "Internal load", "ms", 0x40, 1, 0, 20, 0.05F, 0, Color.Red); //OK
                    AddToSymbolCollection(sc, "Ignition advance", "Ignition advance", "IgnAdv", 0x55, 1, -10, 40, -0.75F, 78F, Color.LightBlue);
                    AddToSymbolCollection(sc, "Injection time", "Injection time", "Inj_ms", 0x6F, 1, 0, 30, 0.001513F, 0, Color.SandyBrown);
                    AddToSymbolCollection(sc, "Airmass", "Airmass", "kg/h", 0x99, 1, 0, 100, 1.6F, 0, Color.Yellow);
                    AddToSymbolCollection(sc, "Vehicle speed", "Vehicle speed", "km/h", 0xB8, 1, 0, 255, 1F, 0, Color.Lavender);
                    AddToSymbolCollection(sc, "Turbo duty cycle", "Turbo duty cycle", "T D/C", 0xBC, 1, 0, 100, 0.391F, 0, Color.RoyalBlue);
                }
                else if (_ecucomms is M44Communication)
                {
                    // fill with all known M44 symbols
                    AddToSymbolCollection(sc, "TPS", "TPS", "TPS degrees", 0x22, 1, 0, 100, 0.4167F, -5.34F, Color.DimGray);//OK??? Offset and address?
                    AddToSymbolCollection(sc, "Battery voltage", "Battery voltage", "Volt", 0x36, 1, 0, 16, 0.0704F, 0, Color.Ivory);//OK
                    AddToSymbolCollection(sc, "Engine temp", "Engine temp", "CT", 0x38, 1, -40, 120, 1, -80, Color.LimeGreen); //OK
                    AddToSymbolCollection(sc, "Engine speed", "Engine speed", "Rpm", 0x3B, 1, 0, 8000, 30, 0, Color.Green); //OK
                    AddToSymbolCollection(sc, "Internal load", "Internal load", "ms", 0x40, 1, 0, 20, 0.05F, 0, Color.Red); //OK
                    AddToSymbolCollection(sc, "Ignition advance", "Ignition advance", "IgnAdv", 0x54, 1, -10, 40, -0.75F, 78F, Color.LightBlue);//OK
                    AddToSymbolCollection(sc, "Injection time", "Injection time", "Inj_ms", 0x6E, 1, 0, 30, 0.002F, 0, Color.SandyBrown); //OK
                    AddToSymbolCollection(sc, "Airmass", "Airmass", "kg/h", 0x99, 1, 0, 100, 1.6F, 0, Color.Yellow); //OK
                    AddToSymbolCollection(sc, "Vehicle speed", "Vehicle speed", "km/h", 0xB8, 1, 0, 255, 1F, 0, Color.Lavender);//OK
                    AddToSymbolCollection(sc, "Turbo duty cycle", "Turbo duty cycle", "T D/C", 0xBC, 1, 0, 100, 0.3922F, 0, Color.RoyalBlue); //OK .. address?

                }
            }
            return sc;
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            // save layout
            // save the user defined symbols in the list
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Realtime layout files|*.msrtl";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                SaveRealtimeTable(sfd.FileName);
            }
        }
        private void SaveRealtimeTable(string filename)
        {
            try
            {
                if (gridControl3.DataSource != null)
                {
                    SymbolCollection sc = (SymbolCollection)gridControl3.DataSource;
                    using (StreamWriter sw = new StreamWriter(filename))
                    {
                        foreach (SymbolHelper sh in sc)
                        {
                            sw.WriteLine(sh.Varname + "|" + sh.Description + "|" + sh.Start_address.ToString() + "|" + sh.Length.ToString() + "|" + sh.CorrectionFactor.ToString() + "|" + sh.CorrectionOffset.ToString() + "|" + sh.MinValue.ToString() + "|" + sh.MaxValue.ToString() + "|" + sh.Color.ToArgb().ToString() + "|" + sh.Units);
                        }
                    }
                }
            }
            catch (Exception E)
            {
                Console.WriteLine("Failed to write realtime datatable: " + E.Message);
            }
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            // load layout
            // load user defined symbols
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = false;
            ofd.Filter = "Realtime layout files|*.msrtl";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                LoadRealtimeTable(ofd.FileName);
            }
        }

        private void LoadRealtimeTable(string filename)
        {
            try
            {
                // create a tabel from scratch
                gridControl3.DataSource = null;
                gridControl3.Invalidate();
                gridView2.Invalidate();
                Application.DoEvents();
                SymbolCollection sc = new SymbolCollection();
                if (File.Exists(filename))
                {
                    using (StreamReader sr = new StreamReader(filename))
                    {
                        string line = string.Empty;
                        char[] sep = new char[1];
                        sep.SetValue('|', 0);
                        while ((line = sr.ReadLine()) != null)
                        {
                            
                            //sh.Varname + "|" 
                            //sh.Description + "|" 
                            //sh.Start_address.ToString() + "|" 
                            //sh.Length.ToString() + "|" 
                            //sh.CorrectionFactor.ToString() + "|" 
                            //sh.CorrectionOffset.ToString() + "|" 
                            //sh.MinValue.ToString() + "|" 
                            //sh.MaxValue.ToString() + "|" 
                            //sh.Color.ToArgb().ToString());
                            Console.WriteLine(line);
                            string[] values = line.Split(sep);
                            if (values.Length >= 10)
                            {
                                string symbolname = (string)values.GetValue(0);
                                AddSymbolToRealTimeList(symbolname, 0, ConvertToDouble((string)values.GetValue(6)), ConvertToDouble((string)values.GetValue(7)), ConvertToDouble((string)values.GetValue(5)), ConvertToDouble((string)values.GetValue(4)), (string)values.GetValue(1), Convert.ToInt32((string)values.GetValue(2)),Convert.ToInt32((string)values.GetValue(3)),  Color.FromArgb(Convert.ToInt32((string)values.GetValue(8))),(string)values.GetValue(9), true);
                            }
                        }
                    }
                    onlineGraphControl1.ClearData();
                }
                //gridControl3.DataSource = sc;
            }
            catch (Exception E)
            {
                Console.WriteLine("Failed to load realtime symbol table: " + E.Message);
            }
        }

        private double ConvertToDouble(string v)
        {
            double d = 0;
            if (v == "") return d;
            string vs = "";
            vs = v.Replace(System.Threading.Thread.CurrentThread.CurrentCulture.NumberFormat.NumberGroupSeparator, System.Threading.Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator);
            Double.TryParse(vs, out d);
            return d;
        }

        private void gridControl3_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                toolStripButton3_Click(this, EventArgs.Empty);
            }
        }

        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            if (gridControl3.DataSource != null)
            {
                SymbolCollection sc = (SymbolCollection)gridControl3.DataSource;
                foreach (SymbolHelper sh in sc)
                {
                    sh.PeakValue = sh.MinValue;
                }
                gridControl3.RefreshDataSource();
                Application.DoEvents();

            }
        }

        private void gridControl3_DoubleClick(object sender, EventArgs e)
        {
            EditCurrentSymbol();
        }

        private void btnExportXMLDescriptor_ItemClick(object sender, ItemClickEventArgs e)
        {
            
            
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "XML files|*.xml";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                ExportXMLDescriptor(sfd.FileName);
                
            }
        }

        private void ExportXMLDescriptor(string filename)
        {
            System.Data.DataTable dt = new System.Data.DataTable(Path.GetFileNameWithoutExtension(FileTools.Instance.Currentfile));
            dt.Columns.Add("SYMBOLNAME");
            dt.Columns.Add("SYMBOLNUMBER", Type.GetType("System.Int32"));
            dt.Columns.Add("FLASHADDRESS", Type.GetType("System.Int32"));
            //dt.Columns.Add("LENGTH", Type.GetType("System.Int32"));
            dt.Columns.Add("DESCRIPTION");
            //dt.Columns.Add("AXIS", Type.GetType("System.Boolean"));
            //dt.Columns.Add("SIXTEENBIT", Type.GetType("System.Boolean"));
            string xmlfilename = filename;
            if (File.Exists(xmlfilename)) File.Delete(xmlfilename);
            foreach (SymbolHelper sh in _workingFile.Symbols)
            {
                if (sh.UserDescription == "")
                {
                    sh.UserDescription = sh.Varname;
                }
                if (sh.UserDescription != "")
                {
                    dt.Rows.Add(sh.Varname, sh.Symbol_number, sh.Flash_start_address, sh.UserDescription);
                }
            }
            dt.WriteXml(xmlfilename);
        }
    }
}