using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using MotronicTools;

namespace MotronicSuite
{
    public enum FileType : int
    {
        LH22,
        LH24,
        LH242,
        ML11,
        MOTRONIC43,
        MOTRONIC44,
        MOTRONIC18,
        MOTRONICME7,
        UNKNOWN
    }
    /// <summary>
    /// </summary>
    abstract public class IECUFile
    {
        
        /// <summary>
        /// </summary>
        /// 
        public delegate void DecodeProgress(object sender, DecodeProgressEventArgs e);
        abstract public event DecodeProgress onDecodeProgress;

        public delegate void TransactionLogChanged(object sender, TransactionsEventArgs e);
        abstract public event TransactionLogChanged onTransactionLogChanged;

        abstract public byte[] ReadData(uint offset, uint length, bool issixteenbit);

        //abstract public int[] GetSymbolAsIntArray(string symbolname);
        
        //abstract public int GetSymbolAsInt(string symbolname);
        
        //abstract public int[] GetXaxisValues(string filename, string symbolname);
        
        //abstract public int[] GetYaxisValues(string filename, string symbolname);

        abstract public void SetTransactionLog(TransactionLog transactionLog);

        abstract public void SetAutoUpdateChecksum(bool autoUpdate);

        //abstract public byte[] ReadDataFromFile(string filename, uint offset, uint length);

        abstract public bool WriteDataNoLog(byte[] data, uint offset);

        abstract public bool WriteData(byte[] data, uint offset);

        abstract public bool WriteData(byte[] data, uint offset, string note);

        abstract public bool ValidateChecksum();

        abstract public void UpdateChecksum();

        abstract public bool HasSymbol(string symbolname);

        abstract public bool IsTableSixteenBits(string symbolname);

        abstract public bool IsAutomaticTransmission(out bool found);

        abstract public double GetCorrectionFactorForMap(string symbolname);

        //abstract public int[] GetMapXaxisValues(string symbolname);

        abstract public void GetMapAxisDescriptions(string symbolname, out string x, out string y, out string z);

        abstract public void GetMapMatrixWitdhByName(string symbolname, out int columns, out int rows);

        //abstract public int[] GetMapYaxisValues(string symbolname);

        abstract public double GetOffsetForMap(string symbolname);

        abstract public void SelectFile(string filename);

        abstract public void BackupFile();

        abstract public string GetSoftwareVersion();
        abstract public string GetPartnumber();
        abstract public string GetHardwareID();
        abstract public string GetDamosInfo();

        abstract public FileInformation ParseFile();

        abstract public FileInformation GetFileInfo();

        abstract public FileType DetermineFileType();

        abstract public bool Exists();

        abstract public void WriteSpeedLimiter(int speedlimit);

        abstract public int ReadSpeedLimiter();

        abstract public void WriteRpmLimiter(int rpmlimiter);

        abstract public int ReadRpmLimiter();

        abstract public SymbolCollection Symbols
        {
            get;
            set;
        }

        abstract public AxisCollection Axis
        {
            get;
            set;
        }

    }

    public class TransactionsEventArgs : System.EventArgs
    {
        private TransactionEntry _entry;

        public TransactionEntry Entry
        {
            get { return _entry; }
            set { _entry = value; }
        }

        public TransactionsEventArgs(TransactionEntry entry)
        {
            this._entry = entry;
        }
    }

    public class DecodeProgressEventArgs : System.EventArgs
    {
        private int _progress;

        public int Progress
        {
            get { return _progress; }
            set { _progress = value; }
        }

        private string _info;

        public string Info
        {
            get { return _info; }
            set { _info = value; }
        }


        public DecodeProgressEventArgs(int progress, string info)
        {
            this._progress = progress;
            this._info = info;
        }
    }
}

