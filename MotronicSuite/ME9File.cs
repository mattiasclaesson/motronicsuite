using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MotronicTools;

namespace MotronicSuite
{
    public class ME9File : IECUFile
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

        private int findSequence(byte[] fileData, int offset, byte[] sequence, byte[]mask)
        {
           
            byte data;
            int i, max;
            i = 0;
            max = 0;
            int position = offset;
            while (position < fileData.Length)
            {
                data = (byte)fileData[position++];
                if (data == sequence[i] || mask[i] == 0)
                {
                    i++;
                    

                }
                else
                {
                    if (i > max) max = i;
                    position -= i ;
                    i = 0;
                }
                if (i == sequence.Length) break;
            }
            if (i == sequence.Length)
            {
                return ((int)position - sequence.Length);
            }
            else
            {
                return -1;
            }
        }

        private void LoadME7File(string filename, out SymbolCollection symbols, out AxisCollection axis)
        {
            // this should detect the 4 different file types for ME7 (ME7.0 512Kb, ME7.0 1024Kb, ME7.0.1 version A and ME7.0.1 version B) 
            // and parse them with a different algorithm.


            symbols = new SymbolCollection();
            axis = new AxisCollection();
            SetProgressPercentage("Analyzing structure", 30);
            
            byte[] allBytes = File.ReadAllBytes(filename);

            if (findSequence(allBytes, 0, new byte[7] { 0x4d, 0x45, 0x37, 0x5F, 0x35, 0x30, 0x30 }, new byte[7] { 1, 1, 1, 1, 1, 1, 1 }) > 0)
            {
                ParseME701File(filename, out symbols, out axis);
            }
            else
            {
                ParseME700File(filename, out symbols, out axis);
            }
            
            SetProgressPercentage("Sorting data", 90);
            // sort the symbol on length, biggest on top
            symbols.SortColumn = "Length";
            symbols.SortingOrder = GenericComparer.SortOrder.Descending;
            symbols.Sort();

           

        }

        private void ParseME701File(string filename, out SymbolCollection symbols, out AxisCollection axis)
        {
            symbols = new SymbolCollection();
            axis = new AxisCollection();
            byte[] allBytes = File.ReadAllBytes(filename);
            int state = 0;
            // parse the file and look for 0xE6 0xFC ?? ?? 0xF2 0xFD indicators for starters
            for (int t = 0; t < allBytes.Length; t++)
            {
                switch (state)
                {
                    case 0:
                        if (allBytes[t] == 0xE6) state++;
                        break;
                    case 1:
                        if (allBytes[t] == 0xFC) state++;
                        break;
                    case 2:
                        //Console.WriteLine(allBytes[t + 2].ToString("X2") + " " + allBytes[t + 3].ToString("X2") + " " + allBytes[t + 1].ToString("X2") + allBytes[t].ToString("X2"));
                        if (allBytes[t + 2] == 0xF2 && allBytes[t + 3] == 0xFD)
                        {
                            SymbolHelper sh = new SymbolHelper();
                            sh.IsSixteenbits = true; // allways for ME7
                            sh.Varname = t.ToString("X8");
                            int address = Convert.ToInt32(allBytes[t + 1]) * 256;
                            address += Convert.ToInt32(allBytes[t]);
                            address += 0x10000;

                            if (address == 0x1196A)
                            {
                                Console.WriteLine("Found 1196A");
                            }
                            int ylength = (allBytes[address] + Convert.ToInt32(allBytes[address + 1]) * 256);
                            int xlength = (allBytes[address + 2] + Convert.ToInt32(allBytes[address + 3]) * 256);
                            sh.Length = ylength * xlength * 2;
                            if (sh.Length > 0 && sh.Length <= 0x200 && sh.Length != 0x90) // 0x90 length is not a map
                            {
                                sh.Y_axis_address = address/*sh.Flash_start_address*/ + 4;
                                sh.X_axis_address = address/*sh.Flash_start_address*/ + 4 + (2 * ylength);

                                sh.X_axis_length = xlength;
                                sh.Y_axis_length = ylength * 2;
                                sh.Flash_start_address = address + 4 + ylength * 2 + xlength * 2;
                                if (!CollectionContainsAddress(symbols, sh.Flash_start_address))
                                {
                                    symbols.Add(sh);
                                    /*if (sh.Flash_start_address == 0x1196A)
                                    {
                                        Console.WriteLine("Found 1196A");
                                    }*/
                                }
                                Console.WriteLine("Address: " + address.ToString("X8") + " " + sh.X_axis_length.ToString("X8") + " " + sh.Y_axis_length.ToString("X8"));

                            }


                        }
                        else if (allBytes[t + 2] == 0xE6 && allBytes[t + 3] == 0xFD)
                        {
                            SymbolHelper sh = new SymbolHelper();
                            sh.IsSixteenbits = true; // allways for ME7
                            sh.Varname = t.ToString("X8");
                            int address = Convert.ToInt32(allBytes[t + 1]) * 256;
                            address += Convert.ToInt32(allBytes[t]);
                            address += 0x10000;
                            //Console.WriteLine("Addition map: " + address.ToString("X8"));
                            if (address == 0x12AA2)
                            {
                                Console.WriteLine("Found 12AA2");
                            }
                            int ylength = (allBytes[address] + Convert.ToInt32(allBytes[address + 1]) * 256);
                            int xlength = (allBytes[address + 2] + Convert.ToInt32(allBytes[address + 3]) * 256);
                            sh.Length = ylength * xlength * 2;
                            if (sh.Length > 0 && sh.Length <= 0x200 && sh.Length != 0x90) // 0x90 length is not a map
                            {
                                sh.Y_axis_address = address/*sh.Flash_start_address*/ + 4;
                                sh.X_axis_address = address/*sh.Flash_start_address*/ + 4 + (2 * ylength);

                                sh.X_axis_length = xlength;
                                sh.Y_axis_length = ylength * 2;
                                sh.Flash_start_address = address + 4 + ylength * 2 + xlength * 2;
                                if (!CollectionContainsAddress(symbols, sh.Flash_start_address))
                                {
                                    symbols.Add(sh);
                                    /*if (sh.Flash_start_address == 0x1196A)
                                    {
                                        Console.WriteLine("Found 1196A");
                                    }*/
                                }
                                Console.WriteLine("Address: " + address.ToString("X8") + " " + sh.X_axis_length.ToString("X8") + " " + sh.Y_axis_length.ToString("X8"));

                            }


                        }
                        state = 0;
                        break;
                }
            }

            

            int LambdaRequestMapAddress = GetLambdaRequestMap(allBytes);
            if (LambdaRequestMapAddress > 0)
            {
                SymbolHelper sh = new SymbolHelper();
                sh.IsSixteenbits = false;
                // sh.UserDescription = "Lambda driver demand map";
                sh.Varname = "Fuel.Lambda driver demand map [LAMFA]";

                sh.X_axis_length = 6;
                sh.Y_axis_length = 15;
                sh.Length = 15 * 6;
                sh.X_axis_address = LambdaRequestMapAddress + 2;
                sh.Y_axis_address = LambdaRequestMapAddress + 17;
                sh.Flash_start_address = LambdaRequestMapAddress + 23 + 7;
                symbols.Add(sh);
            }

            int KFMIOPAddress = GetKFMIOPMap(allBytes);
            if (KFMIOPAddress > 0)
            {
                SymbolHelper sh = new SymbolHelper();
                sh.IsSixteenbits = true;
                sh.Varname = "Torque.Optimum engine torque map [KFMIOP]";
                sh.MapAllowsNegatives = false;
                sh.X_axis_length = 11;
                sh.Y_axis_length = 16 * 2;
                sh.Length = 11 * 16;
                sh.X_axis_address = KFMIOPAddress;
                sh.Y_axis_address = KFMIOPAddress;
                sh.Flash_start_address = KFMIOPAddress;
                symbols.Add(sh);
            }

            int LDRXNAddress = GetLDRXNMap(allBytes);
            if (LDRXNAddress > 0)
            {
                LDRXNAddress += 4;
                SymbolHelper sh = new SymbolHelper();
                sh.IsSixteenbits = true;
                sh.Varname = "Boost.Maximum load boost control [LDRXN]";
                sh.MapAllowsNegatives = false;
                sh.X_axis_length = 16;
                sh.Y_axis_length = 1 * 2;
                sh.Length = 16;
                sh.X_axis_address = LDRXNAddress;
                sh.Y_axis_address = LDRXNAddress;
                sh.Flash_start_address = LDRXNAddress;
                symbols.Add(sh);
            }

            // Correct from here!! 

            int IgnitionMapAddress = GetIgnitionMapAddress(allBytes);
            Console.WriteLine("Ignition map: " + IgnitionMapAddress.ToString("X16"));
            if (IgnitionMapAddress <= 0) IgnitionMapAddress = 0x12AA2; // TEST
            if (IgnitionMapAddress > 0)
            {
                SymbolHelper sh = new SymbolHelper();
                sh.IsSixteenbits = false;
                //sh.UserDescription = "Ignition map";
                sh.Varname = "Ignition.Ignition map [KFZW]";

                //sh.Description = "Main ignition map";
                sh.X_axis_length = 11;
                sh.Y_axis_length = 16;
                sh.Length = 0xB0;
                sh.X_axis_address = IgnitionMapAddress;
                sh.Y_axis_address = IgnitionMapAddress;
                sh.Flash_start_address = IgnitionMapAddress;
                symbols.Add(sh);
                sh = new SymbolHelper();
                sh.IsSixteenbits = false;
                //sh.UserDescription = "Secondary ignition map";
                sh.Varname = "Ignition.Optimal ignition map [KFZWOP]";
                sh.X_axis_length = 11;
                sh.Y_axis_length = 16;
                sh.Length = 0xB0;
                sh.X_axis_address = IgnitionMapAddress + 0xB0;
                sh.Y_axis_address = IgnitionMapAddress + 0xB0;
                sh.Flash_start_address = IgnitionMapAddress + 0xB0;
                symbols.Add(sh);

                sh = new SymbolHelper();
                sh.IsSixteenbits = false;
                //sh.UserDescription = "Secondary ignition map";
                sh.Varname = "Ignition.Optimal ignition map 2 [KFZWOP2]";
                sh.X_axis_length = 11;
                sh.Y_axis_length = 16;
                sh.Length = 0xB0;
                sh.X_axis_address = IgnitionMapAddress + (0xB0 * 2);
                sh.Y_axis_address = IgnitionMapAddress + (0xB0 * 2);
                sh.Flash_start_address = IgnitionMapAddress + (0xB0 * 2);
                symbols.Add(sh);
            }
            int VEMapAddress = GetVEMap(allBytes);  //KFKHFM
            Console.WriteLine("VE map: " + VEMapAddress.ToString("X16"));
            if (VEMapAddress > 0)
            {
                SymbolHelper sh = new SymbolHelper();
                sh.IsSixteenbits = false;
                sh.Varname = "Airmass.Air mass meter correction [KFKHFM]";
                sh.X_axis_length = 14;
                sh.Y_axis_length = 14;
                sh.Length = 14 * 14;
                sh.X_axis_address = VEMapAddress + 2;
                sh.Y_axis_address = VEMapAddress + 16;
                sh.Flash_start_address = VEMapAddress + 30;
                symbols.Add(sh);
            }

            int enrichmentMapAddress = GetEnrichmentMap(allBytes); //KFLBTS
            Console.WriteLine("enrichment map: " + enrichmentMapAddress.ToString("X16"));
            if (enrichmentMapAddress > 0)
            {
                SymbolHelper sh = new SymbolHelper();
                sh.IsSixteenbits = false;
                sh.Varname = "Fuel.Enrichment delta map [KFFDLBTS]";
                sh.X_axis_length = 16;
                sh.Y_axis_length = 12;
                sh.Length = 12 * 16;
                sh.X_axis_address = enrichmentMapAddress;
                sh.Y_axis_address = enrichmentMapAddress;
                sh.Flash_start_address = enrichmentMapAddress;
                symbols.Add(sh);

                sh = new SymbolHelper();
                sh.IsSixteenbits = false;
                sh.Varname = "Fuel.Enrichment map [KFLBTS]";
                sh.X_axis_length = 16;
                sh.Y_axis_length = 12;
                sh.Length = 12 * 16;
                sh.X_axis_address = enrichmentMapAddress + 0xC0;
                sh.Y_axis_address = enrichmentMapAddress + 0xC0;
                sh.Flash_start_address = enrichmentMapAddress + 0xC0;
                symbols.Add(sh);

            }

            int KFLDIMX_Address = GetKFLDIMXMap(allBytes);
            Console.WriteLine("KFLDIMX map: " + KFLDIMX_Address.ToString("X16"));
            if (KFLDIMX_Address > 0)
            {
                SymbolHelper sh = new SymbolHelper();
                sh.IsSixteenbits = true;
                sh.Varname = "Boost.Boost control map (I limit) [KFLDIMX]";
                sh.X_axis_length = 8;
                sh.Y_axis_length = 16 * 2;
                sh.Length = 8 * 16;
                if (allBytes.Length == 0x80000)
                {
                    // small files have axis
                    sh.X_axis_address = KFLDIMX_Address + 4;
                    sh.Y_axis_address = KFLDIMX_Address + 4 + 8 * 2;
                    sh.Flash_start_address = KFLDIMX_Address + 4 + 8 * 2 + 16 * 2;
                }
                else
                {
                    sh.X_axis_address = KFLDIMX_Address;
                    sh.Y_axis_address = KFLDIMX_Address;
                    sh.Flash_start_address = KFLDIMX_Address;
                    // symbols.Add(sh);
                    /*sh = new SymbolHelper();
                    sh.IsSixteenbits = true;
                    sh.Varname = "Boost.Map for linearisation boost pressure = f(TV) [KFLDRL]";
                    sh.X_axis_length = 10;
                    sh.Y_axis_length = 16 * 2;
                    sh.Length = 10 * 16;
                    sh.X_axis_address = LDRXN_Address + 0x100;
                    sh.Y_axis_address = LDRXN_Address + 0x100;
                    sh.Flash_start_address = LDRXN_Address + 0x100;*/
                    // works only for the file we have a2l for
                }

                symbols.Add(sh);
            }

            
            int KFLDHBNAddress = GetKFLDHBNMap(allBytes);
            Console.WriteLine("KFLDHBN map: " + KFLDHBNAddress.ToString("X16"));
            if (KFLDHBNAddress > 0)
            {
                SymbolHelper sh = new SymbolHelper();
                sh.IsSixteenbits = false;
                sh.Varname = "Boost.Boost control limitation compression ratio turbocharger [KFLDHBN]";
                sh.MapAllowsNegatives = false;
                sh.X_axis_length = 8;
                sh.Y_axis_length = 8;
                sh.Length = 8 * 8;
                sh.X_axis_address = KFLDHBNAddress;
                sh.Y_axis_address = KFLDHBNAddress;
                sh.Flash_start_address = KFLDHBNAddress;
                symbols.Add(sh);
            }
            symbols.SortColumn = "Flash_start_address";
            symbols.SortingOrder = GenericComparer.SortOrder.Ascending;
            symbols.Sort();
            int countx180 = 0;
            int countx70 = 0;
            foreach (SymbolHelper sh in symbols)
            {
                if (sh.Length == 0x200)
                {
                    sh.Varname = "Boost.Target load map [KFMIRL]";
                }
                else if (sh.Length == 0x180)
                {
                    if (countx180 == 0)
                    {
                        sh.Varname = "Throttle.Accelerator pedal map [KFPED]";
                        sh.MapAllowsNegatives = false;
                    }
                    else if (countx180 == 1) sh.Varname = "Boost.Target load map [KFMIRL]";
                    countx180++;
                }
                else if (sh.Length == 0x100)
                {
                    sh.Varname = "Throttle.Accelerator pedal map AUT reverse [KFPEDR]";
                }
                else if (sh.Length == 0xF0)
                {
                    sh.Varname = "Torque.Engine drag torque map [KFMDS]";
                }
                else if (sh.Length == 0x60)
                {
                    sh.Varname = "Cruise.Speed offset for cruise control [KFVOFFS]";
                }
                else if (sh.Length == 0xC0 && sh.X_axis_length == 6 && sh.Y_axis_length == 32)
                {
                    sh.Varname = "Airmass.Normalized airmass flow via the throttle [KFMSNWDK]";
                }
                else if (sh.Length == 0x70 && sh.X_axis_length == 8 && sh.Y_axis_length == 14)
                {
                    if(countx70 == 0) sh.Varname = "Lambda.Lambda delay time [KFLRST]";
                    else if (countx70 == 1) sh.Varname = "Lambda.Lambda delay time 2 [KFLRST2]";

                    countx70++;
                }
                if (sh.Category == "Undocumented" || sh.Category == "")
                {
                    if (sh.Varname.Contains("."))
                    {
                        try
                        {
                            sh.Category = sh.Varname.Substring(0, sh.Varname.IndexOf("."));
                        }
                        catch (Exception cE)
                        {
                            Console.WriteLine("Failed to assign category to symbol: " + sh.Varname + " err: " + cE.Message);
                        }
                    }

                }
            }
        }

        private void ParseME700File(string filename, out SymbolCollection symbols, out AxisCollection axis)
        {
            // parse the file and look for 0xE6 0xFC ?? ?? 0xF2 0xFD indicators for starters
            symbols = new SymbolCollection();
            axis = new AxisCollection();
            byte[] allBytes = File.ReadAllBytes(filename);
            int state = 0;
            for (int t = 0; t < allBytes.Length; t++)
            {
                switch (state)
                {
                    case 0:
                        if (allBytes[t] == 0xE6) state++;
                        break;
                    case 1:
                        if (allBytes[t] == 0xFC) state++;
                        break;
                    case 2:
                        //Console.WriteLine(allBytes[t + 2].ToString("X2") + " " + allBytes[t + 3].ToString("X2") + " " + allBytes[t + 1].ToString("X2") + allBytes[t].ToString("X2"));
                        if (allBytes[t + 2] == 0xF2 && allBytes[t + 3] == 0xFD)
                        {
                            SymbolHelper sh = new SymbolHelper();
                            sh.IsSixteenbits = true; // allways for ME7
                            sh.Varname = t.ToString("X8");
                            int address = Convert.ToInt32(allBytes[t + 1]) * 256;
                            address += Convert.ToInt32(allBytes[t]);
                            address += 0x10000;

                            if (address == 0x1196A)
                            {
                                Console.WriteLine("Found 1196A");
                            }
                            int ylength = (allBytes[address] + Convert.ToInt32(allBytes[address + 1]) * 256);
                            int xlength = (allBytes[address + 2] + Convert.ToInt32(allBytes[address + 3]) * 256);
                            sh.Length = ylength * xlength * 2;
                            if (sh.Length > 0 && sh.Length <= 0x200 && sh.Length != 0x90) // 0x90 length is not a map
                            {
                                sh.Y_axis_address = address/*sh.Flash_start_address*/ + 4;
                                sh.X_axis_address = address/*sh.Flash_start_address*/ + 4 + (2 * ylength);

                                sh.X_axis_length = xlength;
                                sh.Y_axis_length = ylength * 2;
                                sh.Flash_start_address = address + 4 + ylength * 2 + xlength * 2;
                                if (!CollectionContainsAddress(symbols, sh.Flash_start_address))
                                {
                                    symbols.Add(sh);
                                    /*if (sh.Flash_start_address == 0x1196A)
                                    {
                                        Console.WriteLine("Found 1196A");
                                    }*/
                                }
                                //Console.WriteLine("Address: " + address.ToString("X8"));

                            }


                        }
                        state = 0;
                        break;
                }
            }

            int IgnitionMapAddress = GetIgnitionMapAddress(allBytes);
            if (IgnitionMapAddress <= 0) IgnitionMapAddress = GetIgnitionMapAddress521KBFiles(allBytes);
            if (IgnitionMapAddress > 0)
            {
                SymbolHelper sh = new SymbolHelper();
                sh.IsSixteenbits = false;
                //sh.UserDescription = "Ignition map";
                sh.Varname = "Ignition.Ignition map [KFZW]";

                //sh.Description = "Main ignition map";
                sh.X_axis_length = 12;
                sh.Y_axis_length = 16;
                sh.Length = 0xC0;
                sh.X_axis_address = IgnitionMapAddress;
                sh.Y_axis_address = IgnitionMapAddress;
                sh.Flash_start_address = IgnitionMapAddress;
                symbols.Add(sh);
                sh = new SymbolHelper();
                sh.IsSixteenbits = false;
                //sh.UserDescription = "Secondary ignition map";
                sh.Varname = "Ignition.Secondary ignition map [KFZW2]";
                sh.X_axis_length = 12;
                sh.Y_axis_length = 16;
                sh.Length = 0xC0;
                sh.X_axis_address = IgnitionMapAddress + 0xC0;
                sh.Y_axis_address = IgnitionMapAddress + 0xC0;
                sh.Flash_start_address = IgnitionMapAddress + 0xC0;
                symbols.Add(sh);
            }

            int LambdaRequestMapAddress = GetLambdaRequestMap(allBytes);

            if (LambdaRequestMapAddress > 0)
            {
                SymbolHelper sh = new SymbolHelper();
                sh.IsSixteenbits = false;
                // sh.UserDescription = "Lambda driver demand map";
                sh.Varname = "Fuel.Lambda driver demand map [LAMFA]";

                sh.X_axis_length = 6;
                sh.Y_axis_length = 15;
                sh.Length = 15 * 6;
                sh.X_axis_address = LambdaRequestMapAddress + 2;
                sh.Y_axis_address = LambdaRequestMapAddress + 17;
                sh.Flash_start_address = LambdaRequestMapAddress + 23 +7;
                symbols.Add(sh);
            }
            else
            {
                LambdaRequestMapAddress = GetLambdaRequestMap512KBFiles(allBytes);
                if (LambdaRequestMapAddress > 0)
                {
                    SymbolHelper sh = new SymbolHelper();
                    sh.IsSixteenbits = false;
                    // sh.UserDescription = "Lambda driver demand map";
                    sh.Varname = "Fuel.Lambda driver demand map [LAMFA]";
                    sh.X_axis_length = 6;
                    sh.Y_axis_length = 10;
                    sh.Length = 10 * 6;
                    sh.X_axis_address = LambdaRequestMapAddress + 2;
                    sh.Y_axis_address = LambdaRequestMapAddress + 12;
                    sh.Flash_start_address = LambdaRequestMapAddress +18 + 6;

                    /*sh.X_axis_length = 10;
                    sh.Y_axis_length = 6;
                    sh.Length = 10 * 6;
                    sh.X_axis_address = LambdaRequestMapAddress + 2;
                    sh.Y_axis_address = LambdaRequestMapAddress + 12;
                    sh.Flash_start_address = LambdaRequestMapAddress + 18;*/
                    symbols.Add(sh);
                }
            }

            int VEMapAddress = GetVEMap(allBytes);  //KFKHFM
            if (VEMapAddress > 0)
            {
                SymbolHelper sh = new SymbolHelper();
                sh.IsSixteenbits = false;
                sh.Varname = "Airmass.Air mass meter correction [KFKHFM]";
                sh.X_axis_length = 14;
                sh.Y_axis_length = 14;
                sh.Length = 14 * 14;
                sh.X_axis_address = VEMapAddress + 2;
                sh.Y_axis_address = VEMapAddress + 16;
                sh.Flash_start_address = VEMapAddress + 30;
                symbols.Add(sh);
            }

            int enrichmentMapAddress = GetEnrichmentMap(allBytes); //KFLBTS
            if (enrichmentMapAddress > 0)
            {
                SymbolHelper sh = new SymbolHelper();
                sh.IsSixteenbits = false;
                sh.Varname = "Fuel.Enrichment delta map [KFFDLBTS]";
                sh.X_axis_length = 16;
                sh.Y_axis_length = 12;
                sh.Length = 12 * 16;
                sh.X_axis_address = enrichmentMapAddress;
                sh.Y_axis_address = enrichmentMapAddress;
                sh.Flash_start_address = enrichmentMapAddress;
                symbols.Add(sh);

                sh = new SymbolHelper();
                sh.IsSixteenbits = false;
                sh.Varname = "Fuel.Enrichment map [KFLBTS]";
                sh.X_axis_length = 16;
                sh.Y_axis_length = 12;
                sh.Length = 12 * 16;
                sh.X_axis_address = enrichmentMapAddress + 0xC0;
                sh.Y_axis_address = enrichmentMapAddress + 0xC0;
                sh.Flash_start_address = enrichmentMapAddress + 0xC0;
                symbols.Add(sh);

            }
            int KFLDIMX_Address = GetKFLDIMXMap(allBytes);
            if (KFLDIMX_Address > 0)
            {
                SymbolHelper sh = new SymbolHelper();
                sh.IsSixteenbits = true;
                sh.Varname = "Boost.Boost control map (I limit) [KFLDIMX]";
                sh.X_axis_length = 8;
                sh.Y_axis_length = 16 * 2;
                sh.Length = 8 * 16;
                if (allBytes.Length == 0x80000)
                {
                    // small files have axis
                    sh.X_axis_address = KFLDIMX_Address + 4;
                    sh.Y_axis_address = KFLDIMX_Address + 4 + 8 * 2;
                    sh.Flash_start_address = KFLDIMX_Address + 4 + 8 * 2 + 16 * 2;
                }
                else
                {
                    sh.X_axis_address = KFLDIMX_Address;
                    sh.Y_axis_address = KFLDIMX_Address;
                    sh.Flash_start_address = KFLDIMX_Address;
                    // symbols.Add(sh);
                    /*sh = new SymbolHelper();
                    sh.IsSixteenbits = true;
                    sh.Varname = "Boost.Map for linearisation boost pressure = f(TV) [KFLDRL]";
                    sh.X_axis_length = 10;
                    sh.Y_axis_length = 16 * 2;
                    sh.Length = 10 * 16;
                    sh.X_axis_address = LDRXN_Address + 0x100;
                    sh.Y_axis_address = LDRXN_Address + 0x100;
                    sh.Flash_start_address = LDRXN_Address + 0x100;*/
                    // works only for the file we have a2l for
                }

                symbols.Add(sh);
            }

            int KFMIOPAddress = GetKFMIOPMap(allBytes);
            if (KFMIOPAddress > 0)
            {
                SymbolHelper sh = new SymbolHelper();
                sh.IsSixteenbits = true;
                sh.Varname = "Torque.Optimum engine torque map [KFMIOP]";
                sh.MapAllowsNegatives = false;
                sh.X_axis_length = 11;
                sh.Y_axis_length = 16 * 2;
                sh.Length = 11 * 16;
                sh.X_axis_address = KFMIOPAddress;
                sh.Y_axis_address = KFMIOPAddress;
                sh.Flash_start_address = KFMIOPAddress;
                symbols.Add(sh);
            }

            int LDRXNAddress = GetLDRXNMap(allBytes);
            if (LDRXNAddress > 0)
            {
                SymbolHelper sh = new SymbolHelper();
                sh.IsSixteenbits = true;
                sh.Varname = "Boost.Maximum load boost control [LDRXN]";
                sh.MapAllowsNegatives = false;
                sh.X_axis_length = 16;
                sh.Y_axis_length = 1 * 2;
                sh.Length = 16;
                sh.X_axis_address = LDRXNAddress;
                sh.Y_axis_address = LDRXNAddress;
                sh.Flash_start_address = LDRXNAddress;
                symbols.Add(sh);
            }
            int KFLDHBNAddress = GetKFLDHBNMap(allBytes);
            if (KFLDHBNAddress > 0)
            {
                SymbolHelper sh = new SymbolHelper();
                sh.IsSixteenbits = false;
                sh.Varname = "Boost.Boost control limitation compression ratio turbocharger [KFLDHBN]";
                sh.MapAllowsNegatives = false;
                sh.X_axis_length = 8;
                sh.Y_axis_length = 8;
                sh.Length = 8 * 8;
                sh.X_axis_address = KFLDHBNAddress;
                sh.Y_axis_address = KFLDHBNAddress;
                sh.Flash_start_address = KFLDHBNAddress;
                symbols.Add(sh);
            }
            symbols.SortColumn = "Flash_start_address";
            symbols.SortingOrder = GenericComparer.SortOrder.Ascending;
            symbols.Sort();
            int countx180 = 0;
            foreach (SymbolHelper sh in symbols)
            {
                if (sh.Length == 0x200)
                {
                    sh.Varname = "Boost.Target load map [KFMIRL]";
                }
                else if (sh.Length == 0x180)
                {
                    if (countx180 == 0) sh.Varname = "Throttle.Accelerator pedal map [KFPED]";
                    else if (countx180 == 1) sh.Varname = "Boost.Target load map [KFMIRL]";
                    countx180++;
                }
                else if (sh.Length == 0x100)
                {
                    sh.Varname = "Throttle.Accelerator pedal map AUT reverse [KFPEDR]";
                }
                else if (sh.Length == 0xF0)
                {
                    sh.Varname = "Torque.Engine drag torque map [KFMDS]";
                }
                else if (sh.Length == 0x60)
                {
                    sh.Varname = "Cruise.Speed offset for cruise control [KFVOFFS]";
                }
                else if (sh.Length == 0xC0 && sh.X_axis_length == 6 && sh.Y_axis_length == 32)
                {
                    sh.Varname = "Airmass.Normalized airmass flow via the throttle [KFMSNWDK]";
                }
                else if (sh.Length == 0x70 && sh.X_axis_length == 8 && sh.Y_axis_length == 14)
                {
                    sh.Varname = "Lambda.Lambda delay time [KFLRST]";
                }
                if (sh.Category == "Undocumented" || sh.Category == "")
                {
                    if (sh.Varname.Contains("."))
                    {
                        try
                        {
                            sh.Category = sh.Varname.Substring(0, sh.Varname.IndexOf("."));
                        }
                        catch (Exception cE)
                        {
                            Console.WriteLine("Failed to assign category to symbol: " + sh.Varname + " err: " + cE.Message);
                        }
                    }

                }
            }
        }

        private int GetKFLDHBNMap(byte[] allBytes)
        {
            int offSetInFile = -1;
            byte[] sequence = new byte[5] { 0x04, 0xE0, 0xFF, 0xFF, 0xFB};
            byte[] seq_mask = new byte[5] { 1, 1, 0, 0, 1};
            offSetInFile = findSequence(allBytes, 0, sequence, seq_mask);
            if (offSetInFile > 0) offSetInFile += 201;
            return offSetInFile;
/*            int offSetInFile = -1;
            byte[] sequence = new byte[8] { 0xE0, 0xE7, 0xED, 0xFB, 0x80, 0x6D, 0x59, 0x59 };
            byte[] seq_mask = new byte[8] { 1, 1, 1, 1, 1, 1, 1, 1 };
            offSetInFile = findSequence(allBytes, 0, sequence, seq_mask);
            if (offSetInFile > 0) offSetInFile += 200;
            return offSetInFile;*/
        }

        private int GetLDRXNMap(byte[] allBytes)
        {
            int offSetInFile = -1;
            byte[] sequence = new byte[5] { 0x00, 0x1E, 0x00, 0xF0, 0x00};
            byte[] seq_mask = new byte[5] { 1, 1, 1, 1, 1};
            offSetInFile = findSequence(allBytes, 0, sequence, seq_mask);
            if (offSetInFile > 0) offSetInFile += 5;
            return offSetInFile;

        }

        private int GetKFMIOPMap(byte[] allBytes)
        {
            int offSetInFile = -1;
            byte[] sequence = new byte[6] { 0x1A, 0x33, 0x4D, 0x66, 0x80, 0x9A};
            byte[] seq_mask = new byte[6] { 1, 1, 1, 1, 1, 1};
            offSetInFile = findSequence(allBytes, 0, sequence, seq_mask);
            if (offSetInFile > 0) offSetInFile += 54;
            return offSetInFile;
        }

        private bool CollectionContainsAddress(SymbolCollection symbols, int p)
        {
            foreach (SymbolHelper sh in symbols)
            {
                if (sh.Flash_start_address == p) return true;
            }
            return false;
        }

        private int GetKFLDIMXMap(byte[] allBytes)
        {
            int offSetInFile = -1;
            if (allBytes.Length == 0x80000)
            {
                // small file goes a little bit different and has axis attached to it: 0x18D1E start of map in 0261204559-01275781-0000035097.Bin
                // axis start @
                byte[] sequence = new byte[18] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x10, 0x00, 0x08, 0x00 };
                byte[] seq_mask = new byte[18] { 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1 };
                offSetInFile = findSequence(allBytes, 0, sequence, seq_mask);
                if (offSetInFile > 0) offSetInFile += 14;
            }
            else
            {
                byte[] sequence = new byte[6] { 0x00, 0x00, 0x1D, 0x10, 0xB5, 0x10 };
                byte[] seq_mask = new byte[6] { 1, 1, 1, 1, 1, 1 };
                offSetInFile = findSequence(allBytes, 0, sequence, seq_mask);
                if (offSetInFile > 0) offSetInFile += 0x4C;
            }
            return offSetInFile;
        }

        private int GetEnrichmentMap(byte[] allBytes)
        {
            //TODO: Not always correct??? Look @ file Volvo S60 2.0T 163HP 0261204559 359462.ori.bin and s80-b6304s.bin
            byte[] sequence = new byte[8] {0x60, 0xA0, 0x60, 0xA0, 0xFF, 0x04, 0x28, 0x6B};
            byte[] seq_mask = new byte[8] {1, 1, 1, 1, 0, 1, 1, 1};
            int offSetInFile = findSequence(allBytes, 0, sequence, seq_mask);
            if (offSetInFile > 0) offSetInFile += 17;
            return offSetInFile;
        }

        private int GetVEMap(byte[] allBytes)
        {
            //KFKHFM
            byte[] sequence = new byte[20] {0x7C, 0x74, 0xF7, 0xF8, 0xFF, 0xFF, 0xFF, 0xFF,
                                            0x80, 0x00, 0xF7, 0xF8, 0xFF, 0x86, 0xE6, 0xFC,
                                            0xFF, 0xFF, 0xC2, 0xFD};
            byte[] seq_mask = new byte[20] {1, 1, 1, 1, 0, 0, 0, 0,
                                            1, 1, 1, 1, 0, 0, 1, 1,   
                                            0, 0, 1, 1};
            int offSetInFile = findSequence(allBytes, 0, sequence, seq_mask);
            if (offSetInFile > 0)
            {
                offSetInFile = Convert.ToInt32(allBytes[offSetInFile + 17]) * 256 + Convert.ToInt32(allBytes[offSetInFile + 16]);
            }

            return offSetInFile;
        }

        private int GetIgnitionMapAddress521KBFiles(byte[] allBytes)
        {
            //C1 00 C2 FF XX 06 DA 02 QQ QQ F7 F8 WW WW E6 FC ZZ ZZ E6 FD

            byte[] sequence = new byte[19] {0x1D, 0xC2, 0xFF, 0x42, 0x86, 0xDA, 0x02,
                                            0x34, 0x9A, 0xF7, 0xF8, 0x97, 0x85, 0xE6, 0xFC,
                                            0xFF, 0xFF, 0xE6, 0xFD};
            byte[] seq_mask = new byte[19] {1, 1, 1, 0, 1, 1, 1,
                                            0, 0, 1, 1, 0, 0, 1, 1,   
                                            0, 0, 1, 1};
            int offSetInFile = findSequence(allBytes, 0, sequence, seq_mask);
            if (offSetInFile > 0)
            {
                offSetInFile = Convert.ToInt32(allBytes[offSetInFile + 16]) * 256 + Convert.ToInt32(allBytes[offSetInFile + 15]);
            }

            return offSetInFile;
        }

        private int GetLambdaRequestMap(byte[] allBytes)
        {
            byte[] sequence = new byte[6] {0x0F, 0x06, 0xFF, 0xFF, 0x32, 0x3F};
            byte[] seq_mask = new byte[6] {1, 1, 0, 0, 1, 1};
            int offSetInFile = findSequence(allBytes, 0, sequence, seq_mask);
            return offSetInFile;
        }

        private int GetLambdaRequestMap512KBFiles(byte[] allBytes)
        {
            byte[] sequence = new byte[5] { 0x0A, 0x06, 0xFF, 0x32, 0x3F };
            byte[] seq_mask = new byte[5] { 1, 1, 0, 1, 1 };
            int offSetInFile = findSequence(allBytes, 0, sequence, seq_mask);
            return offSetInFile;
        }

        private int GetIgnitionMapAddress(byte[] allBytes)
        {
            //C1 00 C2 FF XX 06 DA 02 QQ QQ F7 F8 WW WW E6 FC ZZ ZZ E6 FD

            byte[] sequence = new byte[20] {0xC1, 0x00, 0xC2, 0xFF, 0xFF, 0x06, 0xDA, 0x02,
                                            0xFF, 0xFF, 0xF7, 0xF8, 0xFF, 0xFF, 0xE6, 0xFC,
                                            0xFF, 0xFF, 0xE6, 0xFD};
            byte[] seq_mask = new byte[20] {1, 1, 1, 1, 0, 1, 1, 1,
                                            0, 0, 1, 1, 0, 0, 1, 1,   
                                            0, 0, 1, 1};
            int offSetInFile = findSequence(allBytes, 0, sequence, seq_mask);
            if (offSetInFile > 0)
            {
                offSetInFile = Convert.ToInt32(allBytes[offSetInFile + 17]) * 256 + Convert.ToInt32(allBytes[offSetInFile + 16]);
            }
            
            return offSetInFile;


        }

        private void SetProgressPercentage(string info, int percentage)
        {
            if (onDecodeProgress != null)
            {
                onDecodeProgress(this, new DecodeProgressEventArgs(percentage, info));
            }
        }

        

        

        private float DetermineAverageMapValue(string filename, SymbolHelper sh)
        {
            float retval = 0;
            byte[] data = FileTools.Instance.readdatafromfile(filename, sh.Flash_start_address, sh.Length, sh.IsSixteenbits);
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
            if (symbolname == "Boost map") retval = 0.0234375; // <GS-16022011>
            else if (symbolname == "Target load map") retval = 0.0234375; // <GS-16022011>
            else if (symbolname.Contains("[KFMIRL]")) retval = 0.0234375; // <GS-16022011>
            else if (symbolname.Contains("Accelerator pedal characteristic") || symbolname.Contains("[KFPED]")) retval = 0.0030517578125;
            else if (symbolname.Contains("[KFPEDR]")) retval = 0.0030517578125;
            else if (symbolname.Contains("[LAMFA]")) retval = 0.0078125;
            else if (symbolname.Contains("[KFLDRL]")) retval = 0.005;
            else if (symbolname.Contains("[KFLDIMX]")) retval = 0.005;

            else if (symbolname.Contains("[KFMIOP]")) retval = 0.00152587890625;
            else if (symbolname.Contains("[LDRXN]")) retval = 0.0234375;
            else if (symbolname.Contains("[KFLDHBN]")) retval = 0.015625;
            else if (symbolname.Contains("[KFMDS]")) retval = 0.00152587890625;
            else if (symbolname.Contains("[KFMSNWDK]")) retval = 0.1;
            else if (symbolname.Contains("[KFVOFFS]")) retval = 0.0078125;
            else if (symbolname == "Internal load limiter") retval = 0.05;
            else if (symbolname == "Overboost map") retval = 0.0234375; // <GS-16022011>
            else if (symbolname.StartsWith("VE map")) retval = (double)1 / (double)127;
            else if (symbolname.StartsWith("Air mass meter correction")) retval = (double)1 / (double)128;
            else if (symbolname.Contains("[KFKHFM]")) retval = (double)1 / (double)128;
            else if (symbolname.Contains("[KFFDLBTS]")) retval = (double)1 / (double)128;
            else if (symbolname.Contains("[KFLBTS]")) retval = (double)1 / (double)128;
            else if (symbolname.StartsWith("WOT enrichment")) retval = (double)1 / (double)127;
            else if (symbolname.StartsWith("Cylinder compensation")) retval = (double)1 / (double)127;
            else if (symbolname.StartsWith("Ignition map")) retval = 0.75; //( 360º / #TEETH) / 4 = ( 360º /120) / 4 =0.75º 
            else if (symbolname.Contains("[KFZW]")) retval = 0.75;
            else if (symbolname.Contains("[KFZW2]")) retval = 0.75;
            else if (symbolname.Contains("[KFZWOP]")) retval = 0.75;
            else if (symbolname.Contains("[KFZWOP2]")) retval = 0.75;
            else if (symbolname.StartsWith("Secondary ignition map")) retval = 0.75; //( 360º / #TEETH) / 4 = ( 360º /120) / 4 =0.75º 
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
            return FileTools.Instance.readdatafromfile(m_currentFile, (int)offset, (int)length, issixteenbit);
        }

        public override FileInformation ParseFile()
        {
            m_fileInfo = new FileInformation();
            SymbolCollection symbols = new SymbolCollection();
            AxisCollection axis = new AxisCollection();
            LoadME7File(m_currentFile, out symbols, out axis);
            m_symbols = symbols;
            m_axis = axis;
            m_fileInfo.Symbols = symbols;
            m_fileInfo.Axis = axis;
            return m_fileInfo;
        }

        private void DecodeME7FileInformation(string filename, out string hardwareID, out string softwareID, out string partnumber, out string damosinfo)
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
                Console.WriteLine("DecodeME7FileInformation: " + E.Message);
            }

        }

        public override string GetPartnumber()
        {
            string hwid = string.Empty;
            string swid = string.Empty;
            string partnumber = string.Empty;
            string damos = string.Empty;

            DecodeME7FileInformation(m_currentFile, out hwid, out swid, out partnumber, out damos);
            return partnumber;
        }

        public override string GetHardwareID()
        {
            string hwid = string.Empty;
            string swid = string.Empty;
            string partnumber = string.Empty;
            string damos = string.Empty;

            DecodeME7FileInformation(m_currentFile, out hwid, out swid, out partnumber, out damos);
            return hwid;
        }

        public override string GetDamosInfo()
        {
            string hwid = string.Empty;
            string swid = string.Empty;
            string partnumber = string.Empty;
            string damos = string.Empty;
            DecodeME7FileInformation(m_currentFile, out hwid, out swid, out partnumber, out damos);
            return damos;
        }

        public override string GetSoftwareVersion()
        {
            string hwid = string.Empty;
            string swid = string.Empty;
            string partnumber = string.Empty;
            string damos = string.Empty;
            DecodeME7FileInformation(m_currentFile, out hwid, out swid, out partnumber, out damos);
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
            retval = Checksum_Volvo_ME7(FileTools.Instance.Currentfile, true);
            return retval;
        }

 /*******************************************************************************
  *  Routine: Checksum_Volvo_ME7
  *  Input: file_buffer = bin file buffer for checksum calculation 
  *  Output: file_buffer is directly modified
  *  
  *  Author: Salvatore Faro
  *  E-Mail: info@mtx-electronics.com
  *  Website: www.mtx-electronics.com
  * 
  *  License: Dilemma use the same open source license that you are using for your prog        
  ******************************************************************************/

        private bool Checksum_Volvo_ME7(string filename, bool checkOnly)
        {
            UInt32 buffer_index = 0x1F810;
            UInt32 start_addr;
            UInt32 end_addr;
            UInt32 checksum;
            UInt32 currChecksum;
            UInt32 ComplimentcurrChecksum;
            byte[] file_buffer = File.ReadAllBytes(filename);
            bool valid = true;
            do
            {
                // Get the checksum zone start address
                start_addr = ((UInt32)file_buffer[buffer_index + 3] << 24)
                           + ((UInt32)file_buffer[buffer_index + 2] << 16)
                           + ((UInt32)file_buffer[buffer_index + 1] << 8)
                           + (UInt32)file_buffer[buffer_index];

                // Get the checksum zone end address
                end_addr = ((UInt32)file_buffer[buffer_index + 7] << 24)
                         + ((UInt32)file_buffer[buffer_index + 6] << 16)
                         + ((UInt32)file_buffer[buffer_index + 5] << 8)
                         + (UInt32)file_buffer[buffer_index + 4];

                // Calculate the checksum by 32bit sum from star_addr to end_addr
                checksum = 0;
                for (UInt32 addr = start_addr; addr < end_addr; addr += 2)
                    checksum += ((UInt32)file_buffer[addr + 1] << 8) + (UInt32)file_buffer[addr];


                currChecksum = ((UInt32)file_buffer[buffer_index + 11] << 24)
                           + ((UInt32)file_buffer[buffer_index + 10] << 16)
                           + ((UInt32)file_buffer[buffer_index + 9] << 8)
                           + (UInt32)file_buffer[buffer_index + 8];
                ComplimentcurrChecksum = ((UInt32)file_buffer[buffer_index + 15] << 24)
                           + ((UInt32)file_buffer[buffer_index + 14] << 16)
                           + ((UInt32)file_buffer[buffer_index + 13] << 8)
                           + (UInt32)file_buffer[buffer_index + 12];

                Console.WriteLine("checksum calc: " + checksum.ToString("X8") + " file: " + currChecksum.ToString("X8"));

                if (checksum != currChecksum)
                {
                    valid = false;
                }
                UInt32 complchecksum = ~checksum;
                Console.WriteLine("checksum inv calc: " + checksum.ToString("X8") + " file: " + currChecksum.ToString("X8"));
                if (ComplimentcurrChecksum != complchecksum)
                {
                    valid = false;
                }
                if (!checkOnly)
                {
                    // Save the new checksum

                    FileTools.Instance.savebytetobinary((int)(buffer_index + 8), (byte)(checksum & 0x000000FF), filename);
                    FileTools.Instance.savebytetobinary((int)(buffer_index + 9), (byte)((checksum & 0x0000FF00) >> 8), filename);
                    FileTools.Instance.savebytetobinary((int)(buffer_index + 10), (byte)((checksum & 0x00FF0000) >> 16), filename);
                    FileTools.Instance.savebytetobinary((int)(buffer_index + 11), (byte)((checksum & 0xFF000000) >> 24), filename);
                    // Save the complement of the new checksum
                    checksum = ~checksum;
                    FileTools.Instance.savebytetobinary((int)(buffer_index + 12), (byte)(checksum & 0x000000FF), filename);
                    FileTools.Instance.savebytetobinary((int)(buffer_index + 13), (byte)((checksum & 0x0000FF00) >> 8), filename);
                    FileTools.Instance.savebytetobinary((int)(buffer_index + 14), (byte)((checksum & 0x00FF0000) >> 16), filename);
                    FileTools.Instance.savebytetobinary((int)(buffer_index + 15), (byte)((checksum & 0xFF000000) >> 24), filename);
                }
                buffer_index += 0x10;
            }
            while (buffer_index < 0x1FA00);
            return valid;
        }

        public override void UpdateChecksum()
        {
            Checksum_Volvo_ME7(FileTools.Instance.Currentfile, false);
        }

        public override double GetOffsetForMap(string symbolname)
        {
            return GetMapCorrectionOffset(symbolname);
        }

        private double GetMapCorrectionOffset(string mapname)
        {
            double retval = 0;
            //if (mapname.StartsWith("Boost map")) retval = 0;
            //if (mapname.StartsWith("Boost map")) retval = -1; // <GS-16022011>
            if (mapname.StartsWith("Overboost map")) retval = 0; // <GS-16022011>

                // was-1 
            else if (mapname.StartsWith("VE map")) retval = 0;
            //else if (mapname.StartsWith("Ignition map")) retval = -30; // or - 30 ?
            //else if (mapname.StartsWith("Ignition map")) retval = 60F; // or - 30 ?
            //else if (mapname.StartsWith("WOT ignition")) retval = 60F; // or - 30 ?
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
