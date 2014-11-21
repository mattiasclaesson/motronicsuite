using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MotronicTools;
using System.Windows.Forms;

namespace MotronicSuite
{
    class M44File : IECUFile
    {
        private bool m_autoUpdateChecksum = false;

        public bool AutoUpdateChecksum
        {
            get { return m_autoUpdateChecksum; }
            set { m_autoUpdateChecksum = value; }
        }

        public override void SetTransactionLog(TransactionLog transactionLog)
        {
            m_transactionLog = transactionLog;
        }

        public override void SetAutoUpdateChecksum(bool autoUpdate)
        {
            m_autoUpdateChecksum = autoUpdate;
        }

        public override void BackupFile()
        {
            //TODO: create a binary backupfile
        }

        public override FileType DetermineFileType()
        {
            if (File.Exists(m_currentFile))
            {
                FileInfo fi = new FileInfo(m_currentFile);
                if (fi.Length == 0x10000) return FileType.MOTRONIC43;
                else if (fi.Length == 0x20000) return FileType.MOTRONIC44;
                else if (fi.Length == 0x2000) return FileType.LH22;
                else if (fi.Length == 0x4000) return FileType.LH24;
                else if (fi.Length == 0x8000) return FileType.LH242;
            }
            return FileType.UNKNOWN;
        }

        public override void SelectFile(string filename)
        {
            m_currentFile = filename;
        }

        public override bool Exists()
        {
            bool retval = false;
            if (m_currentFile != "")
            {
                if (File.Exists(m_currentFile))
                {
                    retval = true;
                }
            }
            return retval;
        }

        private string StripCategoryFromSymbolName(string symbolname)
        {
            string retval = symbolname;
            char[] sep = new char[1];
            sep.SetValue('.', 0);
            string[] vals = symbolname.Split(sep);
            if (vals.Length == 2)
            {
                retval = vals.GetValue(1).ToString();
            }
            return retval;
        }

        public override double GetCorrectionFactorForMap(string symbolnamein)
        {
            double retval = 1;
            string symbolname = symbolnamein;
            if (symbolname.Contains("."))
            {
                symbolname = StripCategoryFromSymbolName(symbolname);
            }
            //if (symbolname == "Boost map") retval = 0.01; // <GS-16022011>
            if (symbolname == "Boost map") retval = 0.048; // <GS-22032011>
            else if (symbolname == "Internal load limiter") retval = 0.05;
            else if (symbolname == "Overboost map") retval = 0.005; // <GS-16022011>
            else if (symbolname.StartsWith("VE map")) retval = (double)1 / (double)127;
            else if (symbolname.StartsWith("WOT enrichment")) retval = (double)1 / (double)127;
            else if (symbolname.StartsWith("Cylinder compensation")) retval = (double)1 / (double)127;
            else if (symbolname.StartsWith("Ignition map")) retval = 0.75; //( 360º / #TEETH) / 4 = ( 360º /120) / 4 =0.75º 
            else if (symbolname.StartsWith("WOT ignition")) retval = 0.75; //( 360º / #TEETH) / 4 = ( 360º /120) / 4 =0.75º 
            else if (symbolname.StartsWith("WOT ignition")) retval = 0.75; //( 360º / #TEETH) / 4 = ( 360º /120) / 4 =0.75º 
            else if (symbolname == "Dynamic airmass when AC compressor comes on") retval = 0.4;
            else if (symbolname == "Ignition delta when aircocompressor is on") retval = -0.75;
            else if (symbolname == "Delta ignition advance after vacuum (?)") retval = -0.75;
            else if (symbolname == "Dynamic ignition retard") retval = -0.75;
            else if (symbolname == "Dashpot extra air") retval = 0.4;
            else if (symbolname == "Virtual throttle angle from bypass correction") retval = 0.41667;
            else if (symbolname == "Load value from throttle position (angle) including bypass correction") retval = 0.048;
            else if (symbolname == "Increase of idle target rpm when catalyst heating") retval = 10;
            else if (symbolname == "Injection angle on start of injection") retval = -6;
            else if (symbolname == "Altitude dependent leak diagnosis threshold") retval = 0.003906;
            else if (symbolname == "Max. enrichment for knock") retval = 0.007813;
            else if (symbolname == "Re-engange ignition advance") retval = -0.75;
            else if (symbolname == "Startfactor reduction in second range") retval = 0.003906;
            else if (symbolname == "Airmass increase for catalyst heating") retval = 0.4;
            else if (symbolname == "Delta ignition advance for catalyst heating in partload") retval = -0.75;
            else if (symbolname == "Temporary change in ignition advance when catalyst heating in vacuum") retval = 0.003906;
            else if (symbolname == "Temporary change in ignition advance when catalyst heating on partload") retval = 0.003906;
            else if (symbolname == "Injection duration when fuel comes back") retval = 0.007813;
            else if (symbolname == "Volumeflow through the open tank purge valve") retval = 0.026000;
            else if (symbolname == "Maximum duty cycle") retval = 0.390625;
            else if (symbolname == "Dutycycle tank purge valve") retval = 0.390625;
            else if (symbolname == "Coldstart correction factor") retval = 0.007813;
            else if (symbolname == "Restart correction factor") retval = 0.007813;
            else if (symbolname == "Dutycycle bias for boost control") retval = 0.390625;
            else if (symbolname == "Dutycycle correction for boost control for altitude") retval = 0.390625;
            else if (symbolname == "Intake air temperature correction for boost control dutycycle") retval = 0.390625;
            else if (symbolname == "Threshold total ignition retard for boostpressure fadeout") retval = -0.750000;
            else if (symbolname == "Kennfeld fur Laufunruhe-reference-value") retval = 0.517400;
            else if (symbolname == "Kennfeld Absenkungsfaktor fur Lur-Wert bei erkannten Mehrfachaussetzern") retval = 0.003906;
            else if (symbolname == "Kennfeld fur Laufunruhe-Referenzwert zur Mehrfachaussetzererkennung ->Lum-Vergl.") retval = 0.517400;
            else if (symbolname == "MAF to Load conversion map") retval = 0.05;
            return retval;
        }

        public override void GetMapAxisDescriptions(string symbolname, out string x, out string y, out string z)
        {
            x = "";
            y = "";
            z = "";
        }

        public override FileInformation GetFileInfo()
        {
            return m_fileInfo;
        }

        public override void GetMapMatrixWitdhByName(string symbolname, out int columns, out int rows)
        {
            columns = 0;
            rows = 0;
        }

        public override byte[] ReadData(uint offset, uint length, bool issixteenbit)
        {
            return FileTools.Instance.readdatafromfile(m_currentFile, (int)offset, (int)length, false);
        }

        private string m_currentFile = string.Empty;

        private FileInformation m_fileInfo = new FileInformation();
        private TransactionLog m_transactionLog = null;

        public override SymbolCollection Symbols
        {
            get { return m_symbols; }
            set { m_symbols = value; }
        }

        public override event IECUFile.TransactionLogChanged onTransactionLogChanged;

        private void SignalTransactionLogChanged(TransactionEntry entry)
        {
            if (onTransactionLogChanged != null)
            {
                onTransactionLogChanged(this, new TransactionsEventArgs(entry));
            }
        }
        public override event IECUFile.DecodeProgress onDecodeProgress;


        SymbolCollection m_symbols = new SymbolCollection();

        AxisCollection m_axis = new AxisCollection();

        public override AxisCollection Axis
        {
            get { return m_axis; }
            set { m_axis = value; }
        }

        AxisCollection m_tempaxis = new AxisCollection();

        private bool FillAxisInformation(string filename, AxisHelper ah)
        {
            bool retval = false;
            FileStream fs = new FileStream(filename, FileMode.Open);
            fs.Position = ah.Addressinfile;
            using (BinaryReader br = new BinaryReader(fs))
            {
                // read first byte = identifier
                ah.Identifier = (int)br.ReadByte();
                if (ah.Identifier >= 0x03 && ah.Identifier <= 0x99)
                {
                    // now read 1 byte length
                    ah.Length = (int)br.ReadByte();
                    if (ah.Length < 32)
                    {
                        retval = true;
                        ah.Values = new int[ah.Length];
                        for (int i = 0; i < ah.Length; i++)
                        {
                            // read values
                            ah.Values.SetValue((int)br.ReadByte(), i);
                        }
                        ah.CalculateRealValues();
                        //Console.WriteLine("Found axis: " + ah.Descr + " " + ah.Identifier.ToString("X2"));

                    }
                }
                //DumpAxis(ah);
            }
            fs.Close();
            fs.Dispose();
            return retval;
        }

        private void LoadMotronic44File(string filename, out SymbolCollection symbols, out AxisCollection axis)
        {
            //frmInfoBox info = new frmInfoBox("Motronic 4.4 support is not yet implemented");
            // get the list from the file
            // Get axis table from the binary
            // find sequence 00 02 04 00 02 00
            // if the next byte is 00 then we should read on 00 02 04 07 09 and then start
            // otherwise start immediately

            AxisCollection m_gatheredaxisaddress = new AxisCollection();

            int readstate = 0;
            int lookuptablestartaddress = 0x00;
            int axisaddress = 0;
            readstate = 0;
            axis = new AxisCollection();
            m_tempaxis = new AxisCollection();
            symbols = new SymbolCollection();
            SetProgressPercentage("Loading addressmap", 20);

            LoadMotronic44ThirdAxisTable(filename, out m_tempaxis);
            Console.WriteLine("Entries from LoadMotronic44ThirdAxisTable: " + m_tempaxis.Count.ToString());
            foreach (AxisHelper ah in m_tempaxis)
            {
               // DumpAxis(ah);
                if (ah.Addressinfile == 0xF962) Console.WriteLine("Det in third axis!");
                m_gatheredaxisaddress.Add(ah);
            }

            m_tempaxis = new AxisCollection();

            LoadMotronic44MapTable(filename, out m_tempaxis);

            // DEBUG
            foreach (AxisHelper ah in m_tempaxis)
            {
                //DumpAxis(ah);
                if (ah.Addressinfile == 0xF962) Console.WriteLine("Det in maptable!");
                m_gatheredaxisaddress.Add(ah);
            }
            m_tempaxis.SortColumn = "Addressinfile";
            m_tempaxis.SortingOrder = GenericComparer.SortOrder.Ascending;
            m_tempaxis.Sort();

            Console.WriteLine("Entries from LoadMotronic44MapTable: " + m_tempaxis.Count.ToString());
            AxisCollection m_interpolatedAxis = new AxisCollection();
            foreach (AxisHelper ah in m_tempaxis)
            {
                if (ah.Addressinfile == 0xF962) Console.WriteLine("Det in maptable2!");
                m_gatheredaxisaddress.Add(ah);
            }

            // now read all axis addresses upto the end marker
            // now determine the gaps in the axis structure

            int bigmapcount = 0;
            for (int tel = 0; tel < m_tempaxis.Count - 1; tel++)
            {
                int length = m_tempaxis[tel + 1].Addressinfile - m_tempaxis[tel].Addressinfile;
                //Console.WriteLine("Symbol: " + m_tempaxis[tel].Addressinfile.ToString("X4") + "length: " + length.ToString());
                m_tempaxis[tel].Length = length;
                if (length > 0x0100)
                {
                    Console.WriteLine("Table is too big: " + m_tempaxis[tel].Addressinfile.ToString("X6") + " len: " + length.ToString("X4"));
                }
                SymbolHelper sh = new SymbolHelper();
                sh.Flash_start_address = m_tempaxis[tel].Addressinfile;
                sh.Length = m_tempaxis[tel].Length;

                sh.Varname = sh.Flash_start_address.ToString("X4");
                if (sh.Length == 0x100)
                {
                    string category = "Undocumented";
                    string units = "";
                    string xaxisunits = "";
                    string yaxisunits = "";
                    //TODO: fix the naming of maps here!
                    //sh.Varname = GetBigMapLowerPartName(bigmapcount++, sh.Flash_start_address, out category, out units, out xaxisunits, out yaxisunits);
                    sh.Category = category;
                    sh.XDescr = xaxisunits;
                    sh.YDescr = yaxisunits;
                    sh.ZDescr = units;

                }
                sh.Cols = Helpers.Instance.DetermineColumnsInMapByLength(sh.Length);
                sh.Rows = Helpers.Instance.DetermineRowsInMapByLength(sh.Length);
                symbols.Add(sh);
            }
            SetProgressPercentage("Adding axis", 30);

            m_tempaxis = new AxisCollection();
            LoadMotronic44AxisTable(filename, out m_tempaxis);

            // DEBUG
            Console.WriteLine("Entries from LoadMotronic44AxisTable: " + m_tempaxis.Count.ToString());
            if (m_tempaxis.Count > 16) // failsafe
            {
                foreach (AxisHelper ah in m_tempaxis)
                {
                    //DumpAxis(ah);
                    if (ah.Addressinfile == 0xF962) Console.WriteLine("Det in first axis!");
                    m_gatheredaxisaddress.Add(ah);
                }
            }

            bigmapcount = 0;
            m_tempaxis = new AxisCollection();
            LoadMotronicSecondaryMapTable(filename, out m_tempaxis);

            // DEBUG
            Console.WriteLine("Entries from LoadMotronicSecondaryMapTable: " + m_tempaxis.Count.ToString());
            foreach (AxisHelper ah in m_tempaxis)
            {
                if (ah.Addressinfile == 0xF962) Console.WriteLine("Det in second axis!");

                m_gatheredaxisaddress.Add(ah);
                //DumpAxis(ah);
            }

            // now add all axis to tempaxis
            m_tempaxis.Clear();
            foreach (AxisHelper ah in m_gatheredaxisaddress)
            {
                m_tempaxis.Add(ah);
            }

            m_tempaxis.SortColumn = "Addressinfile";
            m_tempaxis.SortingOrder = GenericComparer.SortOrder.Ascending;
            m_tempaxis.Sort();
            SetProgressPercentage("Analyzing structure", 40);

            for (int tel = 0; tel < m_tempaxis.Count - 1; tel++)
            {

                int length = m_tempaxis[tel + 1].Addressinfile - m_tempaxis[tel].Addressinfile;
                //Console.WriteLine("Symbol: " + m_tempaxis[tel].Addressinfile.ToString("X4") + "length: " + length.ToString());
                m_tempaxis[tel].Length = length;
                SymbolHelper sh = new SymbolHelper();
                sh.Flash_start_address = m_tempaxis[tel].Addressinfile;
                sh.Length = m_tempaxis[tel].Length;
                sh.Varname = sh.Flash_start_address.ToString("X4");
                if (sh.Length == 0x100)
                {
                    string category = "Undocumented";
                    int xaxisaddress = 0;
                    int yaxisaddress = 0;
                    string units = "";
                    string xaxisunits = string.Empty;
                    string yaxisunits = string.Empty;
                    //TODO: fix the naming of maps here!
                    //sh.Varname = GetBigMapUpperPartName(bigmapcount++, sh.Flash_start_address, m_axis, out category, out xaxisaddress, out yaxisaddress, out units, out xaxisunits, out yaxisunits);
                    sh.XDescr = xaxisunits;
                    sh.YDescr = yaxisunits;
                    sh.ZDescr = units;
                    if (xaxisaddress != 0)
                    {
                        sh.X_axis_address = xaxisaddress;
                        //sh.X_axis_length = cols;
                        //sh.XDescr = 
                    }
                    if (yaxisaddress != 0)
                    {
                        sh.Y_axis_address = yaxisaddress;
                        //sh.Y_axis_length = rows;
                    }
                    sh.Category = category;
                    //TODO: add subcategory

                }
                sh.Cols = Helpers.Instance.DetermineColumnsInMapByLength(sh.Length);
                sh.Rows = Helpers.Instance.DetermineRowsInMapByLength(sh.Length);
                symbols.Add(sh);
            }

            SetProgressPercentage("Determining maps", 50);

            axis.Clear();

            // see if there are axis in the symbol list.
            //foreach (SymbolHelper sh in symbols)
            foreach (AxisHelper sh in m_gatheredaxisaddress)
            {
                // read 3 bytes, 2 ID (actually SRAM address internally in uC)
                // 1 byte length
                byte[] axisheader = FileTools.Instance.readdatafromfile(filename, sh.Addressinfile, 3, false);
                int id = Convert.ToInt32(axisheader.GetValue(0)) * 256 + Convert.ToInt32(axisheader.GetValue(1));
                int length = Convert.ToInt32(axisheader.GetValue(2));
                if (IsKnownM44Id(id & 0x00FF) && length <= 32)
                {
                    // this is probably an axis, add it to the collection

                    AxisHelper ah = new AxisHelper();
                    ah.IsMotronic44 = true;
                    ah.Addressinfile = sh.Addressinfile;
                    if (FillAxisInformationM44(filename, ah))
                    {
                        //Console.WriteLine("Added M4.4 axis to collection: " + ah.Identifier.ToString("X4") + " adr: " + ah.Addressinfile.ToString("X4"));
                        if (ah.Identifier != 0)
                        {
                            if (ah.Length != 0 && (ah.Length % 2 == 0))
                            {
                                axis.Add(ah);
                                Console.WriteLine("Added axis to collection: " + ah.Addressinfile.ToString("X8") + " " + ah.Descr + " vals " + ah.Values.Length.ToString());
                            }
                            else
                            {
                                Console.WriteLine("Length was zero or uneven for axis: " + ah.Addressinfile.ToString("X8"));
                            }
                        }
                        else
                        {
                            Console.WriteLine("Identifier was zero for axis: " + ah.Addressinfile.ToString("X8"));
                        }
                    }
                    else
                    {
                        Console.WriteLine("Unable to fill axis information for axis: " + ah.Addressinfile.ToString("X8"));
                    }

                }
                else
                {
                    Console.WriteLine("Unknown identifier seen: " + id.ToString("X4"));
                }
            }

            Console.WriteLine("Final axis collection");
            
            SetProgressPercentage("Determining maps, 2nd run", 60);

            // now filter all unwanted stuff from the final collections
            // first copy all symbolhelpers to a temporary storage
            SymbolCollection sctemp = new SymbolCollection();
            foreach (SymbolHelper sh in symbols)
            {
                if (sh.Length <= 512 && sh.Length > 0)
                {
                    sctemp.Add(sh);
                }
            }
            // clear the final collection
            symbols.Clear();
            // do the same for the axis collection
            AxisCollection actemp = new AxisCollection();
            foreach (AxisHelper ah in axis)
            {
                if (ah.Length > 0 && ah.Length <= 32)
                {
                    actemp.Add(ah);
                }
            }
            axis.Clear();
            // now, add all the symbols that are not in the axis collection
            foreach (SymbolHelper sh in sctemp)
            {
                bool fnd = false;
                foreach (AxisHelper ah in actemp)
                {
                    if (ah.Addressinfile == sh.Flash_start_address)
                    {
                        // fnd = true; //<GS-20102009>
                        break;
                    }
                }
                if (!fnd)
                {
                    bool alreadyincollection = false;
                    foreach (SymbolHelper shtmp in symbols)
                    {
                        if (shtmp.Flash_start_address == sh.Flash_start_address)
                        {
                            alreadyincollection = true;
                        }
                    }
                    if (!alreadyincollection)
                    {
                        symbols.Add(sh);
                    }
                }
            }
            SetProgressPercentage("Determining maps, 3rd run", 70);

            // now add the final axis to the collection
            foreach (AxisHelper ah in actemp)
            {
                axis.Add(ah);
            }

            FileTools.Instance.Speedlimit = GetVehicleSpeedLimiter(filename);

            SetProgressPercentage("Sorting data", 80);
            symbols.SortColumn = "Length";
            symbols.SortingOrder = GenericComparer.SortOrder.Descending;
            symbols.Sort();
        }

        private int GetVehicleSpeedLimiterIndex(string filename)
        {
            int readstate = 0;
            int lookuptablestartaddress = 0x00;
            readstate = 0;
            FileStream fs = new FileStream(filename, FileMode.Open);
            fs.Position = 0;
            using (BinaryReader br = new BinaryReader(fs))
            {
                for (int t = 0; t < fs.Length; t++)
                {
                    byte b = br.ReadByte();
                    if (lookuptablestartaddress > 0) break;
                    //01 82 31 31 31 31

                    switch (readstate)
                    {
                        case 0:
                            if (b == 0x01)
                            {
                                readstate++;
                            }
                            else
                            {
                                lookuptablestartaddress = 0x00;
                                readstate = 0;
                            }
                            break;
                        case 1:
                            if (b == 0x82)
                            {
                                readstate++;
                            }
                            else if (b == 0x00)
                            {
                                //readstate++;
                            }
                            else
                            {
                                lookuptablestartaddress = 0x00;
                                readstate = 0;
                            }
                            break;
                        case 2:
                            if (b == 0x31)
                            {
                                readstate++;
                            }
                            else
                            {
                                lookuptablestartaddress = 0x00;
                                readstate = 0;
                            }
                            break;
                        case 3:
                            if (b == 0x31)
                            {
                                readstate++;
                            }
                            else
                            {
                                lookuptablestartaddress = 0x00;
                                readstate = 0;
                            }
                            break;
                        case 4:
                            if (b == 0x31)
                            {
                                readstate++;
                            }
                            else
                            {
                                lookuptablestartaddress = 0x00;
                                readstate = 0;
                            }
                            break;
                        case 5:
                            if (b == 0x31)
                            {
                                // found it
                                lookuptablestartaddress = (int)fs.Position + 3;
                                readstate++;
                                break;
                            }
                            else
                            {
                                lookuptablestartaddress = 0x00;
                                readstate = 0;
                            }
                            break;
                        default:
                            break;

                    }
                }
            }
            fs.Close();
            fs.Dispose();
            return lookuptablestartaddress;
        }

        public override void WriteSpeedLimiter(int speedlimit)
        {
            SetVehicleSpeedLimiter(m_currentFile, speedlimit);
            UpdateChecksum();
        }

        public override int ReadSpeedLimiter()
        {
            FileTools.Instance.Speedlimit = GetVehicleSpeedLimiter(m_currentFile);
            return FileTools.Instance.Speedlimit;
        }

        public override int ReadRpmLimiter()
        {
            //Settings and options.Limiters.Engine speed (RPM) limit
            //Settings and options.Dauerdrehzahlgrenze
            foreach (SymbolHelper sh in m_symbols)
            {
                if (sh.Varname== "Settings and options.Limiters.Engine speed (RPM) limit")
                {
                    byte[] rpmlimit = FileTools.Instance.readdatafromfile(m_currentFile, sh.Flash_start_address, sh.Length, false);
                    if (rpmlimit.Length > 0)
                    {
                        int limit = Convert.ToInt32(rpmlimit[0]) * 30;
                        FileTools.Instance.Rpmlimit = limit;
                        return limit;
                    }
                }
            }
            return -1; // not found
        }
        public override void WriteRpmLimiter(int rpmlimiter)
        {
            
            foreach (SymbolHelper sh in m_symbols)
            {
                if (sh.Varname == "Settings and options.Limiters.Engine speed (RPM) limit")
                {
                    byte[] b2write = new byte[1];
                    int rpmlimbyte = rpmlimiter / 30;
                    b2write.SetValue(Convert.ToByte(rpmlimbyte), 0);
                    FileTools.Instance.savedatatobinary(sh.Flash_start_address, 1, b2write, m_currentFile);
                    FileTools.Instance.savedatatobinary(sh.Flash_start_address + 0x10000, 1, b2write, m_currentFile);
                    UpdateChecksum();
                }
                if (sh.Varname == "Settings and options.Dauerdrehzahlgrenze")
                {
                    byte[] b2write = new byte[1];
                    int rpmlimbyte = rpmlimiter / 30;
                    b2write.SetValue(Convert.ToByte(rpmlimbyte), 0);
                    FileTools.Instance.savedatatobinary(sh.Flash_start_address, 1, b2write, m_currentFile);
                    FileTools.Instance.savedatatobinary(sh.Flash_start_address + 0x10000, 1, b2write, m_currentFile);
                    UpdateChecksum();
                }
            }
        }


        private void SetVehicleSpeedLimiter(string filename, int speedlimit)
        {
            // There is a second speed limiter (shear velociy threshold for switch-off) one byte further
            // in M4.4 we treat that as follows:
            // In stock bins the second parameter is 3km/h higher then the first one.
            // We only use the first one for reading and set the second one 3 km/h higher when writing (max = 255)

            int shearswitchoffspeedlimit = speedlimit + 3;
            if (speedlimit > 255) speedlimit = 255;
            if (speedlimit < 0) speedlimit = 250;
            if (shearswitchoffspeedlimit > 255) shearswitchoffspeedlimit = 255;
            if (shearswitchoffspeedlimit < 0) shearswitchoffspeedlimit = 250;
            byte[] b2write = new byte[2];
            b2write.SetValue(Convert.ToByte(speedlimit), 0);
            b2write.SetValue(Convert.ToByte(shearswitchoffspeedlimit), 0);
            byte[] arrtosearch = new byte[6] { 0x01, 0x82, 0x31, 0x31, 0x31, 0x31 };
            int indexInFile = FileTools.Instance.LookupByteArrayInFile(arrtosearch, filename);
            if (indexInFile > 0)
            {
                indexInFile += 3;
                FileTools.Instance.savedatatobinary(indexInFile, 2, b2write, filename);
            }
            
        }

        public override bool IsAutomaticTransmission(out bool found)
        {
            found = false;
            bool retval = false;
            byte[] arrtosearch = new byte[6] { 0xEE, 0x4E, 0xEE, 0x5E, 0xEE, 0x6E };
            int indexInFile = FileTools.Instance.LookupByteArrayInFile(arrtosearch, m_currentFile);
            if (indexInFile > 0)
            {
                byte[] isauto = FileTools.Instance.readdatafromfile(m_currentFile, indexInFile, 1, false);
                if (isauto.Length > 0)
                {
                    if (isauto[0] == 0x01) retval = true;
                    found = true;
                }
            }
            return retval;
        }

        private int GetVehicleSpeedLimiter(string filename)
        {
            // There is a second speed limiter (shear velociy threshold for switch-off) one byte further
            // in M4.4 we treat that as follows:
            // In stock bins the second parameter is 3km/h higher then the first one.
            // We only use the first one for reading and set the second one 3 km/h higher when writing (max = 255)
            // get index in file
            //01 82 31 31 31 31
            int retval = 0;
            byte[] arrtosearch = new byte[6] { 0x01, 0x82, 0x31, 0x31, 0x31, 0x31 };
            int indexInFile = FileTools.Instance.LookupByteArrayInFile(arrtosearch, filename);
            if (indexInFile > 0)
            {
                indexInFile += 3;
                byte[] limiter = FileTools.Instance.readdatafromfile(filename, indexInFile, 1, false);
                if (limiter.Length > 0)
                {
                    retval = Convert.ToInt32(limiter[0]);
                }
            }
            return retval;
        }

        private void LoadMotronic44ThirdAxisTable(string filename, out AxisCollection axis)
        {
            int readstate = 0;
            int lookuptablestartaddress = 0x00;
            int axisaddress = 0;
            readstate = 0;
            axis = new AxisCollection();
            FileStream fs = new FileStream(filename, FileMode.Open);
            fs.Position = 0;
            using (BinaryReader br = new BinaryReader(fs))
            {
                for (int t = 0; t < fs.Length; t++)
                {
                    byte b = br.ReadByte();
                    //00 02 04 06 00 02 04 06 08 0A 0D

                    switch (readstate)
                    {
                        case 0:
                            if (b == 0x00)
                            {
                                readstate++;
                            }
                            else
                            {
                                lookuptablestartaddress = 0x00;
                                readstate = 0;
                            }
                            break;
                        case 1:
                            if (b == 0x02)
                            {
                                readstate++;
                            }
                            else if (b == 0x00)
                            {
                                //readstate++;
                            }
                            else
                            {
                                lookuptablestartaddress = 0x00;
                                readstate = 0;
                            }
                            break;
                        case 2:
                            if (b == 0x04)
                            {
                                readstate++;
                            }
                            else
                            {
                                lookuptablestartaddress = 0x00;
                                readstate = 0;
                            }
                            break;
                        case 3:
                            if (b == 0x06)
                            {
                                readstate++;
                            }
                            else
                            {
                                lookuptablestartaddress = 0x00;
                                readstate = 0;
                            }
                            break;
                        case 4:
                            if (b == 0x00)
                            {
                                readstate++;
                            }
                            else
                            {
                                lookuptablestartaddress = 0x00;
                                readstate = 0;
                            }
                            break;
                        case 5:
                            if (b == 0x02)
                            {
                                readstate++;
                            }
                            else
                            {
                                lookuptablestartaddress = 0x00;
                                readstate = 0;
                            }
                            break;
                        case 6:
                            if (b == 0x04)
                            {
                                readstate++;
                            }
                            else
                            {
                                lookuptablestartaddress = 0x00;
                                readstate = 0;
                            }
                            break;
                        case 7:
                            if (b == 0x06)
                            {
                                readstate++;
                            }
                            else
                            {
                                lookuptablestartaddress = 0x00;
                                readstate = 0;
                            }
                            break;
                        case 8:
                            if (b == 0x08)
                            {
                                readstate++;
                            }
                            else
                            {
                                lookuptablestartaddress = 0x00;
                                readstate = 0;
                            }
                            break;
                        case 9:
                            if (b == 0x0A)
                            {
                                readstate++;
                            }
                            else
                            {
                                lookuptablestartaddress = 0x00;
                                readstate = 0;
                            }
                            break;

                        case 10:
                            if (b == 0x0D)
                            {
                                readstate++;
                                lookuptablestartaddress = t + 1;
                            }
                            else
                            {
                                lookuptablestartaddress = 0x00;
                                readstate = 0;
                            }
                            break;
                        case 11:
                            // we're reading addresses now
                            if (b == 0x00)
                            {
                                // end of table... stop reading
                                readstate = 13;
                            }
                            else
                            {
                                axisaddress = (int)b * 256;
                                readstate = 12;
                            }
                            break;
                        case 12:
                            axisaddress += (int)b;
                            AxisHelper ah = new AxisHelper();
                            //Console.WriteLine("Symbol address: " + axisaddress.ToString("X4"));
                            ah.Addressinfile = axisaddress;
                            ah.IsMotronic44 = true;
                            axis.Add(ah);
                            axisaddress = 0;
                            readstate = 11;
                            break;
                        case 13:
                            break;
                        default:
                            break;

                    }
                }
            }
            fs.Close();
            fs.Dispose();
            Console.WriteLine("LoadMotronic44ThirdAxisTable lookuptablestartaddress = " + lookuptablestartaddress.ToString("X8"));
        }

        private void LoadMotronic44MapTable(string filename, out AxisCollection axis)
        {
            int readstate = 0;
            int lookuptablestartaddress = 0x00;
            int axisaddress = 0;
            readstate = 0;
            axis = new AxisCollection();
            FileStream fs = new FileStream(filename, FileMode.Open);
            fs.Position = 0;
            using (BinaryReader br = new BinaryReader(fs, Encoding.ASCII))
            {
                for (int t = 0; t < fs.Length; t++)
                {
                    byte b = br.ReadByte();
                    //00 02 04 00 02 00
                    // OR 
                    //00 02 04 00 02 

                    switch (readstate)
                    {
                        case 0:
                            if (b == 0x00)
                            {
                                readstate++;
                            }
                            else
                            {
                                lookuptablestartaddress = 0x00;
                                readstate = 0;
                            }
                            break;
                        case 1:
                            if (b == 0x02)
                            {
                                readstate++;
                            }
                            else if (b == 0x00)
                            {
                                //readstate++;
                            }
                            else
                            {
                                lookuptablestartaddress = 0x00;
                                readstate = 0;
                            }
                            break;
                        case 2:
                            if (b == 0x04)
                            {
                                readstate++;
                            }
                            else
                            {
                                lookuptablestartaddress = 0x00;
                                readstate = 0;
                            }
                            break;
                        case 3:
                            if (b == 0x00)
                            {
                                readstate++;
                            }
                            else
                            {
                                lookuptablestartaddress = 0x00;
                                readstate = 0;
                            }
                            break;
                        case 4:
                            if (b == 0x02)
                            {
                                byte peekbyte = (byte)br.PeekChar();
                                if (peekbyte != 0x00)
                                {
                                    lookuptablestartaddress = t;
                                    readstate = 6;
                                    //fs.Seek(6, SeekOrigin.Current);
                                    //t += 6;
                                }
                                else
                                {
                                    readstate++;
                                }
                            }
                            else
                            {
                                lookuptablestartaddress = 0x00;
                                readstate = 0;
                            }
                            break;
                        case 5:
                            if (b == 0x00)
                            {
                                readstate++;
                                lookuptablestartaddress = t + 1;
                                byte peekbyte = (byte)br.PeekChar();
                                if (peekbyte == 0x00)
                                {
                                    lookuptablestartaddress = t + 7;
                                    fs.Seek(6, SeekOrigin.Current);
                                    t += 6;
                                }
                                Console.WriteLine("LoadMotronic44MapTable: Start of table = " + lookuptablestartaddress.ToString("X8"));

                            }
                            else
                            {
                                lookuptablestartaddress = 0x00;
                                readstate = 0;
                                //readstate++;
                                //lookuptablestartaddress = t;
                                //Console.WriteLine("LoadMotronic44MapTable (alternate): Start of table = " + lookuptablestartaddress.ToString("X8"));
                            }
                            break;
                        case 6:

                            // we're reading addresses now
                            if (b <= 0x10)
                            {
                                // end of table... stop reading
                                readstate = 8;
                            }
                            else
                            {
                                axisaddress = (int)b * 256;
                                readstate = 7;
                            }
                            break;
                        case 7:
                            axisaddress += (int)b;
                            AxisHelper ah = new AxisHelper();
                            //Console.WriteLine("M4.4 Symbol address: " + axisaddress.ToString("X4"));
                            ah.Addressinfile = axisaddress;
                            ah.IsMotronic44 = true;
                            axis.Add(ah);
                            axisaddress = 0;
                            readstate = 6;
                            break;
                        case 8:
                            break;
                        default:
                            break;

                    }
                }
            }
            fs.Close();
            fs.Dispose();
        }

        private void LoadMotronicSecondaryMapTable(string filename, out AxisCollection axis)
        {
            int readstate = 0;
            int lookuptablestartaddress = 0x00;
            int axisaddress = 0;
            readstate = 0;
            axis = new AxisCollection();
            FileStream fs = new FileStream(filename, FileMode.Open);
            fs.Position = 0x13100;
            using (BinaryReader br = new BinaryReader(fs))
            {
                if (br.ReadByte() == 0x00)
                {
                    if (br.ReadByte() == 0x02)
                    {
                        // yes, this is one of those files
                        // read addresses
                        bool endofmap = false;
                        while (!endofmap)
                        {
                            byte b1 = br.ReadByte();
                            byte b2 = br.ReadByte();
                            if (b1 == 0) endofmap = true;
                            else
                            {
                                int address = (int)b1 * 256;
                                address += (int)b2;
                                AxisHelper ah = new AxisHelper();
                                ah.Addressinfile = 0x10000 + address;
                                axis.Add(ah);
                            }
                        }
                    }
                }
            }
            fs.Close();
            fs.Dispose();
            Console.WriteLine("LoadMotronic44SecondaryAxisTable lookuptablestartaddress = " + lookuptablestartaddress.ToString("X8"));

        }


        

        private void LoadMotronic44AxisTable(string filename, out AxisCollection axis)
        {
            int readstate = 0;
            int lookuptablestartaddress = 0x00;
            int axisaddress = 0;
            readstate = 0;
            axis = new AxisCollection();
            FileStream fs = new FileStream(filename, FileMode.Open);
            fs.Position = 0;
            using (BinaryReader br = new BinaryReader(fs))
            {
                for (int t = 0; t < fs.Length; t++)
                {
                    byte b = br.ReadByte();
                    //00 40 00 40 00 40

                    switch (readstate)
                    {
                        case 0:
                            if (b == 0x00)
                            {
                                readstate++;
                            }
                            else
                            {
                                lookuptablestartaddress = 0x00;
                                readstate = 0;
                            }
                            break;
                        case 1:
                            if (b == 0x40)
                            {
                                readstate++;
                            }
                            else if (b == 0x00)
                            {
                                //readstate++;
                            }
                            else
                            {
                                lookuptablestartaddress = 0x00;
                                readstate = 0;
                            }
                            break;
                        case 2:
                            if (b == 0x00)
                            {
                                readstate++;
                            }
                            else
                            {
                                lookuptablestartaddress = 0x00;
                                readstate = 0;
                            }
                            break;
                        case 3:
                            if (b == 0x40)
                            {
                                readstate++;
                            }
                            else
                            {
                                lookuptablestartaddress = 0x00;
                                readstate = 0;
                            }
                            break;
                        case 4:
                            if (b == 0x00)
                            {
                                readstate++;
                            }
                            else
                            {
                                lookuptablestartaddress = 0x00;
                                readstate = 0;
                            }
                            break;
                        case 5:
                            if (b == 0x40)
                            {
                                readstate++;
                                lookuptablestartaddress = t + 1;
                                byte peekbyte = (byte)br.PeekChar();
                                if (peekbyte == 0x00)
                                {
                                    lookuptablestartaddress = t + 11;
                                    fs.Seek(10, SeekOrigin.Current);
                                    t += 10;
                                }
                            }
                            else
                            {
                                lookuptablestartaddress = 0x00;
                                readstate = 0;
                            }
                            break;
                        case 6:
                            // we're reading addresses now
                            if (b == 0x00)
                            {
                                // end of table... stop reading
                                readstate = 8;
                            }
                            else
                            {
                                axisaddress = (int)b * 256;
                                readstate = 7;
                            }
                            break;
                        case 7:
                            axisaddress += (int)b;
                            AxisHelper ah = new AxisHelper();
                            //Console.WriteLine("Symbol address: " + axisaddress.ToString("X4"));
                            ah.Addressinfile = axisaddress;
                            ah.IsMotronic44 = true;
                            axis.Add(ah);
                            axisaddress = 0;
                            readstate = 6;
                            break;
                        case 8:
                            break;
                        default:
                            break;

                    }
                }
            }
            fs.Close();
            fs.Dispose();
            Console.WriteLine("LoadMotronic44AxisTable lookuptablestartaddress = " + lookuptablestartaddress.ToString("X8"));

        }

        private string GetBigMapLowerPartName(int index, int address, out string category, out string units, out string xaxisunits, out string yaxisunits)
        {
            // based on index in the file
            units = "";
            xaxisunits = "";
            yaxisunits = "";
            string mapname = address.ToString("X4");
            category = "Undocumented";
            switch (index)
            {
                case 0:
                    mapname = "Altitude dependent leak diagnosis threshold";
                    category = "Advanced";
                    units = "hPa/s";
                    xaxisunits = "";
                    yaxisunits = "hPa/s";
                    break;
                case 1:
                    mapname = "VE map (knock)";
                    category = "Knock";
                    units = "correction factor";
                    xaxisunits = "rpm";
                    yaxisunits = "load (ms)";
                    break;
                case 2:
                    mapname = "Max. enrichment for knock";
                    category = "Knock";
                    units = "correction factor";
                    xaxisunits = "rpm";
                    yaxisunits = "load (ms)";
                    break;
                case 3:
                    mapname = "Catalyst diagnosis";
                    category = "Lambda";
                    units = "";
                    xaxisunits = "load (ms)";
                    yaxisunits = "rpm";
                    break;
                case 4:
                    mapname = "Re-engange ignition advance";
                    category = "Ignition";
                    units = "degrees";
                    xaxisunits = "coolant (C)";
                    yaxisunits = "rpm";
                    break;
                case 5:
                    mapname = "Startfactor reduction in second range";
                    category = "Fuel";
                    units = "correction factor";
                    xaxisunits = "coolant (C)";
                    yaxisunits = "";
                    break;
                case 6:
                    mapname = "Airmass increase for catalyst heating";
                    category = "Lambda";
                    units = "kg/h";
                    xaxisunits = "coolant (C)";
                    yaxisunits = "ign.count";
                    break;
                case 7:
                    mapname = "Delta ignition advance for catalyst heating in partload";
                    category = "Ignition";
                    units = "degrees";
                    xaxisunits = "load (ms)";
                    yaxisunits = "rpm";
                    break;
                case 8:
                    mapname = "Temporary change in ignition advance when catalyst heating in vacuum";
                    category = "Ignition";
                    units = "degrees";
                    xaxisunits = "coolant (C)";
                    yaxisunits = "rpm";
                    break;
                case 9:
                    mapname = "Temporary change in ignition advance when catalyst heating on partload";
                    category = "Ignition";
                    units = "degrees";
                    xaxisunits = "coolant (C)";
                    yaxisunits = "rpm";
                    break;
                case 10:
                    mapname = "Injection duration when fuel comes back";
                    category = "Fuel";
                    units = "factor";
                    xaxisunits = "load (ms)";
                    yaxisunits = "rpm";
                    break;
                case 11:
                    mapname = "Volumeflow through the open tank purge valve";
                    category = "Purge";
                    units = "m^3/h";
                    xaxisunits = "Throttle angle (TPS)";
                    yaxisunits = "rpm";
                    break;
                case 12:
                    mapname = "Maximum duty cycle";
                    category = "Purge";
                    units = "%";
                    xaxisunits = "Throttle angle (TPS)";
                    yaxisunits = "rpm";
                    break;
                case 13:
                    mapname = "Dutycycle tank purge valve";
                    category = "Purge";
                    units = "%";
                    xaxisunits = "Throttle angle (TPS)";
                    yaxisunits = "rpm";
                    break;
                case 14:
                    mapname = "Coldstart correction factor";
                    category = "Fuel";
                    units = "correction factor";
                    xaxisunits = "coolant (C)";
                    yaxisunits = "timer (in 3.06 sec)";
                    break;
                case 15:
                    mapname = "Restart correction factor";
                    category = "Fuel";
                    units = "correction factor";
                    xaxisunits = "coolant (C)";
                    yaxisunits = "timer (in 3.06 sec)";
                    break;
                case 16:
                    mapname = "Boost map";
                    category = "Boost";
                    units = "load (ms)";
                    xaxisunits = "rpm";
                    yaxisunits = "Throttle angle (TPS) for turbomap";
                    break;
                case 17:
                    mapname = "Dutycycle bias for boost control";
                    category = "Boost";
                    units = "%";
                    xaxisunits = "rpm";
                    yaxisunits = "Throttle angle (TPS) for turbomap";
                    break;
                case 18:
                    mapname = "Dutycycle correction for boost control for altitude";
                    category = "Boost";
                    units = "%";
                    xaxisunits = "corr.factor";
                    yaxisunits = "rpm";
                    break;
                case 19:
                    mapname = "Intake air temperature correction for boost control dutycycle";
                    category = "Boost";
                    units = "%";
                    xaxisunits = "IAT";
                    yaxisunits = "rpm";

                    break;
                case 20:
                    mapname = "Threshold total ignition retard for boostpressure fadeout";
                    category = "Boost";
                    units = "degrees";
                    xaxisunits = "load (ms)";
                    yaxisunits = "rpm";
                    break;
                case 21:
                    mapname = "Kennfeld fur Laufunruhe-reference-value";
                    category = "Undocumented";
                    units = "(RPM/s)^2";
                    xaxisunits = "load (ms)";
                    yaxisunits = "rpm";
                    break;
                case 22:
                    mapname = "Kennfeld Absenkungsfaktor fur Lur-Wert bei erkannten Mehrfachaussetzern";
                    units = "correction factor";
                    xaxisunits = "load (ms)";
                    yaxisunits = "rpm";
                    break;
                case 23:
                    mapname = "Kennfeld fur Laufunruhe-Referenzwert zur Mehrfachaussetzererkennung ->Lum-Vergl.";
                    units = "(RPM/s)^2";
                    xaxisunits = "load (ms)";
                    yaxisunits = "rpm";
                    break;
                case 24:
                    mapname = "Catalyst safe factors";
                    category = "Lambda";
                    units = "factor";
                    xaxisunits = "load (ms)";
                    yaxisunits = "rpm";
                    break;
            }
            return mapname;

        }

        private string GetBigMapUpperPartName(int index, int address, AxisCollection axiscoll, out string category, out int xaxisaddress, out int yaxisaddress, out string units, out string xaxisunits, out string yaxisunits)
        {
            // based on index in the file

            string mapname = address.ToString("X4");
            xaxisaddress = 0;
            yaxisaddress = 0;
            category = "Undocumented";
            units = "";
            xaxisunits = "";
            yaxisunits = "";

            switch (index)
            {
                case 0:
                    mapname = "Dynamic airmass when AC compressor comes on";
                    units = "kg/h";
                    xaxisunits = "load (ms)";
                    yaxisunits = "rpm";

                    break;
                case 1:
                    mapname = "Ignition delta when aircocompressor is on";
                    category = "Ignition";
                    units = "degrees";
                    xaxisunits = "rpm";
                    yaxisunits = "load (ms)";
                    break;
                case 2:
                    mapname = "Delta ignition advance after vacuum (?)";
                    category = "Ignition";
                    units = "degrees";
                    xaxisunits = "rpm";
                    yaxisunits = "load (ms)";
                    break;
                case 3:
                    mapname = "Knock detection threshold";
                    category = "Knock";
                    xaxisunits = "load (ms)";
                    yaxisunits = "rpm";
                    break;
                case 4:
                    mapname = "Dynamic ignition retard";
                    category = "Ignition";
                    units = "degrees";
                    xaxisunits = "rpm";
                    yaxisunits = "delta-TLW(KF) positiv pro 12ms (ms/12ms)";
                    break;
                case 5:
                    mapname = "Dashpot extra air";
                    category = "Dashpot";
                    units = "kg/h";
                    xaxisunits = "TPS";
                    yaxisunits = "rpm";
                    break;
                case 6:
                    mapname = "Ignition map";
                    category = "Ignition";
                    units = "degrees";
                    xaxisunits = "load (ms)";
                    yaxisunits = "rpm";
                    break;
                case 7:
                    mapname = "VE map partload";
                    category = "Fuel";
                    units = "correction factor";
                    xaxisunits = "load (ms)";
                    yaxisunits = "rpm";
                    break;
                case 8:
                    // converteert naar vTPS van airmass en TPS
                    mapname = "Virtual throttle angle from bypass correction";
                    category = "Throttle";
                    units = "virtual TPS angle degrees";
                    xaxisunits = "airmass (kg/h)";
                    yaxisunits = "TPS angle";
                    break;
                case 9:
                    // converteert naar interne load waarden (van TPS en RPM)
                    mapname = "Load value from throttle position (angle) including bypass correction";
                    category = "Throttle";
                    units = "load (ms)";
                    xaxisunits = "TPS angle";
                    yaxisunits = "rpm";
                    break;
                case 10:
                    mapname = "Increase of idle target rpm when catalyst heating";
                    category = "Idle";
                    units = "rpm * 10";
                    xaxisunits = "coolant (C)";
                    yaxisunits = "ign.count";
                    break;
                case 11:
                    // converteert naar inj.hoek
                    mapname = "Injection angle on start of injection";
                    category = "Fuel";
                    units = "Degrees";
                    xaxisunits = "TI - Value for SEFI (ms)";
                    yaxisunits = "rpm";
                    break;

            }

            return mapname;
        }

        private bool IsKnownM44Id(int id)
        {
            if (id < 256) return true;
            return false;
        }

        private bool FillAxisInformationM44(string filename, AxisHelper ah)
        {
            bool retval = false;
            FileStream fs = new FileStream(filename, FileMode.Open);
            fs.Position = ah.Addressinfile;
            ah.IsMotronic44 = true;
            using (BinaryReader br = new BinaryReader(fs))
            {
                // read first byte = identifier
                ah.Identifier = Convert.ToInt32(br.ReadByte()) * 256;
                ah.Identifier += (int)br.ReadByte();
                if (IsKnownM44Id(ah.Identifier & 0x00FF))
                {
                    // now read 1 byte length
                    ah.Length = (int)br.ReadByte();
                    if (ah.Length <= 32)
                    {
                        retval = true;
                        ah.Values = new int[ah.Length];
                        for (int i = 0; i < ah.Length; i++)
                        {
                            // read values
                            ah.Values.SetValue((int)br.ReadByte(), i);
                        }
                        ah.CalculateRealValues();
                    }
                }
                //DumpAxis(ah);
            }
            fs.Close();
            fs.Dispose();
            return retval;
        }

        private void SetProgressPercentage(string info, int percentage)
        {
            if (onDecodeProgress != null)
            {
                onDecodeProgress(this, new DecodeProgressEventArgs(percentage, info));
            }
        }

        public override FileInformation ParseFile()
        {
            m_fileInfo = new FileInformation();
            SymbolCollection symbols = new SymbolCollection();
            AxisCollection axis = new AxisCollection();
            LoadMotronic44File(m_currentFile, out symbols, out axis);
            m_symbols = symbols;
            m_axis = axis;

            m_fileInfo.Symbols = symbols;
            m_fileInfo.Axis = axis;
            return m_fileInfo;
        }

        public override string GetPartnumber()
        {
            string hwid = string.Empty;
            string swid = string.Empty;
            string partnumber = string.Empty;
            string damos = string.Empty;

            DecodeM44FileInformation(m_currentFile, out hwid, out swid, out partnumber, out damos);
            return partnumber;
        }

        public override string GetHardwareID()
        {
            string hwid = string.Empty;
            string swid = string.Empty;
            string partnumber = string.Empty;
            string damos = string.Empty;

            DecodeM44FileInformation(m_currentFile, out hwid, out swid, out partnumber, out damos);
            return hwid;
        }

        public override string GetDamosInfo()
        {
            string hwid = string.Empty;
            string swid = string.Empty;
            string partnumber = string.Empty;
            string damos = string.Empty;
            DecodeM44FileInformation(m_currentFile, out hwid, out swid, out partnumber, out damos);
            return damos;
        }

        public override string GetSoftwareVersion()
        {
            string hwid = string.Empty;
            string swid = string.Empty;
            string partnumber = string.Empty;
            string damos = string.Empty;
            DecodeM44FileInformation(m_currentFile, out hwid, out swid, out partnumber, out damos);
            return swid;
        }

        private void DecodeM44FileInformation(string filename, out string hardwareID, out string softwareID, out string partnumber, out string damosinfo)
        {
            hardwareID = string.Empty;
            softwareID = string.Empty;
            partnumber = string.Empty;
            damosinfo = string.Empty;

            try
            {
                byte[] filebytes = File.ReadAllBytes(filename);
                // search file for indicator M4.4
                for (int i = 0; i < filebytes.Length; i++)
                {
                    if (i < filebytes.Length - 10)
                    {
                        if (filebytes[i] == 0x12 && filebytes[i + 1] == 0x13 && filebytes[i + 2] == 0x14 && filebytes[i + 3] == 0x15 && filebytes[i + 4] == 0x16 && filebytes[i + 5] == 0x17 && filebytes[i + 6] == 0x18 && filebytes[i + 7] == 0x0A && filebytes[i + 8] == 0x08 && filebytes[i + 9] == 0x09)
                        {
                            // found.. now parse info 40 bytes further
                            for (int j = 0; j < 10; j++) hardwareID += Convert.ToChar(filebytes[i + 10 + j]);
                            for (int j = 0; j < 10; j++) softwareID += Convert.ToChar(filebytes[i + 20 + j]);
                            for (int j = 0; j < 7; j++) partnumber += Convert.ToChar(filebytes[i + 30 + j]);
                            Console.WriteLine("hwid: " + hardwareID);
                            Console.WriteLine("swid: " + softwareID);
                            Console.WriteLine("part: " + partnumber);
                            break;
                        }
                    }
                }

                // find damos info M4.4 indicator
                for (int i = 0; i < filebytes.Length; i++)
                {
                    if (i < filebytes.Length - 8)
                    {
                        if (filebytes[i] == 'M' && filebytes[i + 1] == '4' && filebytes[i + 2] == '.' && filebytes[i + 3] == '4')
                        {
                            for (int j = 0; j < 44; j++) damosinfo += Convert.ToChar(filebytes[i - 5 + j]);
                            break;
                        }
                    }
                }

            }
            catch (Exception E)
            {
                Console.WriteLine("DecodeM43FileInformation: " + E.Message);
            }

        }


        public override bool HasSymbol(string symbolname)
        {
            foreach (SymbolHelper sh in m_symbols)
            {
                if (sh.Varname == symbolname) return true;
            }
            return false;
        }

        public override bool IsTableSixteenBits(string symbolname)
        {
            return false;
        }

        public override bool ValidateChecksum()
        {
            // CRC1 is stored @ 0xFF00 and 0xFF01
            // CRC2 is stored @ 0x1FF00 and 0x1FF01
            bool retval = false;
            M44CRC crc = CalculateM44CRC(FileTools.Instance.Currentfile);
            byte[] crc1bytes = FileTools.Instance.readdatafromfile(FileTools.Instance.Currentfile, 0xFF00, 2, false);
            uint readcrc1 = (Convert.ToUInt32(crc1bytes.GetValue(0)) * 256) + Convert.ToUInt32(crc1bytes.GetValue(1));
            byte[] crc2bytes = FileTools.Instance.readdatafromfile(FileTools.Instance.Currentfile, 0x1FF00, 2, false);
            uint readcrc2 = (Convert.ToUInt32(crc2bytes.GetValue(0)) * 256) + Convert.ToUInt32(crc2bytes.GetValue(1));
            if (readcrc1 == crc.Volvocrc1 && readcrc2 == crc.Volvocrc2) retval = true;
            return retval;
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

        public override void UpdateChecksum()
        {
            FileStream fs = new FileStream(m_currentFile, FileMode.Open);
            fs.Position = 0;
            uint volvocrc1 = 0;
            uint volvocrc2 = 0;
            using (BinaryReader br = new BinaryReader(fs))
            {
                while (fs.Position < 0xFF00)
                {
                    volvocrc1 += (uint)br.ReadByte();
                }
                fs.Position = 0x10000;
                while (fs.Position < 0x1FF00)
                {
                    volvocrc2 += (uint)br.ReadByte();
                }
            }
            fs.Close();
            fs.Dispose();
            // CRC1 is stored @ 0xFF00 and 0xFF01
            // CRC2 is stored @ 0x1FF00 and 0x1FF01
            FileStream fsi1 = File.OpenWrite(m_currentFile);
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

        public override double GetOffsetForMap(string symbolname)
        {
            return GetMapCorrectionOffset(symbolname);
        }

        private double GetMapCorrectionOffset(string mapnamein)
        {
            double retval = 0;
            string mapname = mapnamein;
            if (mapname.Contains("."))
            {
                mapname = StripCategoryFromSymbolName(mapname);
            }
            if (mapname.StartsWith("Boost map")) retval = 0;
            //if (mapname.StartsWith("Boost map")) retval = -1; // <GS-16022011>
            if (mapname.StartsWith("Overboost map")) retval = 0; // <GS-16022011>

                // was-1 
            else if (mapname.StartsWith("VE map")) retval = 0;
            //else if (mapname.StartsWith("Ignition map")) retval = -30; // or - 30 ?
            else if (mapname.StartsWith("Ignition map")) retval = -22.5F; // or - 30 ?
            else if (mapname.StartsWith("WOT ignition")) retval = -22.5F; // or - 30 ?
            else if (mapname == "Injection angle on start of injection") retval = 574;
            //0.75 and offset -22,5 
            return retval;
        }

        public override bool WriteData(byte[] data, uint offset)
        {
            FileTools.Instance.savedatatobinary((int)offset, data.Length, data, m_currentFile);
            return true;
        }

        public override bool WriteData(byte[] data, uint offset, string note)
        {
            FileTools.Instance.savedatatobinary((int)offset, data.Length, data, m_currentFile, true, note);
            return true;
        }

        public override bool WriteDataNoLog(byte[] data, uint offset)
        {
            return WriteData(data, offset);
        }
    }
}
