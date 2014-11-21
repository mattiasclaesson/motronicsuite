using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MotronicTools;

namespace MotronicSuite
{
    public sealed class FileTools
    {
        private static volatile FileTools instance;
        private static object syncRoot = new Object();

        //<GS-28022011> add project based development
        private string m_CurrentWorkingProject = string.Empty;

        public string CurrentWorkingProject
        {
            get { return m_CurrentWorkingProject; }
            set { m_CurrentWorkingProject = value; }
        }
        private ProjectLog m_ProjectLog = new ProjectLog();

        public ProjectLog ProjectLog
        {
            get { return m_ProjectLog; }
            set { m_ProjectLog = value; }
        }
        //<GS-28022011> add project based development


        private int m_currentfile_size = 0x10000;

        public int Currentfile_size
        {
            get { return m_currentfile_size; }
            set { m_currentfile_size = value; }
        }
        private string m_currentfile = "";

        public string Currentfile
        {
            get { return m_currentfile; }
            set { m_currentfile = value; }
        }


        FileType m_currentFiletype = FileType.UNKNOWN;

        public FileType CurrentFiletype
        {
            get { return m_currentFiletype; }
            set { m_currentFiletype = value; }
        }


        private TransactionLog m_ProjectTransactionLog;

        public TransactionLog ProjectTransactionLog
        {
            get { return m_ProjectTransactionLog; }
            set { m_ProjectTransactionLog = value; }
        }

        public static FileTools Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new FileTools();
                        }
                    }
                }

                return instance;
            }
        }

        private int rpmlimit = 0;

        public int Rpmlimit
        {
            get { return rpmlimit; }
            set { rpmlimit = value; }
        }
        private int rpmlimit2 = 0;

        public int Rpmlimit2
        {
            get { return rpmlimit2; }
            set { rpmlimit2 = value; }
        }
        private int speedlimit = 0;

        public int Speedlimit
        {
            get { return speedlimit; }
            set { speedlimit = value; }
        }

        public bool FirstColumnForTableAveragesLessThan(string filename, SymbolHelper sh, int value, int width)
        {
            bool retval = false;
            int height = sh.Length / width;
            byte[] data = readdatafromfile(filename, sh.Flash_start_address, sh.Length, sh.IsSixteenbits);
            int average = 0;
            for (int i = 0; i < height; i++)
            {
                average += Convert.ToInt32(data[i * width]);
            }
            average /= height;
            if (average < value) retval = true;
            return retval;
        }

        public bool MapContainsMostly(string filename, SymbolHelper sh, int value, int deviation, int percentage)
        {
            bool retval = false;
            byte[] data = readdatafromfile(filename, sh.Flash_start_address, sh.Length, sh.IsSixteenbits);
            int hitcount = 0;
            foreach (byte b in data)
            {
                if ((int)b >= value - deviation && (int)b <= value + deviation)
                {
                    hitcount++;
                }
            }
            int detectedpercentage = (hitcount * 100) / sh.Length;
            if (detectedpercentage > percentage) retval = true;
            //if (retval) Console.WriteLine("map: " + sh.Flash_start_address.ToString("X4") + " contains mostly: " + value.ToString() + " perc: " + detectedpercentage);
            return retval;
        }

        public int[] readdatafromfileasint(string filename, int address, int length)
        {
            int[] retval = new int[length];
            FileStream fsi1 = File.OpenRead(filename);
            while (address > fsi1.Length) address -= (int)fsi1.Length;
            BinaryReader br1 = new BinaryReader(fsi1);
            fsi1.Position = address;
            string temp = string.Empty;
            for (int i = 0; i < length; i++)
            {
                retval.SetValue((int)br1.ReadByte(), i);
            }
            if (m_currentFiletype == FileType.MOTRONICME7)
            {
                // little endian, reverse bytes
                retval = reverseEndian(retval);
            }
            fsi1.Flush();
            br1.Close();
            fsi1.Close();
            fsi1.Dispose();
            return retval;
        }

        public byte[] readdatafromfile(string filename, int address, int length, bool isSixteenBit)
        {
            byte[] retval = new byte[length];
            FileStream fsi1 = File.OpenRead(filename);
            while (address > fsi1.Length) address -= (int)fsi1.Length;
            BinaryReader br1 = new BinaryReader(fsi1);
            fsi1.Position = address;
            string temp = string.Empty;
            for (int i = 0; i < length; i++)
            {
                retval.SetValue(br1.ReadByte(), i);
            }

            if (m_currentFiletype == FileType.MOTRONICME7 && isSixteenBit)
            {
                // little endian, reverse bytes
                retval = reverseEndian(retval);
            }

            fsi1.Flush();
            br1.Close();
            fsi1.Close();
            fsi1.Dispose();
            return retval;
        }

        private byte[] reverseEndian(byte[] retval)
        {
            byte[] ret = new byte[retval.Length];

            try
            {
                if (retval.Length > 0 && retval.Length %2 == 0)
                {
                    for (int i = 0; i < retval.Length; i += 2)
                    {
                        byte b1 = retval[i];
                        byte b2 = retval[i + 1];
                        ret[i] = b2;
                        ret[i + 1] = b1;
                    }
                }
            }
            catch (Exception E)
            {

            }
            return ret;
        }

        private int[] reverseEndian(int[] retval)
        {
            int[] ret = new int[retval.Length];

            try
            {
                if (retval.Length > 0 && retval.Length % 2 == 0)
                {
                    for (int i = 0; i < retval.Length; i += 2)
                    {
                        int b1 = retval[i];
                        int b2 = retval[i + 1];
                        ret[i] = b2;
                        ret[i + 1] = b1;
                    }
                }
            }
            catch (Exception E)
            {

            }
            return ret;
        }

        public void savedatatobinary(int address, int length, byte[] data, string filename, bool DoTransActionEntry)
        {
            FileInfo fi = new FileInfo(filename);
            if (m_currentFiletype == FileType.MOTRONICME7)
            {
                // little endian, reverse bytes
                data = reverseEndian(data);
            }
            if (address > 0 && address < fi.Length)
            {
                try
                {
                    byte[] beforedata = readdatafromfile(filename, address, length, false);
                    FileStream fsi1 = File.OpenWrite(filename);
                    BinaryWriter bw1 = new BinaryWriter(fsi1);
                    fsi1.Position = address;



                    for (int i = 0; i < length; i++)
                    {
                        bw1.Write((byte)data.GetValue(i));
                    }
                    fsi1.Flush();
                    bw1.Close();
                    fsi1.Close();
                    fsi1.Dispose();

                    if (m_ProjectTransactionLog != null && DoTransActionEntry)
                    {
                        TransactionEntry tentry = new TransactionEntry(DateTime.Now, address, length, beforedata, data, 0, 0, "");
                        m_ProjectTransactionLog.AddToTransactionLog(tentry);
                        SignalTransactionLogChanged(tentry.SymbolAddress, tentry.Note);
                    }

                }
                catch (Exception E)
                {
                    frmInfoBox info = new frmInfoBox("Failed to write to binary. Is it read-only? Details: " + E.Message);
                }
            }
        }

        public void savedatatobinary(int address, int length, byte[] data, string filename, bool DoTransActionEntry, string note)
        {
            FileInfo fi = new FileInfo(filename);
            if (m_currentFiletype == FileType.MOTRONICME7)
            {
                // little endian, reverse bytes
                data = reverseEndian(data);
            }
            if (address > 0 && address < fi.Length)
            {
                try
                {
                    byte[] beforedata = readdatafromfile(filename, address, length, false);
                    FileStream fsi1 = File.OpenWrite(filename);
                    BinaryWriter bw1 = new BinaryWriter(fsi1);
                    fsi1.Position = address;

                    for (int i = 0; i < length; i++)
                    {
                        bw1.Write((byte)data.GetValue(i));
                    }
                    fsi1.Flush();
                    bw1.Close();
                    fsi1.Close();
                    fsi1.Dispose();

                    if (m_ProjectTransactionLog != null && DoTransActionEntry)
                    {
                        TransactionEntry tentry = new TransactionEntry(DateTime.Now, address, length, beforedata, data, 0, 0, note);
                        m_ProjectTransactionLog.AddToTransactionLog(tentry);
                        SignalTransactionLogChanged(tentry.SymbolAddress, tentry.Note);
                    }
                }
                catch (Exception E)
                {
                    frmInfoBox info = new frmInfoBox("Failed to write to binary. Is it read-only? Details: " + E.Message);
                }
            }
        }

        private void SignalTransactionLogChanged(int address, string note)
        {
            //TODO: Cast event to owner to update buttons?
            // should contain the new info as well
            // <GS-18032010> insert logbook entry here if project is opened
            /*if (FileTools.Instance.CurrentWorkingProject != string.Empty)
            {
                FileTools.Instance.ProjectLog.WriteLogbookEntry(LogbookEntryType.TransactionExecuted, Helpers.Instance.GetSymbolNameByAddress(SymbolAddress) + " " + Note);
            }*/
            // owner should call function: UpdateRollbackForwardControls
        }

        public void savedatatobinary(int address, int length, byte[] data, string filename)
        {
            if (m_currentFiletype == FileType.MOTRONICME7)
            {
                // little endian, reverse bytes
                data = reverseEndian(data);
            }
            FileStream fsi1 = File.OpenWrite(filename);
            BinaryWriter bw1 = new BinaryWriter(fsi1);
            fsi1.Position = address;

            for (int i = 0; i < length; i++)
            {
                bw1.Write((byte)data.GetValue(i));
            }
            fsi1.Flush();
            bw1.Close();
            fsi1.Close();
            fsi1.Dispose();
        }

        public void savebytetobinary(int address, byte data, string filename)
        {

            FileStream fsi1 = File.OpenWrite(filename);
            BinaryWriter bw1 = new BinaryWriter(fsi1);
            fsi1.Position = address;
            bw1.Write((byte)data);
            fsi1.Flush();
            bw1.Close();
            fsi1.Close();
            fsi1.Dispose();
        }

        public int LookupByteArrayInFile(byte[] arr, string filename)
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
                    if (b == arr[readstate])
                    {
                        readstate++;
                        if (readstate == arr.Length)
                        {
                            lookuptablestartaddress = (int)fs.Position;
                        }
                    }
                    else
                    {
                        readstate = 0;
                    }
                }
            }
            fs.Close();
            fs.Dispose();
            return lookuptablestartaddress;
        }


        internal bool LeftSideLowerThanRightSide(string filename, SymbolHelper sh, int width, int height)
        {
            bool retval = false;
            byte[] data = readdatafromfile(filename, sh.Flash_start_address, sh.Length, sh.IsSixteenbits);

            double leftside = 0;
            double rightside = 0;
            for (int i = 0; i < height * width; i+=width)
            {
                for (int j = 1; j < width / 2; j++)
                {
                    leftside += Convert.ToDouble(data.GetValue(i + j));
                }
                for (int j = width/2; j < width ; j++)
                {
                    rightside += Convert.ToDouble(data.GetValue(i + j));
                }
                
            }

            leftside /= ((width / 2) - 1) * height;
            rightside /= ((width / 2)) * height;
            Console.WriteLine(sh.Flash_start_address.ToString("X8") + " avgleft: " + leftside.ToString() + " avgright: " + rightside.ToString());
            if (leftside < rightside) retval = true;
           
            return retval;
        }
    }
}
