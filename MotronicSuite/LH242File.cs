using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MotronicTools;

namespace MotronicSuite
{
    class LH242File : IECUFile
    {
        public override bool IsAutomaticTransmission(out bool found)
        {
            found = false;
            return false;
        }


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

        public override int ReadRpmLimiter()
        {
            return -1; // not supported yet
        }
        public override int ReadSpeedLimiter()
        {
            return -1; // not supported yet
        }
        public override void WriteRpmLimiter(int rpmlimiter)
        {
            // not supported yet
        }
        public override void WriteSpeedLimiter(int speedlimit)
        {
            // not supported yet
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

        public override double GetCorrectionFactorForMap(string symbolname)
        {
            double retval = 1;
            if (symbolname == "Boost map") retval = 0.01; // <GS-16022011>
            else if (symbolname == "Internal load limiter") retval = 0.05;
            else if (symbolname == "Overboost map") retval = 0.005; // <GS-16022011>
            else if (symbolname.StartsWith("VE map")) retval = (double)1 / (double)127;
            else if (symbolname.StartsWith("WOT enrichment")) retval = (double)1 / (double)127;
            else if (symbolname.StartsWith("Cylinder compensation")) retval = (double)1 / (double)127;
            else if (symbolname.StartsWith("Ignition map")) retval = 0.75; //( 360º / #TEETH) / 4 = ( 360º /120) / 4 =0.75º 
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

        private Int32 GetLH242MapTablepointer(string filename)
        {
            // 22 A2 E0 C2 E0 90 MM MM
            Int32 retval = 0;
            FileInfo fi = new FileInfo(filename);
            using (FileStream a_fileStream = new FileStream(filename, FileMode.Open))
            {
                byte[] sequence = new byte[6] { 0x22, 0xA2, 0xE0, 0xC2, 0xE0, 0x90 };

                byte data;
                int i;
                i = 0;
                while (a_fileStream.Position < fi.Length - 1)
                {
                    data = (byte)a_fileStream.ReadByte();
                    if (data == sequence[i])
                    {
                        i++;
                    }
                    else
                    {
                        i = 0;
                    }
                    if (i == sequence.Length) break;
                }
                if (i == sequence.Length)
                {
                    int val = Convert.ToInt32((byte)a_fileStream.ReadByte());
                    int val2 = Convert.ToInt32((byte)a_fileStream.ReadByte());
                    retval = (val * 256) + val2;//(int)a_fileStream.Position;
                }
            }
            return retval;
        }

        private void LoadLH242File(string filename, out SymbolCollection symbols, out AxisCollection axis)
        {
            // parse the file and find the sequence 22 A2 E0 C2 E0 90 MM MM in which MM MM is the map table pointer
            axis = new AxisCollection();
            m_tempaxis = new AxisCollection();
            symbols = new SymbolCollection();
            Int32 MapTablePointer = GetLH242MapTablepointer(filename);
            if (MapTablePointer != 0)
            {
                //MessageBox.Show("LH242 maptable pointer: " + MapTablePointer.ToString("X4"));
                // read map addresses from this point on until we find an invalid address or 0x00 0x00
                FileStream fs = new FileStream(filename, FileMode.Open);
                fs.Position = MapTablePointer;
                using (BinaryReader br = new BinaryReader(fs))
                {
                    bool invalidAddress = false;
                    while (!invalidAddress)
                    {
                        int val = Convert.ToInt32((byte)br.ReadByte());
                        int val2 = Convert.ToInt32((byte)br.ReadByte());
                        Int32 addressfnd = (val * 256) + val2;
                        if (addressfnd > 0 && addressfnd < 0x8000)
                        {
                            //if (Helpers.Instance.CheckForAxisPresent(filename, addressfnd))
                            //{
                            AxisHelper ah = new AxisHelper();
                            ah.Addressinfile = addressfnd;
                            ah.IsLH242 = true;
                            m_tempaxis.Add(ah);
                            /*}
                            else
                            {
                                SymbolHelper sh = new SymbolHelper();
                                sh.Varname = addressfnd.ToString("X4");
                                sh.Flash_start_address = addressfnd;
                                symbols.Add(sh);
                            }*/

                        }
                        else
                        {
                            invalidAddress = true;
                        }
                    }
                }
                fs.Close();
                fs.Dispose();
                // check all axis for a second axis and mapdata
                SymbolCollection m_Unknown_symbols = new SymbolCollection();
                foreach (AxisHelper ah in m_tempaxis)
                {
                    // get the address information for this axus
                    //Console.WriteLine("Filling information for axis at address: " + ah.Addressinfile.ToString("X4"));
                    if (FillAxisInformation(filename, ah))
                    {
                        axis.Add(ah);
                    }
                    else
                    {
                        SymbolHelper sh = new SymbolHelper();
                        sh.Flash_start_address = ah.Addressinfile;
                        sh.Varname = sh.Flash_start_address.ToString("X4");
                        m_Unknown_symbols.Add(sh);
                        // later we have to add length to it based on next found value

                    }
                }

                // copied from old routine
                m_tempaxis = new AxisCollection();
                foreach (AxisHelper ah in axis)
                {
                    int newaxisstart = ah.Addressinfile + ah.Length + 2;
                    if (Helpers.Instance.CheckForAxisPresent(filename, newaxisstart, m_tempaxis, ah.Length))
                    {
                        //Console.WriteLine("Possible Y axis at address : " + newaxisstart.ToString("X4"));
                        AxisHelper ahnew = new AxisHelper();
                        ahnew.IsLH242 = true;
                        ahnew.Addressinfile = newaxisstart;
                        m_tempaxis.Add(ahnew);
                    }
                }
                // alsnog toevoegen aan collectie
                foreach (AxisHelper ahnew in m_tempaxis)
                {

                    if (FillAxisInformation(filename, ahnew))
                    {
                        axis.Add(ahnew);
                    }
                }
                SetProgressPercentage("Analyzing structure", 60);

                foreach (SymbolHelper sh in m_Unknown_symbols)
                {

                    sh.Length = Helpers.Instance.FindFirstAddressInLists(sh.Flash_start_address, m_axis, m_Unknown_symbols) - sh.Flash_start_address;
                    sh.Cols = Helpers.Instance.DetermineColumnsInMapByLength(sh.Length);
                    sh.Rows = Helpers.Instance.DetermineRowsInMapByLength(sh.Length);
                    if (sh.Length <= 0x200) m_symbols.Add(sh);
                }

                SetProgressPercentage("Determining maps", 70);


                axis.SortColumn = "Addressinfile";
                axis.SortingOrder = GenericComparer.SortOrder.Ascending;
                axis.Sort();
                int address = 0;
                int length = 0;
                AxisHelper previousAxisHelper = new AxisHelper();
                int symbolnumber = 0;
                foreach (AxisHelper ah in axis)
                {
                    /*SymbolHelper shaxis = new SymbolHelper();
                    shaxis.Flash_start_address = ah.Addressinfile;
                    shaxis.Varname = shaxis.Flash_start_address.ToString("X4");
                    shaxis.Length = ah.Length;
                    _workingFile.Symbols.Add(shaxis);*/

                    if (address != 0)
                    {
                        // is there a gap?
                        int endofpreviousaxis = address + length + 2;

                        if (endofpreviousaxis < ah.Addressinfile)
                        {
                            int gaplen = ah.Addressinfile - endofpreviousaxis;
                            Console.WriteLine("GAP: " + endofpreviousaxis.ToString("X4") + " - " + ah.Addressinfile.ToString("X4") + " length: " + gaplen.ToString());
                            //Console.WriteLine("AXIS: " + endofpreviousaxis.ToString("X4") + " - " + ah.Addressinfile.ToString("X4") + " length: " + gaplen.ToString());
                            /*if (endofpreviousaxis == 0xFCC5)
                            {
                                Console.WriteLine("PREV AXIS ADDRESS: "+ previousAxisHelper.Addressinfile.ToString("X4"));
                                Console.WriteLine("GAP: " + endofpreviousaxis.ToString("X4") + " - " + ah.Addressinfile.ToString("X4") + " length: " + gaplen.ToString());
                            }*/
                            //                        Console.WriteLine("GAP: " + endofpreviousaxis.ToString("X4") + " - " + ah.Addressinfile.ToString("X4") + " length: " + gaplen.ToString());
                            SymbolHelper sh = new SymbolHelper();
                            sh.Varname = endofpreviousaxis.ToString("X4");
                            sh.Length = gaplen;
                            sh.Symbol_number = symbolnumber++;

                            sh.Flash_start_address = endofpreviousaxis;
                            sh.Cols = Helpers.Instance.DetermineColumnsInMapByLength(sh.Length);
                            sh.Rows = Helpers.Instance.DetermineRowsInMapByLength(sh.Length);
                            if (sh.Length == 0x100)
                            {
                                sh.Category = "Fuel";
                                sh.Varname = "VE map";
                            }
                            symbols.Add(sh);

                        }
                    }
                    length = ah.Length;
                    address = ah.Addressinfile;
                    previousAxisHelper = ah;
                }
                // try to determine ignition maps probablility
                /*SymbolCollection ignition_maps = new SymbolCollection();
                foreach (SymbolHelper sh in symbols)
                {
                    if (sh.Varname == "Ignition map")
                    {
                        sh.Average_value = DetermineAverageMapValue(filename, sh);
                        ignition_maps.Add(sh);
                    }
                }
                ignition_maps.SortColumn = "Average_value";
                ignition_maps.SortingOrder = GenericComparer.SortOrder.Descending;
                ignition_maps.Sort();
                if (ignition_maps.Count == 3)
                {
                    ignition_maps[0].Varname = "Ignition map: Warmup";
                    Console.WriteLine("Warmup map avg: " + ignition_maps[0].Average_value.ToString("F3") + " address: " + ignition_maps[0].Flash_start_address.ToString());
                    ignition_maps[1].Varname = "Ignition map: Normal";
                    Console.WriteLine("Normal map avg: " + ignition_maps[1].Average_value.ToString("F3") + " address: " + ignition_maps[1].Flash_start_address.ToString());
                    ignition_maps[2].Varname = "Ignition map: Knocking";
                    Console.WriteLine("Knock map avg: " + ignition_maps[2].Average_value.ToString("F3") + " address: " + ignition_maps[2].Flash_start_address.ToString());
                }
                foreach (SymbolHelper sh in ignition_maps)
                {
                    foreach (SymbolHelper shorg in symbols)
                    {
                        if (sh.Flash_start_address == shorg.Flash_start_address)
                        {
                            shorg.Varname = sh.Varname;
                            break;
                        }
                    }
                }*/

                SetProgressPercentage("Sorting data", 80);

                // sort the symbol on length, biggest on top
                symbols.SortColumn = "Length";
                symbols.SortingOrder = GenericComparer.SortOrder.Descending;
                symbols.Sort();
                // end of copy

            }

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
            LoadLH242File(m_currentFile, out symbols, out axis);
            m_symbols = symbols;
            m_axis = axis;

            m_fileInfo.Symbols = symbols;
            m_fileInfo.Axis = axis;
            return m_fileInfo;
        }

        public override string GetPartnumber()
        {
            return "";
        }

        public override string GetHardwareID()
        {
            return "";
        }

        public override string GetDamosInfo()
        {
            return "";
        }

        public override string GetSoftwareVersion()
        {
            return "";
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
            bool retval = false;
            // 0x0000 - 0x7FFF
            // 0x3FF6 - 0x3FF7 = checksum
            // 0x3FF8 - 0x3FF9 = complement of checksum
            //CalculateCRC(FileTools.Instance.Currentfile);
            int lh242checksum = CalculateLH242CRC(FileTools.Instance.Currentfile);
            int lh242checksumcomplement = (lh242checksum ^ 0xFFFF) & 0x0000FFFF;
            byte[] crc1bytes = FileTools.Instance.readdatafromfile(FileTools.Instance.Currentfile, 0x3FF6, 2, false);
            int readcrc1 = (Convert.ToInt32(crc1bytes.GetValue(0)) * 256) + Convert.ToInt32(crc1bytes.GetValue(1));
            byte[] crc2bytes = FileTools.Instance.readdatafromfile(FileTools.Instance.Currentfile, 0x3FF8, 2, false);
            int readcrc2 = (Convert.ToInt32(crc2bytes.GetValue(0)) * 256) + Convert.ToInt32(crc2bytes.GetValue(1));
            if (readcrc1 == lh242checksum && readcrc2 == lh242checksumcomplement) retval = true;
            //Console.WriteLine("checksum LH242: " + lh242checksum.ToString("X4") + " : " + lh242checksumcomplement.ToString("X4"));
            return retval;
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

        public override void UpdateChecksum()
        {
            // 0x0000 - 0x7FFF
            // 0x3FF6 - 0x3FF7 = checksum
            // 0x3FF8 - 0x3FF9 = complement of checksum
            // the tricky bit.. the checksums are included in the data to be calculated
            // but since there is a complement, and the checksum is an addition, that does not matter
            int volvocrc = CalculateLH242CRC(FileTools.Instance.Currentfile);
            int lh242checksumcomplement = (volvocrc ^ 0xFFFF) & 0x0000FFFF;

            // CRC is stored @ 0xFF00 and 0xFF01
            FileStream fsi1 = File.OpenWrite(m_currentFile);
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

        public override double GetOffsetForMap(string symbolname)
        {
            return GetMapCorrectionOffset(symbolname);
        }



        private double GetMapCorrectionOffset(string mapname)
        {
            double retval = 0;
            //if (mapname.StartsWith("Boost map")) retval = 0;
            if (mapname.StartsWith("Boost map")) retval = -1; // <GS-16022011>
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
