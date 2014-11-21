using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MotronicTools;

namespace MotronicSuite
{
    public class M18File : IECUFile
    {
        public override bool IsAutomaticTransmission(out bool found)
        {
            found = false;
            return false;
        }


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


        SymbolCollection m_symbols = new SymbolCollection();

        AxisCollection m_axis = new AxisCollection();

        public override AxisCollection Axis
        {
            get { return m_axis; }
            set { m_axis = value; }
        }

        AxisCollection m_tempaxis = new AxisCollection();

        private bool CheckForAxisPresent(string filename, int startaddress, AxisCollection axis, int lengthOfPreviousAxis)
        {
            bool retval = false;
            FileStream fs = new FileStream(filename, FileMode.Open);
            fs.Position = startaddress;
            using (BinaryReader br = new BinaryReader(fs))
            {
                int id = (int)br.ReadByte();
                int length = (int)br.ReadByte();
                if ((id >= 0x03 && id <= 0x70 || id == 0xCA) && length > 1 && length < 32)
                {
                    // now also check wether a new axis starts (more likely) at the location AFTER this,
                    // that is, if we assume that the data here is actually a 2D map in stead of an axis
                    fs.Position = startaddress + lengthOfPreviousAxis;
                    int id2 = (int)br.ReadByte();
                    int len2 = (int)br.ReadByte();
                    if (id2 == 0x3B || id2 == 0x38 || id2 == 0x36 || id2 == 0x40 || id2 == 0x4C || id2 == 0x55)
                    {
                        if (len2 > 1 && len2 < 32)
                        {
                            int pos = startaddress + lengthOfPreviousAxis;
                            Console.WriteLine("Overruled axis detection at : " + pos.ToString("X4"));
                            return false;
                        }
                    }

                    if (!Helpers.Instance.AxisPresentInCollection(startaddress, axis))
                    {
                        retval = true;
                    }
                }
            }
            return retval;
        }

        private bool FillAxisInformation(string filename, AxisHelper ah)
        {
            bool retval = false;
            FileStream fs = new FileStream(filename, FileMode.Open);
            fs.Position = ah.Addressinfile;
            using (BinaryReader br = new BinaryReader(fs))
            {
                // read first byte = identifier
                ah.Identifier = (int)br.ReadByte();
                if (ah.Identifier >= 0x03 && ah.Identifier <= 0x99 || ah.Identifier == 0xCA)
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

        private void LoadMotronic18File(string filename, out SymbolCollection symbols, out AxisCollection axis)
        {
            // Get axis table from the binary
            // $E328 
            // find sequence 00 02 05 07
            int readstate = 0;
            int lookuptablestartaddress = 0x00;
            int axisaddress = 0;
            readstate = 0;
            axis = new AxisCollection();
            m_tempaxis = new AxisCollection();
            SymbolCollection m_tempSymbols = new SymbolCollection();
            symbols = new SymbolCollection();
            byte[] _fileParameters = new byte[256];
            int _fileParameterIndex = 0;

            byte[] datacheck = FileTools.Instance.readdatafromfile(filename, 0, 16, false);
            bool _fileValid = false;
            foreach (byte b in datacheck)
            {
                if (b != 0xFF) _fileValid = true;
            }
            if (!_fileValid)
            {
                frmInfoBox info = new frmInfoBox("This is not a M1.8 binary. These file types are not supported.");
                return;
            }
            int _fuelAndIgnitionAddress = 0;
            FileStream fs = new FileStream(filename, FileMode.Open);
            fs.Position = 0x7900;
            using (BinaryReader br = new BinaryReader(fs))
            {
                for (int t = 0x7900; t < fs.Length; t++)
                {
                    byte b = br.ReadByte();
                    //00 02 05 07

                    switch (readstate)
                    {
                        case 0:
                            // we're reading addresses now
                            if (b == 0xFF)
                            {
                                // end of table... stop reading
                                readstate = 2;
                            }
                            else
                            {
                                axisaddress = (int)b * 256;
                                readstate = 1;
                            }
                            break;
                        case 1:
                            axisaddress += (int)b;
                            if (axisaddress > 0x2000)
                            {
                                AxisHelper ah = new AxisHelper();
                                ah.IsM18 = true;
                                 Console.WriteLine("Axis address: " + axisaddress.ToString("X4"));
                                if (m_tempaxis.Count == 0) _fuelAndIgnitionAddress = axisaddress;
                                ah.Addressinfile = axisaddress;
                                m_tempaxis.Add(ah);
                            }
                            axisaddress = 0;
                            readstate = 0;
                            break;
                        case 2:
                            break;
                        default:
                            break;

                    }
                }
            }
            fs.Close();
            fs.Dispose();

            



            SetProgressPercentage("Analyzing structure", 30);
            // from here, read 3 times 0F and find the limiters from there

            //Console.WriteLine("Speed limiter: " + _fileParameters[118].ToString() + " km/h");

            // now read all axis addresses upto the end marker
            foreach (AxisHelper ah in m_tempaxis)
            {
                // get the address information for this axus
                Console.WriteLine("Filling information for axis at address: " + ah.Addressinfile.ToString("X4"));
                if (FillAxisInformation(filename, ah))
                {
                    if (!Helpers.Instance.AxisPresentInCollection(ah.Addressinfile, axis))
                    {
                        axis.Add(ah);
                        Console.WriteLine("Added axis: " + ah.Addressinfile.ToString("X4"));
                    }
                }
                else
                {
                    Console.WriteLine("What to do with : " + ah.Addressinfile.ToString("X4"));
                    SymbolHelper sh = new SymbolHelper();
                    sh.Flash_start_address = ah.Addressinfile;
                    m_tempSymbols.Add(sh);
                }

            }
            SetProgressPercentage("Adding axis", 40);

            // add secondary (Y) axis stuff that may not be in the lookup table
            m_tempaxis = new AxisCollection();
            foreach (AxisHelper ah in axis)
            {
                int newaxisstart = ah.Addressinfile + ah.Length + 2;
                Console.WriteLine("Axis: " + ah.Addressinfile.ToString("X4") + " " + ah.Length.ToString());
                if (CheckForAxisPresent(filename, newaxisstart, m_tempaxis, ah.Length))
                {
                    Console.WriteLine("Possible Y axis at address : " + newaxisstart.ToString("X4"));
                    AxisHelper ahnew = new AxisHelper();
                    ahnew.IsM18 = true;
                    ahnew.Addressinfile = newaxisstart;
                    m_tempaxis.Add(ahnew);
                }
            }
            SetProgressPercentage("Adding axis, 2nd run", 60);
            // alsnog toevoegen aan collectie
            foreach (AxisHelper ahnew in m_tempaxis)
            {

                if (FillAxisInformation(filename, ahnew))
                {
                    if (!Helpers.Instance.AxisPresentInCollection(ahnew.Addressinfile, axis))
                    {
                        Console.WriteLine("Add to axis (3rd): " + ahnew.Addressinfile.ToString("X4"));
                        axis.Add(ahnew);
                    }
                }
            }

            // first pointer in the stack points to fuel and ignition maps!
            if (_fuelAndIgnitionAddress > 0)
            {

                SymbolHelper shfuel = new SymbolHelper();
                shfuel.Flash_start_address = _fuelAndIgnitionAddress;
                shfuel.Length = 256;
                shfuel.Varname = "Ignition map";
                shfuel.Cols = 16;
                shfuel.Rows = 16;
                shfuel.Category = "Ignition";
                symbols.Add(shfuel);

                // we need to manually add some axis to it as well.
                SymbolHelper shignition = new SymbolHelper();
                shignition.Flash_start_address = _fuelAndIgnitionAddress + 256;
                shignition.Length = 256;
                shignition.Cols = 16;
                shignition.Rows = 16;
                shignition.Varname = "VE map";
                shignition.Category = "Fuel";
                // we need to manually add some axis to it as well.
                symbols.Add(shignition);
            }

            // now determine the gaps in the axis structure

            axis.SortColumn = "Addressinfile";
            axis.SortingOrder = GenericComparer.SortOrder.Ascending;
            axis.Sort();

            //<GS-01032011> to correctly determine the LAST map in the sequence with this algoritm
            // we need to insert a dummy axis at the first address AFTER the data
            // check whether the two last axis in the collection join up to form a 3D map
            // otherwise its just a 2D map
            if (axis.Count > 2)
            {
                AxisHelper ah = new AxisHelper();
                ah.IsM18 = true;
                ah.Length = 0;
                ah.Descr = "Dummy";
                if (axis[axis.Count - 2].Addressinfile + axis[axis.Count - 2].Length + 2 == axis[axis.Count - 1].Addressinfile)
                {
                    // 3D map
                    ah.Addressinfile = axis[axis.Count - 2].Addressinfile + 4 + axis[axis.Count - 2].Length + axis[axis.Count - 1].Length + (axis[axis.Count - 2].Length * axis[axis.Count - 1].Length);
                }
                else
                {
                    // 2D map
                    ah.Addressinfile = axis[axis.Count - 1].Addressinfile + 2 + axis[axis.Count - 1].Length;
                }
                Console.WriteLine("Added dummy axis: " + ah.Addressinfile.ToString("X4"));
                axis.Add(ah);
            }


            int address = 0;
            int length = 0;
            AxisHelper previousAxisHelper = new AxisHelper();
            int symbolnumber = 0;
            SetProgressPercentage("Determing maps", 80);

            m_tempSymbols.SortColumn = "Flash_start_address";
            m_tempSymbols.SortingOrder = GenericComparer.SortOrder.Ascending;
            m_tempSymbols.Sort();
            int tempLen = 8; // assume first maplength = 8;
            int _previousAddress = 0;
            foreach (SymbolHelper shtest in m_tempSymbols)
            {
                SymbolHelper sh = new SymbolHelper();
                sh.Varname = shtest.Flash_start_address.ToString("X4");
                if (_previousAddress != 0)
                {
                    tempLen = sh.Flash_start_address - _previousAddress;
                }
                else
                {
                    _previousAddress = sh.Flash_start_address;
                }
                sh.Flash_start_address = shtest.Flash_start_address;
                sh.Length = tempLen;
                sh.Symbol_number = symbolnumber++;
                symbols.Add(sh);
            }
            int _mapLengthDiff = 0;
            bool _issueMapFound = false;
            AxisHelper _lastRPMSupportPointAxis = new AxisHelper();
            foreach (AxisHelper ah in axis)
            {
                if (address != 0)
                {
                    // is there a gap?
                    //Console.WriteLine("Handling axis: " + ah.Addressinfile.ToString("X4"));
                    int endofpreviousaxis = address + length + 2;

                    if (endofpreviousaxis < ah.Addressinfile)
                    {
                        int gaplen = ah.Addressinfile - endofpreviousaxis;

                        // check whether there are symbol address in between
                        bool _symbolfound = false;
                        foreach (SymbolHelper shtemp in symbols)
                        {
                            if (shtemp.Flash_start_address >= endofpreviousaxis && shtemp.Flash_start_address + shtemp.Length <= endofpreviousaxis + gaplen)
                            {
                                _symbolfound = true;
                                //Console.WriteLine("GAP OVERRULED: " + endofpreviousaxis.ToString("X4") + " - " + ah.Addressinfile.ToString("X4") + " length: " + gaplen.ToString() + " by " + shtemp.Flash_start_address.ToString("X4") + " " + shtemp.Length.ToString("X4"));
                                // add the axis as a symbol in that case 
                                SymbolHelper shnew = new SymbolHelper();
                                shnew.Flash_start_address = ah.Addressinfile;
                                shnew.Length = 8;// gaplen;// ah.Length;
                                shnew.Varname = /*"NEW: "  +*/ shnew.Flash_start_address.ToString("X4");
                                symbols.Add(shnew);
                                if (gaplen > shnew.Length)
                                {
                                    Console.WriteLine("might be more symbols after " + shtemp.Flash_start_address.ToString("X4"));
                                }
                                break;
                            }
                        }


                        //Console.WriteLine("AXIS: " + endofpreviousaxis.ToString("X4") + " - " + ah.Addressinfile.ToString("X4") + " length: " + gaplen.ToString());
                        /*if (endofpreviousaxis == 0xFCC5)
                        {
                            Console.WriteLine("PREV AXIS ADDRESS: "+ previousAxisHelper.Addressinfile.ToString("X4"));
                            Console.WriteLine("GAP: " + endofpreviousaxis.ToString("X4") + " - " + ah.Addressinfile.ToString("X4") + " length: " + gaplen.ToString());
                        }*/
                        Console.WriteLine("GAP: " + endofpreviousaxis.ToString("X4") + " - " + ah.Addressinfile.ToString("X4") + " length: " + gaplen.ToString());
                        SymbolHelper sh = new SymbolHelper();
                        sh.Varname = endofpreviousaxis.ToString("X4");
                        sh.Length = gaplen;
                        if (_mapLengthDiff > 0)
                        {
                            sh.Length = _mapLengthDiff;
                            _mapLengthDiff = 0;
                        }
                        sh.Symbol_number = symbolnumber++;

                        sh.Flash_start_address = endofpreviousaxis;
                        /*if (sh.Length == 256)
                        {
                            sh.Cols = 16;
                            sh.Rows = 16;
                            if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC18)
                            {
                                // there are 4 maps with this size in Motronic 4.3
                                // the one that had lots of 127 values in it is the VE map
                                // and has a correction factor of 1/127 (lambda = 1)
                                // the others are ignition maps and have a correction factor of 0.75 ??
                                if (FileTools.Instance.MapContainsMostly(filename, sh, 127, 4, 10))
                                {
                                    sh.Varname = "VE map";
                                    sh.Category = "Fuel";
                                }
                                else
                                {
                                    sh.Varname = "Ignition map";
                                    sh.Category = "Ignition";
                                }


                            }
                        }

                        else if (sh.Length == 144)
                        {
                            sh.Cols = 12;
                            sh.Rows = 12;
                            sh.Varname = "Warmup fuel correction";
                            //sh.Category = "Examining";
                            sh.Category = "Fuel";
                        }
                        else if (sh.Length == 128)
                        {
                            sh.Cols = 16;
                            sh.Rows = 8;
                            if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC18)
                            {
                                sh.Varname = "Boost map";
                                sh.Category = "Boost";
                            }
                        }

                        else if (sh.Length == 84)
                        {
                            sh.Cols = 7;
                            sh.Rows = 12;
                            if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC18)
                            {
                                sh.Varname = "Dwell angle characteristic map";
                                //sh.Category = "Examining";
                                sh.Category = "Ignition";
                            }
                        }
                        else if (sh.Length == 80)
                        {
                            sh.Cols = 5;
                            sh.Rows = 16;
                            if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC18)
                            {
                                sh.Varname = "Cylinder compensation";
                                sh.Category = "Correction";
                            }
                        }
                        else if (sh.Length == 70)
                        {
                            sh.Cols = 7;
                            sh.Rows = 10;
                            if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC18)
                            {
                                sh.Varname = "Cranking fuel enrichment";
                                sh.Category = "Cranking";
                            }
                        }
                        else if (sh.Length == 64)
                        {
                            sh.Cols = 8;
                            sh.Rows = 8;
                            // there's one map with this length that has 0x30 ID for axis and this is the MAF to Load conversion map
                            SymbolHelper shxaxis = Helpers.Instance.GetXaxisSymbol(filename, symbols, axis, sh.Varname, sh.Flash_start_address);
                            SymbolHelper shyaxis = Helpers.Instance.GetYAxisSymbol(filename, symbols, axis, sh.Varname, sh.Flash_start_address);
                            if (FileTools.Instance.readdatafromfile(filename, shxaxis.X_axis_address, 1)[0] == 0x30)
                            {
                                if (FileTools.Instance.FirstColumnForTableAveragesLessThan(filename, sh, 50, 8))
                                {
                                    sh.Varname = "MAF to Load conversion map";
                                    sh.Category = "MAF";
                                }
                            }
                            //<GS-28022011> this needs more work.. some bins don't have overboost
                            //these bins have no boost map either and have a different amount of 64 byte length maps

                            else if (FileTools.Instance.readdatafromfile(filename, shxaxis.X_axis_address, 1)[0] == 0x3B )
                            {
                                sh.Varname = "Overboost map";
                                sh.Category = "Boost";
                            }
                            else if (FileTools.Instance.readdatafromfile(filename, shxaxis.X_axis_address, 1)[0] == 0x40 && FileTools.Instance.readdatafromfile(filename, shyaxis.Y_axis_address, 1)[0] == 0x3B)
                            {
                                if (!FileTools.Instance.MapContainsMostly(filename, sh, 0, 0, 90) && FileTools.Instance.FirstColumnForTableAveragesLessThan(filename, sh, 50, 8))
                                {
                                    sh.Varname = "Acceleration correction";
                                    sh.Category = "Fuel";
                                }
                            }
                        }
                        else if (sh.Length == 50)
                        {
                            sh.Cols = 10;
                            sh.Rows = 5;

                        }
                        else if (sh.Length == 48)
                        {
                            sh.Cols = 8;
                            sh.Rows = 6;
                        }
                        else if (sh.Length == 42)
                        {
                            sh.Cols = 6;
                            sh.Rows = 7;
                        }
                        else if (sh.Length == 40)
                        {
                            sh.Cols = 8;
                            sh.Rows = 5;
                            if (FileTools.Instance.CurrentFiletype == FileType.MOTRONIC18)
                            {
                                sh.Varname = "Idle fuel map";
                                sh.Category = "Fuel";
                            }
                        }
                        else if (sh.Length == 24)
                        {
                            sh.Cols = sh.Length;
                            sh.Rows = 0;

                            //SymbolHelper tsh = GetYAxisSymbol(filename, symbols, axis, sh.Varname, sh.Flash_start_address);
                            float[] xaxis;
                            float[] yaxis;
                            string xdescr;
                            string ydescr;
                            Helpers.Instance.GetAxisValues(filename, symbols, axis, sh.Varname, sh.Flash_start_address, sh.Rows, sh.Cols, out xaxis, out yaxis, out xdescr, out ydescr);
                            //Console.WriteLine(sh.Varname + " " + xaxis.Length.ToString() + " " + yaxis.Length.ToString());
                            if (sh.Length != xaxis.Length * yaxis.Length)
                            {
                                // in that case we're at the point where maps are not longer in sync with axis
                                Console.WriteLine("Offset found for sync problem: " + sh.Flash_start_address.ToString("X6"));
                                _mapLengthDiff = sh.Length - (xaxis.Length);
                                _issueMapFound = true;
                                _symbolfound = false;
                            }
                        }
                        else if (sh.Length == 16)
                        {
                            sh.Cols = sh.Length;
                            sh.Rows = 1;

                            //SymbolHelper tsh = GetYAxisSymbol(filename, symbols, axis, sh.Varname, sh.Flash_start_address);
                            float[] xaxis;
                            float[] yaxis;
                            string xdescr;
                            string ydescr;
                            Helpers.Instance.GetAxisValues(filename, symbols, axis, sh.Varname, sh.Flash_start_address, sh.Rows, sh.Cols, out xaxis, out yaxis, out xdescr, out ydescr);
                            //Console.WriteLine(sh.Varname + " " + xaxis.Length.ToString() + " " + yaxis.Length.ToString());

                            if (FileTools.Instance.MapContainsMostly(filename, sh, 127, 20, 50) && xaxis.Length == 16)
                            {
                                sh.Varname = "WOT enrichment";
                                sh.Category = "Fuel";
                            }
                            else if (xaxis.Length == 16 && FileTools.Instance.MapContainsMostly(filename, sh, 55, 4, 30))
                            {
                                sh.Varname = "WOT ignition";
                                sh.Category = "Examining";
                            }
                        }
                        else if (sh.Length == 8)
                        {
                            if (_issueMapFound)
                            {
                                sh.Cols = sh.Length;
                                sh.Rows = 1;
                                // one of these maps should be the MAF limit map... which one though
                            }
                        }
                        else
                        {
                            SymbolHelper shxaxis = Helpers.Instance.GetXaxisSymbol(filename, symbols, axis, sh.Varname, sh.Flash_start_address);
                            if (FileTools.Instance.readdatafromfile(filename, shxaxis.X_axis_address, 1)[0] == 0x38 && shxaxis.X_axis_length == sh.Length)
                            {
                                sh.Category = "Temperature compensation";
                            }
                            else
                            {
                                //Console.WriteLine(sh.Varname + " len: " + sh.Length.ToString("X2") + " axis len: " + shxaxis.X_axis_length.ToString("X2"));
                            }
                            sh.Cols = sh.Length;
                            sh.Rows = 1;
                        }*/
                        if (!_symbolfound)
                        {
                            symbols.Add(sh);
                        }

                    }
                }
                length = ah.Length;
                address = ah.Addressinfile;
                previousAxisHelper = ah;
            }
            // try to determine ignition maps probablility
           /* SymbolCollection ignition_maps = new SymbolCollection();
            foreach (SymbolHelper sh in symbols)
            {
                if (sh.Varname == "Ignition map")
                {
                    sh.Average_value = DetermineAverageMapValue(filename, sh);
                    ignition_maps.Add(sh);
                }
            }
            //ignition_maps.SortColumn = "Average_value";
            ignition_maps.SortColumn = "Flash_start_address";
            //ignition_maps.SortingOrder = GenericComparer.SortOrder.Descending;
            ignition_maps.SortingOrder = GenericComparer.SortOrder.Ascending;
            ignition_maps.Sort();
            if (ignition_maps.Count == 3)
            {
                ignition_maps[0].Varname = "Ignition map: wide open throttle";
                ignition_maps[1].Varname = "Ignition map: part throttle";
                ignition_maps[2].Varname = "Ignition map: knock/limp home";

               

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
            }

            SymbolCollection cylinderCompensationMaps = new SymbolCollection();
            foreach (SymbolHelper sh in symbols)
            {
                if (sh.Varname == "Cylinder compensation")
                {
                    cylinderCompensationMaps.Add(sh);
                }
            }
            cylinderCompensationMaps.SortColumn = "Flash_start_address";
            cylinderCompensationMaps.SortingOrder = GenericComparer.SortOrder.Ascending;
            cylinderCompensationMaps.Sort();

            if (cylinderCompensationMaps.Count == 5)
            {
                cylinderCompensationMaps[0].Varname = "Cylinder compensation #1";
                cylinderCompensationMaps[1].Varname = "Cylinder compensation #2";
                cylinderCompensationMaps[2].Varname = "Cylinder compensation #3";
                cylinderCompensationMaps[3].Varname = "Cylinder compensation #4";
                cylinderCompensationMaps[4].Varname = "Cylinder compensation #5";
            }
            foreach (SymbolHelper sh in cylinderCompensationMaps)
            {
                foreach (SymbolHelper shorg in symbols)
                {
                    if (sh.Flash_start_address == shorg.Flash_start_address)
                    {
                        shorg.Varname = sh.Varname;
                        break;
                    }
                }
            }

            int _8lengthmapcount = 0;
            foreach (SymbolHelper sh in symbols)
            {
                if (sh.Length == 8) _8lengthmapcount++;
            }
            int idx = 7;
            if (_8lengthmapcount < 25) idx = 5;
            //else idx = 7;
            if (_8lengthmapcount > 10)
            {
                symbols.SortColumn = "Flash_start_address";
                symbols.SortingOrder = GenericComparer.SortOrder.Descending;
                symbols.Sort();
                int tidx = 0;
                foreach (SymbolHelper sh in symbols)
                {
                    if (sh.Length == 8) tidx++;
                    if (tidx == idx)
                    {
                        sh.Varname = "MAF limit";
                        sh.Category = "MAF";
                    }
                    if (tidx == idx + 3)
                    {
                        sh.Varname = "Internal load limiter";
                        sh.Category = "Examining";
                    }
                }
            }
            LoadMotronic18Limiters();*/

            //TestSymbolListIntegrety(symbols, axis);


            SetProgressPercentage("Sorting data", 90);
            // sort the symbol on length, biggest on top
            symbols.SortColumn = "Length";
            symbols.SortingOrder = GenericComparer.SortOrder.Descending;
            symbols.Sort();
        }

        private void SetProgressPercentage(string info, int percentage)
        {
            if (onDecodeProgress != null)
            {
                onDecodeProgress(this, new DecodeProgressEventArgs(percentage, info));
            }
        }

        private void LoadMotronic18Limiters()
        {
            //TODO: barLimiterInfo.Caption = "";
            int rpmpointer = LookupMotronic18RpmPointer();
            int speedpointer = LookupMotronic18SpeedPointer();
            Console.WriteLine("speed pointer = " + speedpointer.ToString("X4") + " rpm pointer = " + rpmpointer.ToString("X4"));
            if (rpmpointer > 0 && speedpointer > 0)
            {
                byte[] rpm_limiter = FileTools.Instance.readdatafromfile(FileTools.Instance.Currentfile, rpmpointer, 2, false);
                byte[] rpm_limiter2 = FileTools.Instance.readdatafromfile(FileTools.Instance.Currentfile, rpmpointer + 3, 1, false);
                FileTools.Instance.Rpmlimit2 = Convert.ToInt32(rpm_limiter2[0]);
                FileTools.Instance.Rpmlimit2 *= 40;
                Console.WriteLine("RPM limiter 2: " + FileTools.Instance.Rpmlimit2.ToString());
                byte[] speed_limiter = FileTools.Instance.readdatafromfile(FileTools.Instance.Currentfile, speedpointer, 1, false);


                // index 119 = speed limiter
                // index 109 = rpm limiter
                FileTools.Instance.Rpmlimit = Convert.ToInt32(rpm_limiter[0]);
                int temp = Convert.ToInt32(Math.Pow(2, Convert.ToDouble(rpm_limiter[1])));
                FileTools.Instance.Rpmlimit *= temp;
                FileTools.Instance.Rpmlimit = 9650000 / FileTools.Instance.Rpmlimit;
                //Console.WriteLine("RPM limiter: " + rpmlimit.ToString() + " rpm");
                FileTools.Instance.Speedlimit = Convert.ToInt32(speed_limiter[0]);
                //MessageBox.Show("Speed limit: " + speedlimit.ToString() + Environment.NewLine + "Rpm limit: " + rpmlimit.ToString());
                //TODO: barLimiterInfo.Caption = "Speed limit: " + FileTools.Instance.Speedlimit.ToString() + " rpm limit: " + FileTools.Instance.Rpmlimit.ToString() + " rpm limit2: " + FileTools.Instance.Rpmlimit2.ToString();
            }
        }

        private int LookupMotronic18RpmPointer()
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

        private int LookupMotronic18SpeedPointer()
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
        }

        private float DetermineAverageMapValue(string filename, SymbolHelper sh)
        {
            float retval = 0;
            byte[] data = FileTools.Instance.readdatafromfile(filename, sh.Flash_start_address, sh.Length, false);
            if (data != null)
            {
                foreach (byte b in data)
                {
                    retval += (float)b;
                }
            }
            retval /= sh.Length;
            return retval;
        }

        private string m_currentFile = string.Empty;

        private FileInformation m_fileInfo = new FileInformation();
        private TransactionLog m_transactionLog = null;

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
                else if (fi.Length == 0x8000) return FileType.MOTRONIC18;
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
            else if (symbolname.StartsWith("Ignition map")) retval = -0.375; //( 360º / #TEETH) / 4 = ( 360º /120) / 4 =0.75º 
            else if (symbolname.StartsWith("WOT ignition")) retval = -0.375; //( 360º / #TEETH) / 4 = ( 360º /120) / 4 =0.75º 
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

        public override FileInformation ParseFile()
        {
            m_fileInfo = new FileInformation();
            SymbolCollection symbols = new SymbolCollection();
            AxisCollection axis = new AxisCollection();
            LoadMotronic18File(m_currentFile, out symbols, out axis);
            m_symbols = symbols;
            m_axis = axis;
            m_fileInfo.Symbols = symbols;
            m_fileInfo.Axis = axis;
            return m_fileInfo;
        }

        private void DecodeM18FileInformation(string filename, out string hardwareID, out string softwareID, out string partnumber, out string damosinfo)
        {
            hardwareID = string.Empty;
            softwareID = string.Empty;
            partnumber = string.Empty;
            damosinfo = string.Empty;

            try
            {
                byte[] filebytes = File.ReadAllBytes(filename);
                // search file for indicator ZSMFC
                for (int i = 0; i < filebytes.Length; i++)
                {
                    if (i < filebytes.Length - 8)
                    {
                        if (filebytes[i] == 'Z' && filebytes[i + 1] == 'S' && filebytes[i + 2] == 'M' && filebytes[i + 3] == 'F' && filebytes[i + 4] == 'C')
                        {
                            // found.. now parse info 40 bytes further
                            for (int j = 0; j < 10; j++) hardwareID += Convert.ToChar(filebytes[i + 40 + j]);
                            for (int j = 0; j < 10; j++) softwareID += Convert.ToChar(filebytes[i + 50 + j]);
                            for (int j = 0; j < 7; j++) partnumber += Convert.ToChar(filebytes[i + 60 + j]);
                            Console.WriteLine("hwid: " + hardwareID);
                            Console.WriteLine("swid: " + softwareID);
                            Console.WriteLine("part: " + partnumber);
                            break;
                        }
                    }
                }

                // find damos info M4.3 indicator
                for (int i = 0; i < filebytes.Length; i++)
                {
                    if (i < filebytes.Length - 8)
                    {
                        if (filebytes[i] == 'M' && filebytes[i + 1] == '4' && filebytes[i + 2] == '.' && filebytes[i + 3] == '3')
                        {
                            for (int j = 0; j < 44; j++) damosinfo += Convert.ToChar(filebytes[i - 5 + j]);
                            break;
                        }
                    }
                }

            }
            catch (Exception E)
            {
                Console.WriteLine("DecodeM18FileInformation: " + E.Message);
            }

        }

        public override string GetPartnumber()
        {
            string hwid = string.Empty;
            string swid = string.Empty;
            string partnumber = string.Empty;
            string damos = string.Empty;

            DecodeM18FileInformation(m_currentFile, out hwid, out swid, out partnumber, out damos);
            return partnumber;
        }

        public override string GetHardwareID()
        {
            string hwid = string.Empty;
            string swid = string.Empty;
            string partnumber = string.Empty;
            string damos = string.Empty;

            DecodeM18FileInformation(m_currentFile, out hwid, out swid, out partnumber, out damos);
            return hwid;
        }

        public override string GetDamosInfo()
        {
            string hwid = string.Empty;
            string swid = string.Empty;
            string partnumber = string.Empty;
            string damos = string.Empty;
            DecodeM18FileInformation(m_currentFile, out hwid, out swid, out partnumber, out damos);
            return damos;
        }

        public override string GetSoftwareVersion()
        {
            string hwid = string.Empty;
            string swid = string.Empty;
            string partnumber = string.Empty;
            string damos = string.Empty;
            DecodeM18FileInformation(m_currentFile, out hwid, out swid, out partnumber, out damos);
            return swid;
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
            /* TODO: Implement M1.8 Checksum */
            uint crc = CalculateM18CRC(FileTools.Instance.Currentfile);
            byte[] crcbytes = FileTools.Instance.readdatafromfile(FileTools.Instance.Currentfile, 0x7F00, 2, false);
            int readcrc = (Convert.ToInt32(crcbytes.GetValue(0)) * 256) + Convert.ToInt32(crcbytes.GetValue(1));
            if (crc == readcrc) retval = true;
            return retval;
        }

        public override void UpdateChecksum()
        {
            /* TODO: Implement M1.8 Checksum
            FileStream fs = new FileStream(m_currentFile, FileMode.Open);
            fs.Position = 0;
            int volvocrc = 0;
            using (BinaryReader br = new BinaryReader(fs))
            {
                while (fs.Position < 0x7F00)
                {
                    volvocrc += (int)br.ReadByte();
                }
            }
            fs.Close();
            fs.Dispose();
            // CRC is stored @ 0x7F00 and 0x7F01
            FileStream fsi1 = File.OpenWrite(m_currentFile);
            using (BinaryWriter bw = new BinaryWriter(fsi1))
            {
                fsi1.Position = 0x7F00;
                byte b1 = (byte)((volvocrc & 0x007F00) / 256);
                byte b2 = (byte)(volvocrc & 0x0000FF);
                bw.Write(b1);
                bw.Write(b2);
            }
            fsi1.Close();
            fsi1.Dispose();*/
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
            else if (mapname.StartsWith("Ignition map")) retval = 60F; // or - 30 ?
            else if (mapname.StartsWith("WOT ignition")) retval = 60F; // or - 30 ?
            else if (mapname == "Injection angle on start of injection") retval = 574;
            //0.75 and offset -22,5 
            return retval;
        }

        private uint CalculateM18CRC(string filename)
        {
            FileStream fs = new FileStream(filename, FileMode.Open);
            fs.Position = 0;
            Int64 volvocrc2 = 0;
            uint volvocrc1 = 0;
            /* TODO: Implement M1.8 checksum */

            using (BinaryReader br = new BinaryReader(fs))
            {
                while (fs.Position < 0x7F00)
                {
                    byte b = br.ReadByte();
                    volvocrc1 += (uint)b;
                    volvocrc2 += (uint)b;
                }
            }
            fs.Close();
            fs.Dispose();
            volvocrc1 &= 0x00FFFF;
            //Console.WriteLine("Checksum: " + volvocrc1.ToString("X4"));
            //Console.WriteLine("Checksum64: " + volvocrc2.ToString("X8"));
            return volvocrc1;
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
