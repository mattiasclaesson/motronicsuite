using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MotronicTools;

namespace MotronicSuite
{
    public sealed class Helpers
    {
        private static volatile Helpers instance;
        private static object syncRoot = new Object();

        public static Helpers Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new Helpers();
                        }
                    }
                }

                return instance;
            }
        }

        public int GetSymbolAddress(SymbolCollection sc, string symbol)
        {
            foreach (SymbolHelper sh in sc)
            {
                if (sh.Varname == symbol)
                {
                    return sh.Flash_start_address;
                }
            }
            return 0;
        }

        public int GetSymbolLength(SymbolCollection sc, string symbol)
        {
            foreach (SymbolHelper sh in sc)
            {
                if (sh.Varname == symbol)
                {
                    return sh.Length;
                }
            }
            return 0;
        }

        public double GetMapCorrectionOffset(string mapname)
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

        public double GetMapCorrectionFactor(string mapname)
        {
            double retval = 1;
            if (mapname == "Boost map") retval = 0.01; // <GS-16022011>
            else if (mapname == "Internal load limiter") retval = 0.05;
            else if (mapname == "Overboost map") retval = 0.005; // <GS-16022011>
            //if (mapname.StartsWith("Boost map")) retval = /*0.048;*/0.00391F;
            else if (mapname.StartsWith("VE map")) retval = (double)1 / (double)127;
            else if (mapname.StartsWith("WOT enrichment")) retval = (double)1 / (double)127;
            else if (mapname.StartsWith("Cylinder compensation")) retval = (double)1 / (double)127;
            else if (mapname.StartsWith("Ignition map")) retval = 0.75; //( 360º / #TEETH) / 4 = ( 360º /120) / 4 =0.75º 
            else if (mapname.StartsWith("WOT ignition")) retval = 0.75; //( 360º / #TEETH) / 4 = ( 360º /120) / 4 =0.75º 
            else if (mapname == "Dynamic airmass when AC compressor comes on") retval = 0.4;
            else if (mapname == "Ignition delta when aircocompressor is on") retval = -0.75;
            else if (mapname == "Delta ignition advance after vacuum (?)") retval = -0.75;
            else if (mapname == "Dynamic ignition retard") retval = -0.75;
            else if (mapname == "Dashpot extra air") retval = 0.4;
            else if (mapname == "Virtual throttle angle from bypass correction") retval = 0.41667;
            else if (mapname == "Load value from throttle position (angle) including bypass correction") retval = 0.048;
            else if (mapname == "Increase of idle target rpm when catalyst heating") retval = 10;
            else if (mapname == "Injection angle on start of injection") retval = -6;
            else if (mapname == "Altitude dependent leak diagnosis threshold") retval = 0.003906;
            else if (mapname == "Max. enrichment for knock") retval = 0.007813;
            else if (mapname == "Re-engange ignition advance") retval = -0.75;
            else if (mapname == "Startfactor reduction in second range") retval = 0.003906;
            else if (mapname == "Airmass increase for catalyst heating") retval = 0.4;
            else if (mapname == "Delta ignition advance for catalyst heating in partload") retval = -0.75;
            else if (mapname == "Temporary change in ignition advance when catalyst heating in vacuum") retval = 0.003906;
            else if (mapname == "Temporary change in ignition advance when catalyst heating on partload") retval = 0.003906;
            else if (mapname == "Injection duration when fuel comes back") retval = 0.007813;
            else if (mapname == "Volumeflow through the open tank purge valve") retval = 0.026000;
            else if (mapname == "Maximum duty cycle") retval = 0.390625;
            else if (mapname == "Dutycycle tank purge valve") retval = 0.390625;
            else if (mapname == "Coldstart correction factor") retval = 0.007813;
            else if (mapname == "Restart correction factor") retval = 0.007813;
            // TODO!!!! 
            //else if (mapname == "Boost map") retval = 0.048000;
            else if (mapname == "Dutycycle bias for boost control") retval = 0.390625;
            else if (mapname == "Dutycycle correction for boost control for altitude") retval = 0.390625;
            else if (mapname == "Intake air temperature correction for boost control dutycycle") retval = 0.390625;
            else if (mapname == "Threshold total ignition retard for boostpressure fadeout") retval = -0.750000;
            else if (mapname == "Kennfeld fur Laufunruhe-reference-value") retval = 0.517400;
            else if (mapname == "Kennfeld Absenkungsfaktor fur Lur-Wert bei erkannten Mehrfachaussetzern") retval = 0.003906;
            else if (mapname == "Kennfeld fur Laufunruhe-Referenzwert zur Mehrfachaussetzererkennung ->Lum-Vergl.") retval = 0.517400;
            else if (mapname == "MAF to Load conversion map") retval = 0.05;
            return retval;
        }


        public int FindFirstAddressInLists(int address, AxisCollection axis, SymbolCollection m_Unknown_symbols)
        {
            FileInfo fi = new FileInfo(FileTools.Instance.Currentfile);
            int maxvalue = (int)fi.Length;
            int retval = maxvalue;
            Console.WriteLine("Searching for address : " + address.ToString("X4"));
            foreach (AxisHelper ah in axis)
            {
                if (ah.Addressinfile < maxvalue && ah.Addressinfile < retval && ah.Addressinfile > address)
                {
                    retval = ah.Addressinfile;
                    Console.WriteLine("Ret val is now: " + retval.ToString("X4"));
                }
            }
            foreach (SymbolHelper sh in m_Unknown_symbols)
            {
                if (sh.Flash_start_address < maxvalue && sh.Flash_start_address < retval && sh.Flash_start_address > address)
                {
                    retval = sh.Flash_start_address;
                    Console.WriteLine("Ret val is now: " + retval.ToString("X4"));
                }
            }
            return retval;

        }


        public int DetermineColumnsInMapByLength(int length)
        {
            int cols = 1;
            if (length == 256)
            {
                cols = 16;
            }
            else if (length == 144)
            {
                cols = 12;
            }
            else if (length == 128)
            {
                cols = 16;
            }
            else if (length == 96)
            {
                cols = 16;
            }
            else if (length == 84)
            {
                cols = 7;
            }
            else if (length == 80)
            {
                cols = 5;
            }
            else if (length == 70)
            {
                cols = 7;
            }
            else if (length == 64)
            {
                cols = 8;
            }
            else if (length == 59)
            {
                cols = 10;
            }
            else if (length == 50)
            {
                cols = 10;
            }
            else if (length == 48)
            {
                cols = 8;
            }
            else if (length == 42)
            {
                cols = 6;
            }
            else if (length == 40)
            {
                cols = 8;
            }
            else
            {
                cols = 1;
            }
            return cols;
        }

        public int DetermineRowsInMapByLength(int length)
        {
            int rows = 1; //<GS-21022011>
            if (length == 256)
            {
                rows = 16;
            }
            else if (length == 144)
            {
                rows = 12;
            }
            else if (length == 128)
            {
                rows = 8;
            }
            else if (length == 96)
            {
                rows = 6;
            }

            else if (length == 84)
            {
                rows = 12;
            }
            else if (length == 80)
            {
                rows = 16;
            }
            else if (length == 70)
            {
                rows = 10;
            }
            else if (length == 64)
            {
                rows = 8;
            }
            else if (length == 59)
            {
                rows = 6;
            }
            else if (length == 50)
            {
                rows = 5;
            }
            else if (length == 48)
            {
                rows = 6;
            }
            else if (length == 42)
            {
                rows = 7;
            }
            else if (length == 40)
            {
                rows = 5;
            }
            else
            {
                rows = length;
            }
            return rows;

        }

        public float DetermineAverageMapValue(string filename, SymbolHelper sh)
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

        public bool AxisHasLeadingAxis(AxisCollection axis, int address, out int axisaddress)
        {
            bool retval = false;
            axisaddress = 0;
            foreach (AxisHelper ah in axis)
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

        public bool AxisHasLeadingAxis(AxisCollection axis, int address, out float[] xaxis, out string y_descr)
        {
            bool retval = false;
            y_descr = string.Empty;
            xaxis = new float[1];
            xaxis.SetValue(0, 0);
            foreach (AxisHelper ah in axis)
            {
                int endaddress = ah.Addressinfile + ah.Length + 2;
                if (endaddress == address)
                {
                    retval = true;
                    xaxis = ah.CalculcatedValues;
                    if (ah.Descr != string.Empty)
                    {
                        y_descr = ah.Descr;
                    }
                    else
                    {
                        y_descr = ah.Identifier.ToString();
                    }
                }
            }
            return retval;
        }

        public bool AxisHasLeadingAxis(AxisCollection axis, int address, out int[] xaxis, out string y_descr)
        {
            bool retval = false;
            y_descr = string.Empty;
            xaxis = new int[1];
            xaxis.SetValue(0, 0);
            foreach (AxisHelper ah in axis)
            {
                int endaddress = ah.Addressinfile + ah.Length + 2;
                if (endaddress == address)
                {
                    retval = true;
                    xaxis = ah.Values;
                    if (ah.Descr != string.Empty)
                    {
                        y_descr = ah.Descr;
                    }
                    else
                    {
                        y_descr = ah.Identifier.ToString();
                    }
                }
            }
            return retval;
        }

        public SymbolHelper GetXaxisSymbol(string m_currentfile, SymbolCollection symbols, AxisCollection axis, string symbolname, int address)
        {
            SymbolHelper retval = new SymbolHelper();

            foreach (AxisHelper ah in axis)
            {
                int endaddress = ah.Addressinfile + ah.Length + 2;
                if (endaddress == address)
                {
                    // this is an axis for this table... 
                    // see if there is another one that leads 
                    int[] yaxis;
                    string y_descr = string.Empty;
                    if (AxisHasLeadingAxis(axis, ah.Addressinfile, out yaxis, out y_descr))
                    {
                        retval.X_axis_address = ah.Addressinfile;
                        retval.X_axis_length = ah.Length;
                    }
                    else
                    {
                        retval.X_axis_address = ah.Addressinfile;
                        retval.X_axis_length = ah.Length;
                    }
                }
            }
            return retval;
        }

        public SymbolHelper GetLeadingAxis(AxisCollection axis, int address)
        {
            SymbolHelper retval = new SymbolHelper();

            foreach (AxisHelper ah in axis)
            {
                int endaddress = ah.Addressinfile + ah.Length + 2;
                if (endaddress == address)
                {
                    retval.Y_axis_address = ah.Addressinfile;
                    retval.Y_axis_length = ah.Length;
                }
            }
            return retval;
        }

        public SymbolHelper GetYAxisSymbol(string m_currentfile, SymbolCollection symbols, AxisCollection axis, string symbolname, int address)
        {
            SymbolHelper retval = new SymbolHelper();

            foreach (AxisHelper ah in axis)
            {
                int endaddress = ah.Addressinfile + ah.Length + 2;
                if (endaddress == address)
                {
                    // this is an axis for this table... 
                    // see if there is another one that leads 
                    int[] yaxis;
                    string y_descr = string.Empty;
                    retval = GetLeadingAxis(axis, ah.Addressinfile);
                }
            }
            return retval;
        }

        public void GetAxisValues(string m_currentfile, SymbolCollection symbols, AxisCollection axis, string symbolname, int address, int rows, int cols, out float[] Xaxis, out float[] Yaxis, out string xdescr, out string ydescr)
        {
            xdescr = "";
            ydescr = "";
            Yaxis = new float[rows];
            for (int i = 0; i < Yaxis.Length; i++)
            {
                Yaxis.SetValue((float)i, i);
            }
            Xaxis = new float[cols];
            for (int i = 0; i < Xaxis.Length; i++)
            {
                Xaxis.SetValue((float)i, i);
            }

            // let's see if there are leading axis info to this table

            foreach (AxisHelper ah in axis)
            {
                int endaddress = ah.Addressinfile + ah.Length + 2;
                if (endaddress == address)
                {
                    // this is an axis for this table... 
                    // see if there is another one that leads 
                    float[] yaxis;
                    string y_descr = string.Empty;
                    if (Helpers.Instance.AxisHasLeadingAxis(axis, ah.Addressinfile, out yaxis, out y_descr))
                    {
                        Yaxis = yaxis;
                        ydescr = y_descr;
                        Xaxis = ah.CalculcatedValues;
                        if (ah.Descr != string.Empty)
                        {
                            xdescr = ah.Descr;
                        }
                        else
                        {
                            xdescr = ah.Identifier.ToString();
                        }
                    }
                    else
                    {
                        Xaxis = ah.CalculcatedValues;
                        if (ah.Descr != string.Empty)
                        {
                            xdescr = ah.Descr;
                        }
                        else
                        {
                            xdescr = ah.Identifier.ToString();
                        }
                        Yaxis = new float[rows];
                        for (int i = 0; i < Yaxis.Length; i++)
                        {
                            Yaxis.SetValue(i, i);
                        }
                    }

                }
            }
        }

        public void GetAxisValues(string m_currentfile, SymbolCollection symbols, AxisCollection axis, string symbolname, int address, int rows, int cols, out int[] Xaxis, out int[] Yaxis, out string xdescr, out string ydescr)
        {
            xdescr = "";
            ydescr = "";
            Yaxis = new int[rows];
            for (int i = 0; i < Yaxis.Length; i++)
            {
                Yaxis.SetValue(i, i);
            }
            Xaxis = new int[cols];
            for (int i = 0; i < Xaxis.Length; i++)
            {
                Xaxis.SetValue(i, i);
            }

            // let's see if there are leading axis info to this table

            foreach (AxisHelper ah in axis)
            {
                int endaddress = ah.Addressinfile + ah.Length + 2;
                if (endaddress == address)
                {
                    // this is an axis for this table... 
                    // see if there is another one that leads 
                    int[] yaxis;
                    string y_descr = string.Empty;
                    if (Helpers.Instance.AxisHasLeadingAxis(axis, ah.Addressinfile, out yaxis, out y_descr))
                    {
                        Yaxis = yaxis;
                        ydescr = y_descr;
                        Xaxis = ah.Values;
                        if (ah.Descr != string.Empty)
                        {
                            xdescr = ah.Descr;
                        }
                        else
                        {
                            xdescr = ah.Identifier.ToString();
                        }
                    }
                    else
                    {
                        Xaxis = ah.Values;
                        if (ah.Descr != string.Empty)
                        {
                            xdescr = ah.Descr;
                        }
                        else
                        {
                            xdescr = ah.Identifier.ToString();
                        }
                        Yaxis = new int[rows];
                        for (int i = 0; i < Yaxis.Length; i++)
                        {
                            Yaxis.SetValue(i, i);
                        }
                    }

                }
            }
        }

        public bool CheckForAxisPresent(string filename, int startaddress, AxisCollection axis, int lengthOfPreviousAxis)
        {
            bool retval = false;
            FileStream fs = new FileStream(filename, FileMode.Open);
            fs.Position = startaddress;
            using (BinaryReader br = new BinaryReader(fs))
            {
                int id = (int)br.ReadByte();
                int length = (int)br.ReadByte();
                if (id >= 0x03 && id <= 0x70 && length > 1 && length < 32)
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
                            //Console.WriteLine("Overruled axis detection at : " + pos.ToString("X4"));
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

        public bool AxisPresentInCollection(int address, AxisCollection axis)
        {
            foreach (AxisHelper ah in axis)
            {
                if (ah.Addressinfile == address)
                {
                    return true;
                }
            }
            return false;
        }

        public string GetSymbolNameByAddress(SymbolCollection symbols, Int32 address)
        {
            foreach (SymbolHelper sh in symbols)
            {
                if (sh.Flash_start_address == address) return sh.Varname;
            }
            return address.ToString();
        }

    }
}
