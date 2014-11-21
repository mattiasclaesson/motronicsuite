using System;
using System.Collections.Generic;
using System.Text;
//using System.Security.Permissions;
using Microsoft.Win32;
using System.ComponentModel;
using System.Data;
using System.Data.Odbc;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

//[assembly: RegistryPermissionAttribute(SecurityAction.RequestMinimum,ViewAndModify = "HKEY_CURRENT_USER")]

namespace MotronicSuite
{
    public class AppSettings
    {

        private string m_Comport = "COM1";

        public string Comport
        {
            get { return m_Comport; }
            set
            {
                m_Comport = value;
                SaveRegistrySetting("Comport", m_Comport);
            }
        }

        private bool m_DetermineCommunicationByFileType = false;

        public bool DetermineCommunicationByFileType
        {
            get { return m_DetermineCommunicationByFileType; }
            set
            {
                m_DetermineCommunicationByFileType = value;
                SaveRegistrySetting("DetermineCommunicationByFileType", m_DetermineCommunicationByFileType);

            }
        }

        private bool m_EnableCommLogging = false;

        public bool EnableCommLogging
        {
            get { return m_EnableCommLogging; }
            set
            {
                m_EnableCommLogging = value;
                SaveRegistrySetting("EnableCommLogging", m_EnableCommLogging);
            }
        }

        private bool m_RequestProjectNotes = false;

        public bool RequestProjectNotes
        {
            get { return m_RequestProjectNotes; }
            set
            {
                m_RequestProjectNotes = value;
                SaveRegistrySetting("RequestProjectNotes", m_RequestProjectNotes);
            }
        }

        private string m_ProjectFolder = Application.StartupPath + "\\Projects";

        public string ProjectFolder
        {
            get { return m_ProjectFolder; }
            set
            {
                m_ProjectFolder = value;
                SaveRegistrySetting("ProjectFolder", m_ProjectFolder);
            }
        }

        private int m_LastOpenedType = 0; // 0 = file, 1 = project

        public int LastOpenedType
        {
            get { return m_LastOpenedType; }
            set
            {
                m_LastOpenedType = value;
                SaveRegistrySetting("LastOpenedType", m_LastOpenedType);
            }
        }

        private string m_lastprojectname = "";
        public string Lastprojectname
        {
            get { return m_lastprojectname; }
            set
            {
                if (m_lastprojectname != value)
                {
                    m_lastprojectname = value;
                    SaveRegistrySetting("LastProjectname", m_lastprojectname);
                }
            }
        }

        private bool m_UseNewMapviewer = true;

        public bool UseNewMapviewer
        {
            get { return m_UseNewMapviewer; }
            set
            {
                m_UseNewMapviewer = value;
                SaveRegistrySetting("UseNewMapviewer", m_UseNewMapviewer);
            }
        }

        private bool m_ShowAddressesInHex = true;

        public bool ShowAddressesInHex
        {
            get { return m_ShowAddressesInHex; }
            set
            {
                m_ShowAddressesInHex = value;
                SaveRegistrySetting("ShowAddressesInHex", m_ShowAddressesInHex);
            }
        }

        private string _canDevice = "Lawicel";

        public string CanDevice
        {
            get { return _canDevice; }
            set
            {
                _canDevice = value;
                SaveRegistrySetting("CanDevice", _canDevice);
            }
        }


        private string m_skinname = string.Empty;

        public string Skinname
        {
            get { return m_skinname; }
            set
            {
                m_skinname = value;
                SaveRegistrySetting("Skinname", m_skinname);
            }
        }

        private Font m_RealtimeFont = new Font(FontFamily.GenericSansSerif, 8F, FontStyle.Regular);

        public Font RealtimeFont
        {
            get { return m_RealtimeFont; }
            set
            {
                m_RealtimeFont = value;
                TypeConverter tc = TypeDescriptor.GetConverter(typeof(Font));
                string fontString = tc.ConvertToString(m_RealtimeFont);
                SaveRegistrySetting("RealtimeFont", fontString);
            }
        }

        private bool m_UseWidebandLambdaThroughSymbol = false;

        public bool UseWidebandLambdaThroughSymbol
        {
            get { return m_UseWidebandLambdaThroughSymbol; }
            set
            {
                m_UseWidebandLambdaThroughSymbol = value;
                SaveRegistrySetting("UseWidebandLambdaThroughSymbol", m_UseWidebandLambdaThroughSymbol);
            }
        }

        private bool m_PlayKnockSound = false;

        public bool PlayKnockSound
        {
            get { return m_PlayKnockSound; }
            set
            {
                m_PlayKnockSound = value;
                SaveRegistrySetting("PlayKnockSound", m_PlayKnockSound);
            }
        }

        private bool m_DirectSRAMWriteOnSymbolChange = false;

        public bool DirectSRAMWriteOnSymbolChange
        {
            get { return m_DirectSRAMWriteOnSymbolChange; }
            set
            {
                m_DirectSRAMWriteOnSymbolChange = value;
                SaveRegistrySetting("DirectSRAMWriteOnSymbolChange", m_DirectSRAMWriteOnSymbolChange);
            }
        }

        private bool m_PreventThreeBarRescaling = false;

        public bool PreventThreeBarRescaling
        {
            get { return m_PreventThreeBarRescaling; }
            set { m_PreventThreeBarRescaling = value; }
        }

        private int m_RealtimeLength = 0;

        public int RealtimeLength
        {
            get { return m_RealtimeLength; }
            set
            {
                m_RealtimeLength = value;
                SaveRegistrySetting("RealtimeLength", m_RealtimeLength);
            }
        }

        private bool m_ShowTablesUpsideDown = false;

        public bool ShowTablesUpsideDown
        {
            get { return m_ShowTablesUpsideDown; }
            set
            {
                m_ShowTablesUpsideDown = value;
                SaveRegistrySetting("ShowTablesUpsideDown", m_ShowTablesUpsideDown);
            }
        }

        private bool m_AllowAskForPartnumber = true;

        public bool AllowAskForPartnumber
        {
            get { return m_AllowAskForPartnumber; }
            set
            {
                    m_AllowAskForPartnumber = value;
                    SaveRegistrySetting("AllowAskForPartnumber", m_AllowAskForPartnumber);
            }
        }

        private bool m_SynchronizeMapviewers = true;

        public bool SynchronizeMapviewers
        {
            get { return m_SynchronizeMapviewers; }
            set
            {
                m_SynchronizeMapviewers = value;
                SaveRegistrySetting("SynchronizeMapviewers", m_SynchronizeMapviewers);
            }
        }

        private bool m_FancyDocking = true;

        public bool FancyDocking
        {
            get { return m_FancyDocking; }
            set
            {
                m_FancyDocking = value;
                SaveRegistrySetting("FancyDocking", m_FancyDocking);
            }
        }

        private bool m_AutoLoadLastFile = true;

        public bool AutoLoadLastFile
        {
            get { return m_AutoLoadLastFile; }
            set
            {
                m_AutoLoadLastFile = value;
                SaveRegistrySetting("AutoLoadLastFile", m_AutoLoadLastFile);
            }
        }

        private bool m_AlwaysRecreateRepositoryItems = false;

        public bool AlwaysRecreateRepositoryItems
        {
            get { return m_AlwaysRecreateRepositoryItems; }
            set
            {
                m_AlwaysRecreateRepositoryItems = value;
                SaveRegistrySetting("AlwaysRecreateRepositoryItems", m_AlwaysRecreateRepositoryItems);
            }
        }

        private ViewType m_DefaultViewType = ViewType.Easy;

        public ViewType DefaultViewType
        {
            get { return m_DefaultViewType; }
            set
            {
                m_DefaultViewType = value;
                SaveRegistrySetting("DefaultViewType", (int)m_DefaultViewType);
            }
        }


        private ViewSize m_DefaultViewSize = ViewSize.NormalView;

        public ViewSize DefaultViewSize
        {
            get { return m_DefaultViewSize; }
            set
            {
                m_DefaultViewSize = value;
                SaveRegistrySetting("DefaultViewSize", (int)m_DefaultViewSize);
            }
        }

        private bool m_NewPanelsFloating = false;

        public bool NewPanelsFloating
        {
            get { return m_NewPanelsFloating; }
            set
            {
                m_NewPanelsFloating = value;
                SaveRegistrySetting("NewPanelsFloating", m_NewPanelsFloating);
            }
        }
        private bool m_ShowViewerInWindows = false;

        public bool ShowViewerInWindows
        {
            get { return m_ShowViewerInWindows; }
            set
            {
                m_ShowViewerInWindows = value;
                SaveRegistrySetting("ShowViewerInWindows", m_ShowViewerInWindows);
            }
        }


        private bool m_DisableMapviewerColors = false;

        public bool DisableMapviewerColors
        {
            get { return m_DisableMapviewerColors; }
            set
            {
                m_DisableMapviewerColors = value;
                SaveRegistrySetting("DisableMapviewerColors", m_DisableMapviewerColors);
            }
        }

        private bool m_AutoDockSameFile = false;

        public bool AutoDockSameFile
        {
            get { return m_AutoDockSameFile; }
            set
            {
                m_AutoDockSameFile = value;
                SaveRegistrySetting("AutoDockSameFile", m_AutoDockSameFile);
            }
        }


        private bool m_AutoDockSameSymbol = true;

        public bool AutoDockSameSymbol
        {
            get { return m_AutoDockSameSymbol; }
            set
            {
                m_AutoDockSameSymbol = value;
                SaveRegistrySetting("AutoDockSameSymbol", m_AutoDockSameSymbol);
            }
        }


        private bool m_AutoSizeNewWindows = true;

        public bool AutoSizeNewWindows
        {
            get { return m_AutoSizeNewWindows; }
            set
            {
                m_AutoSizeNewWindows = value;
                SaveRegistrySetting("AutoSizeNewWindows", m_AutoSizeNewWindows);
            }
        }

        private bool m_AutoSizeColumnsInWindows = true;

        public bool AutoSizeColumnsInWindows
        {
            get { return m_AutoSizeColumnsInWindows; }
            set
            {
                m_AutoSizeColumnsInWindows = value;
                SaveRegistrySetting("AutoSizeColumnsInWindows", m_AutoSizeColumnsInWindows);
            }
        }


        private bool m_ShowGraphs = true;

        public bool ShowGraphs
        {
            get { return m_ShowGraphs; }
            set
            {
                m_ShowGraphs = value;
                SaveRegistrySetting("ShowGraphs", m_ShowGraphs);
            }
        }

        private bool m_HideSymbolTable = false;

        public bool HideSymbolTable
        {
            get { return m_HideSymbolTable; }
            set
            {
                m_HideSymbolTable = value;
                SaveRegistrySetting("HideSymbolTable", m_HideSymbolTable);
            }
        }

        private bool m_InterpolateLogWorksTimescale = false;

        public bool InterpolateLogWorksTimescale
        {
            get { return m_InterpolateLogWorksTimescale; }
            set
            {
                m_InterpolateLogWorksTimescale = value;
                SaveRegistrySetting("InterpolateLogWorksTimescale", m_InterpolateLogWorksTimescale);

            }
        }

        private bool m_AutoGenerateLogWorks = false;

        public bool AutoGenerateLogWorks
        {
            get { return m_AutoGenerateLogWorks; }
            set
            {
                m_AutoGenerateLogWorks = value;
                SaveRegistrySetting("AutoGenerateLogWorks", m_AutoGenerateLogWorks);
            }
        }

        private bool m_AutoChecksum = true;

        public bool AutoChecksum
        {
            get { return m_AutoChecksum; }
            set
            {
                m_AutoChecksum = value;
                SaveRegistrySetting("AutoChecksum", m_AutoChecksum);
            }
        }

        private bool m_TemperaturesInFahrenheit = false;

        public bool TemperaturesInFahrenheit
        {
            get { return m_TemperaturesInFahrenheit; }
            set
            {
                m_TemperaturesInFahrenheit = value;
                SaveRegistrySetting("TemperaturesInFahrenheit", m_TemperaturesInFahrenheit);
            }
        }


        private string m_WidebandLambdaSymbol = string.Empty;

        public string WidebandLambdaSymbol
        {
            get { return m_WidebandLambdaSymbol; }
            set
            {
                m_WidebandLambdaSymbol = value;
                SaveRegistrySetting("WidebandLambdaSymbol", m_WidebandLambdaSymbol);
            }
        }

        private double m_WidebandLowVoltage = 0;

        public double WidebandLowVoltage
        {
            get { return m_WidebandLowVoltage; }
            set
            {
                m_WidebandLowVoltage = value;
                SaveRegistrySetting("WidebandLowVoltage", m_WidebandLowVoltage.ToString());
            }
        }
        private double m_WidebandHighVoltage = 5;

        public double WidebandHighVoltage
        {
            get { return m_WidebandHighVoltage; }
            set
            {
                m_WidebandHighVoltage = value;
                SaveRegistrySetting("WidebandHighVoltage", m_WidebandHighVoltage.ToString());
            }
        }
        private double m_WidebandLowAFR = 10;

        public double WidebandLowAFR
        {
            get { return m_WidebandLowAFR; }
            set
            {
                m_WidebandLowAFR = value;
                SaveRegistrySetting("WidebandLowAFR", m_WidebandLowAFR.ToString());
            }
        }
        private double m_WidebandHighAFR = 20;

        public double WidebandHighAFR
        {
            get { return m_WidebandHighAFR; }
            set
            {
                m_WidebandHighAFR = value;
                SaveRegistrySetting("WidebandHighAFR", m_WidebandHighAFR.ToString());
            }
        }


        private string m_TargetECUReadFile = string.Empty;

        public string TargetECUReadFile
        {
            get { return m_TargetECUReadFile; }
            set
            {
                m_TargetECUReadFile = value;
                SaveRegistrySetting("TargetECUReadFile", m_TargetECUReadFile);
            }
        }

        private string m_write_ecuAMDbatchfile = string.Empty;

        public string Write_ecuAMDbatchfile
        {
            get { return m_write_ecuAMDbatchfile; }
            set 
            {
                if (m_write_ecuAMDbatchfile != value)
                {
                    m_write_ecuAMDbatchfile = value;
                    SaveRegistrySetting("WriteECUBatchfile", m_write_ecuAMDbatchfile);
                }
            }
        }

        private string m_write_ecuIntelbatchfile = string.Empty;

        public string Write_ecuIntelbatchfile
        {
            get { return m_write_ecuIntelbatchfile; }
            set
            {
                if (m_write_ecuIntelbatchfile != value)
                {
                    m_write_ecuIntelbatchfile = value;
                    SaveRegistrySetting("WriteECUIntelBatchfile", m_write_ecuIntelbatchfile);
                }
            }
        }

        private string m_write_ecuAtmelbatchfile = string.Empty;

        public string Write_ecuAtmelbatchfile
        {
            get { return m_write_ecuAtmelbatchfile; }
            set
            {
                if (m_write_ecuAtmelbatchfile != value)
                {
                    m_write_ecuAtmelbatchfile = value;
                    SaveRegistrySetting("WriteECUAtmelBatchfile", m_write_ecuAtmelbatchfile);
                }
            }
        }

        private string m_erasebruteforcebatchfile = string.Empty;

        public string Erasebruteforcebatchfile
        {
            get { return m_erasebruteforcebatchfile; }
            set
            {
                if (m_erasebruteforcebatchfile != value)
                {
                    m_erasebruteforcebatchfile = value;
                    SaveRegistrySetting("EraseBruteForceBatchfile", m_erasebruteforcebatchfile);
                }
            }
        }

        private string m_read_ecubatchfile = string.Empty;

        public string Read_ecubatchfile
        {
            get { return m_read_ecubatchfile; }
            set {
                if (m_read_ecubatchfile != value)
                {
                    m_read_ecubatchfile = value;
                    SaveRegistrySetting("ReadECUBatchfile", m_read_ecubatchfile);
                }
            }
        }

        private string m_lastfilename = "";

        private bool m_ShowRedWhite = false;

        public bool ShowRedWhite
        {
            get { return m_ShowRedWhite; }
            set
            {
                if (m_ShowRedWhite != value)
                {
                    m_ShowRedWhite = value;
                    SaveRegistrySetting("ShowRedWhite", m_ShowRedWhite);
                }
            }
        }


        private bool m_AutoExtractSymbols = true;

        public bool AutoExtractSymbols
        {
            get { return m_AutoExtractSymbols; }
            set 
            {
                if(m_AutoExtractSymbols != value)
                {
                    m_AutoExtractSymbols = value;
                    SaveRegistrySetting("AutoExtractSymbols", m_AutoExtractSymbols);
                }
            }
        }



        public string Lastfilename
        {
            get { return m_lastfilename; }
            set {
                if (m_lastfilename != value)
                {
                    m_lastfilename = value;
                    SaveRegistrySetting("LastFilename", m_lastfilename);
                }
            }
        }
        private bool m_viewinhex = false;

        public bool Viewinhex
        {
            get { return m_viewinhex; }
            set 
            {
                if (m_viewinhex != value)
                {
                    m_viewinhex = value;
                    SaveRegistrySetting("ViewInHex", m_viewinhex);
                }
            }
        }

        private bool m_debugmode = false;

        public bool DebugMode
        {
            get { return m_debugmode; }
        }



        private void SaveRegistrySetting(string key, string value)
        {
            RegistryKey TempKey = null;
            TempKey = Registry.CurrentUser.CreateSubKey("Software");

            using (RegistryKey saveSettings = TempKey.CreateSubKey("MotronicSuite"))
            {
                saveSettings.SetValue(key, value);
            }
        }
        private void SaveRegistrySetting(string key, Int32 value)
        {
            RegistryKey TempKey = null;
            TempKey = Registry.CurrentUser.CreateSubKey("Software");

            using (RegistryKey saveSettings = TempKey.CreateSubKey("MotronicSuite"))
            {
                saveSettings.SetValue(key, value);
            }
        }
        private void SaveRegistrySetting(string key, bool value)
        {
            RegistryKey TempKey = null;
            TempKey = Registry.CurrentUser.CreateSubKey("Software");

            using (RegistryKey saveSettings = TempKey.CreateSubKey("MotronicSuite"))
            {
                saveSettings.SetValue(key, value);
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

        public void SaveSettings()
        {
            RegistryKey TempKey = null;
            TempKey = Registry.CurrentUser.CreateSubKey("Software");

            using (RegistryKey saveSettings = TempKey.CreateSubKey("MotronicSuite"))
            {
                saveSettings.SetValue("ViewInHex", m_viewinhex);
                saveSettings.SetValue("LastFilename", m_lastfilename);
                saveSettings.SetValue("AutoExtractSymbols", m_AutoExtractSymbols);
                saveSettings.SetValue("WriteECUBatchfile", m_write_ecuAMDbatchfile);
                saveSettings.SetValue("WriteECUIntelBatchfile", m_write_ecuIntelbatchfile);
                saveSettings.SetValue("WriteECUAtmelBatchfile", m_write_ecuAtmelbatchfile);
                saveSettings.SetValue("EraseBruteForceBatchfile", m_erasebruteforcebatchfile);
                saveSettings.SetValue("ReadECUBatchfile", m_read_ecubatchfile);
                saveSettings.SetValue("ShowRedWhite", m_ShowRedWhite);
                saveSettings.SetValue("TargetECUReadFile", m_TargetECUReadFile);
                saveSettings.SetValue("WidebandLambdaSymbol", m_WidebandLambdaSymbol);
                saveSettings.SetValue("AutoChecksum", m_AutoChecksum);
                saveSettings.SetValue("TemperaturesInFahrenheit", m_TemperaturesInFahrenheit);
                saveSettings.SetValue("AutoGenerateLogWorks", m_AutoGenerateLogWorks);
                saveSettings.SetValue("InterpolateLogWorksTimescale", m_InterpolateLogWorksTimescale);
                saveSettings.SetValue("ShowGraphs", m_ShowGraphs);
                saveSettings.SetValue("HideSymbolTable", m_HideSymbolTable);
                saveSettings.SetValue("AutoSizeNewWindows", m_AutoSizeNewWindows);
                saveSettings.SetValue("AutoSizeColumnsInWindows", m_AutoSizeColumnsInWindows);
                saveSettings.SetValue("DisableMapviewerColors", m_DisableMapviewerColors);
                saveSettings.SetValue("AutoDockSameFile", m_AutoDockSameFile);
                saveSettings.SetValue("AutoDockSameSymbol", m_AutoDockSameSymbol);
                saveSettings.SetValue("ShowViewerInWindows", m_ShowViewerInWindows);
                saveSettings.SetValue("NewPanelsFloating", m_NewPanelsFloating);

                saveSettings.SetValue("DefaultViewType", (int)m_DefaultViewType);
                saveSettings.SetValue("DefaultViewSize", (int)m_DefaultViewSize);
                saveSettings.SetValue("AutoLoadLastFile", m_AutoLoadLastFile);
                saveSettings.SetValue("FancyDocking", m_FancyDocking);
                saveSettings.SetValue("AlwaysRecreateRepositoryItems", m_AlwaysRecreateRepositoryItems);
                saveSettings.SetValue("SynchronizeMapviewers", m_SynchronizeMapviewers);
                saveSettings.SetValue("AllowAskForPartnumber", m_AllowAskForPartnumber);
                saveSettings.SetValue("ShowTablesUpsideDown", m_ShowTablesUpsideDown);
                saveSettings.SetValue("PlayKnockSound", m_PlayKnockSound);
                saveSettings.SetValue("ShowAddressesInHex", m_ShowAddressesInHex);
                saveSettings.SetValue("UseNewMapviewer", m_UseNewMapviewer);
                saveSettings.SetValue("UseWidebandLambdaThroughSymbol", m_UseWidebandLambdaThroughSymbol);
                saveSettings.SetValue("Skinname", m_skinname);
                saveSettings.SetValue("CanDevice", _canDevice);
                saveSettings.SetValue("DirectSRAMWriteOnSymbolChange", m_DirectSRAMWriteOnSymbolChange);
                saveSettings.SetValue("RealtimeLength", m_RealtimeLength);
                TypeConverter tc = TypeDescriptor.GetConverter(typeof(Font));
                string fontString = tc.ConvertToString(m_RealtimeFont);
                SaveRegistrySetting("RealtimeFont", fontString);
                saveSettings.SetValue("RealtimeFont", fontString);
                saveSettings.SetValue("WidebandLowVoltage", m_WidebandLowVoltage.ToString());
                saveSettings.SetValue("WidebandHighVoltage", m_WidebandHighVoltage.ToString());
                saveSettings.SetValue("WidebandLowAFR", m_WidebandLowAFR.ToString());
                saveSettings.SetValue("WidebandHighAFR", m_WidebandHighAFR.ToString());

                saveSettings.SetValue("LastProjectname", m_lastprojectname);
                saveSettings.SetValue("LastOpenedType", m_LastOpenedType);
                saveSettings.SetValue("ProjectFolder", m_ProjectFolder);
                saveSettings.SetValue("RequestProjectNotes", m_RequestProjectNotes);
                saveSettings.SetValue("EnableCommLogging", m_EnableCommLogging);
                saveSettings.SetValue("DetermineCommunicationByFileType", m_DetermineCommunicationByFileType);

                saveSettings.SetValue("Comport", m_Comport);

            }
        }

        public AppSettings()
        {


            // laad alle waarden uit het register
            RegistryKey TempKey = null;
            TempKey = Registry.CurrentUser.CreateSubKey("Software");


            using (RegistryKey Settings = TempKey.CreateSubKey("MotronicSuite"))
            {
                if (Settings != null)
                {
                    string[] vals = Settings.GetValueNames();
                    foreach (string a in vals)
                    {
                        try
                        {
                            if (a == "ViewInHex")
                            {
                                m_viewinhex = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            if (a == "DebugMode")
                            {
                                m_debugmode = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "LastFilename")
                            {
                                m_lastfilename = Settings.GetValue(a).ToString();
                            }
                            else if (a == "AutoExtractSymbols")
                            {
                                m_AutoExtractSymbols = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "WriteECUBatchfile")
                            {
                                m_write_ecuAMDbatchfile = Settings.GetValue(a).ToString();
                            }
                            else if (a == "EraseBruteForceBatchfile")
                            {
                                m_erasebruteforcebatchfile = Settings.GetValue(a).ToString();
                            }
                            else if (a == "WriteECUIntelBatchfile")
                            {
                                m_write_ecuIntelbatchfile = Settings.GetValue(a).ToString();
                            }
                            else if (a == "WriteECUAtmelBatchfile")
                            {
                                m_write_ecuAtmelbatchfile = Settings.GetValue(a).ToString();
                            }
                            else if (a == "WidebandLowVoltage")
                            {
                                m_WidebandLowVoltage = ConvertToDouble(Settings.GetValue(a).ToString());
                            }
                            else if (a == "WidebandHighVoltage")
                            {
                                m_WidebandHighVoltage = ConvertToDouble(Settings.GetValue(a).ToString());
                            }
                            else if (a == "WidebandLowAFR")
                            {
                                m_WidebandLowAFR = ConvertToDouble(Settings.GetValue(a).ToString());
                            }
                            else if (a == "WidebandHighAFR")
                            {
                                m_WidebandHighAFR = ConvertToDouble(Settings.GetValue(a).ToString());
                            }
                            else if (a == "TargetECUReadFile")
                            {
                                m_TargetECUReadFile = Settings.GetValue(a).ToString();
                            }
                            else if (a == "WidebandLambdaSymbol")
                            {
                                m_WidebandLambdaSymbol = Settings.GetValue(a).ToString();
                            }
                            else if (a == "AutoChecksum")
                            {
                                m_AutoChecksum = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "TemperaturesInFahrenheit")
                            {
                                m_TemperaturesInFahrenheit = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "AutoGenerateLogWorks")
                            {
                                m_AutoGenerateLogWorks = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "InterpolateLogWorksTimescale")
                            {
                                m_InterpolateLogWorksTimescale = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "HideSymbolTable")
                            {
                                m_HideSymbolTable = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "ShowGraphs")
                            {
                                m_ShowGraphs = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "AutoSizeNewWindows")
                            {
                                m_AutoSizeNewWindows = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "AutoSizeColumnsInWindows")
                            {
                                m_AutoSizeColumnsInWindows = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }

                            else if (a == "ReadECUBatchfile")
                            {
                                m_read_ecubatchfile = Settings.GetValue(a).ToString();
                            }
                            else if (a == "ShowRedWhite")
                            {
                                m_ShowRedWhite = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "DisableMapviewerColors")
                            {
                                m_DisableMapviewerColors = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "AutoDockSameFile")
                            {
                                m_AutoDockSameFile = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "AutoDockSameSymbol")
                            {
                                m_AutoDockSameSymbol = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "ShowViewerInWindows")
                            {
                                m_ShowViewerInWindows = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "NewPanelsFloating")
                            {
                                m_NewPanelsFloating = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "AlwaysRecreateRepositoryItems")
                            {
                                m_AlwaysRecreateRepositoryItems = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "AutoLoadLastFile")
                            {
                                m_AutoLoadLastFile = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "FancyDocking")
                            {
                                m_FancyDocking = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "DefaultViewType")
                            {
                                m_DefaultViewType = (ViewType)Convert.ToInt32(Settings.GetValue(a).ToString());
                            }
                            else if (a == "DefaultViewSize")
                            {
                                m_DefaultViewSize = (ViewSize)Convert.ToInt32(Settings.GetValue(a).ToString());
                            }
                            else if (a == "SynchronizeMapviewers")
                            {
                                m_SynchronizeMapviewers = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "AllowAskForPartnumber")
                            {
                                m_AllowAskForPartnumber = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "ShowTablesUpsideDown")
                            {
                                m_ShowTablesUpsideDown = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "RealtimeLength")
                            {
                                m_RealtimeLength= Convert.ToInt32(Settings.GetValue(a).ToString());
                            }
                            else if (a == "PreventThreeBarRescaling")
                            {
                                m_PreventThreeBarRescaling = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "PlayKnockSound")
                            {
                                m_PlayKnockSound = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "ShowAddressesInHex")
                            {
                                m_ShowAddressesInHex = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "UseNewMapviewer")
                            {
                                m_UseNewMapviewer = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "UseWidebandLambdaThroughSymbol")
                            {
                                m_UseWidebandLambdaThroughSymbol = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "Skinname")
                            {
                                m_skinname = Settings.GetValue(a).ToString();
                            }
                            else if (a == "CanDevice")
                            {
                                _canDevice = Settings.GetValue(a).ToString();
                            }

                            else if (a == "DirectSRAMWriteOnSymbolChange")
                            {
                                m_DirectSRAMWriteOnSymbolChange = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "RealtimeFont")
                            {
                                //m_RealtimeFont = new Font(Settings.GetValue(a).ToString(), 10F);
                                TypeConverter tc = TypeDescriptor.GetConverter(typeof(Font));
                                //string fontString = tc.ConvertToString(font);
                                //Console.WriteLine("Font as string: {0}", fontString);

                                m_RealtimeFont = (Font)tc.ConvertFromString(Settings.GetValue(a).ToString());

                                
                            }

                            else if (a == "RequestProjectNotes")
                            {
                                m_RequestProjectNotes = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "DetermineCommunicationByFileType")
                            {
                                m_DetermineCommunicationByFileType = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "EnableCommLogging")
                            {
                                m_EnableCommLogging = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "LastProjectname")
                            {
                                m_lastprojectname = Settings.GetValue(a).ToString();
                            }
                            else if (a == "ProjectFolder")
                            {
                                m_ProjectFolder = Settings.GetValue(a).ToString();
                            }
                            else if (a == "LastOpenedType")
                            {
                                m_LastOpenedType = Convert.ToInt32(Settings.GetValue(a).ToString());
                            }
                            else if (a == "Comport")
                            {
                                m_Comport = Settings.GetValue(a).ToString();
                            }
                        }
                        catch (Exception E)
                        {
                            Console.WriteLine("error retrieving registry settings: " + E.Message);
                        }

                    }
                }
            }

        }
    }
}
