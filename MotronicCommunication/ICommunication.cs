using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MotronicTools;

namespace MotronicCommunication
{

    abstract public class ICommunication
    {
        public enum CommunicationState : int
        {
            Start,
            SendWakeup,
            WaitForKeywords,
            Idle,
            SendCommand,
            WaitForResponse,
            Timeout
        }
        public enum ECUState : int
        {
            NotInitialized,
            Initialized,
            CommunicationRunning,
            Busy
        }
        public abstract bool CommunicationRunning
        {
            get;
            set;
        }

        public abstract bool IsWaitingForResponse
        {
            get;
            set;
        }

        public abstract bool EnableLogging
        {
            get;
            set;
        }

        public abstract string LogFolder
        {
            get;
            set;
        }

        public abstract void ReadDTCCodes(int timeout);
        public abstract void ClearDTC(int timeout);
        public abstract void ReadEprom(string filename, int timeout);
        public abstract bool WriteEprom(string filename, int timeout);
        public abstract void setCANDevice(string adapterType);
        public abstract byte[] readSRAM(int address, int bytestoread, int timeout, out bool success);

        public abstract void StartCommunication(string comportnumber, bool HighSpeed);
        public abstract void StopCommunication();

        //didnt make these abstract because they are needed only in one comm
        public virtual List<byte> ReadSensor(int pid, out bool success)
        {
            success = false;
            return null;
        }
        public virtual SymbolCollection ReadSupportedSensors()
        {
            return null;
        }

        public delegate void StatusChanged(object sender, StatusEventArgs e);
        abstract public event StatusChanged onStatusChanged;

        public delegate void DTCInfo(object sender, DTCEventArgs e);
        abstract public event DTCInfo onDTCInfo;

        public delegate void ECUInfo(object sender, ECUInfoEventArgs e);
        abstract public event ECUInfo onECUInfo;

        public class ECUInfoEventArgs : System.EventArgs
        {
            private string _info;

            public string Info
            {
                get { return _info; }
                set { _info = value; }
            }

            private int _idnumber;

            public int IDNumber
            {
                get { return _idnumber; }
                set { _idnumber = value; }
            }

            public ECUInfoEventArgs(string info, int idnumber)
            {
                this._info = info;
                this._idnumber = idnumber;
            }
        }

        public class DTCEventArgs : System.EventArgs
        {
            private int _dtccode;

            public int Dtccode
            {
                get { return _dtccode; }
                set { _dtccode = value; }
            }
            private int _dtcstate;

            public int Dtcstate
            {
                get { return _dtcstate; }
                set { _dtcstate = value; }
            }
            private int _dtccondition1;

            public int Dtccondition1
            {
                get { return _dtccondition1; }
                set { _dtccondition1 = value; }
            }
            private int _dtccondition2;

            public int Dtccondition2
            {
                get { return _dtccondition2; }
                set { _dtccondition2 = value; }
            }
            private int _dtccounter;

            public int Dtccounter
            {
                get { return _dtccounter; }
                set { _dtccounter = value; }
            }

            public DTCEventArgs(int dtccode, int dtcstate, int dtccondition1, int dtccondition2, int dtccounter)
            {
                _dtccode = dtccode;
                _dtcstate = dtcstate;
                _dtccondition1 = dtccondition1;
                _dtccondition2 = dtccondition2;
                _dtccounter = dtccounter;
            }
        }

        public class StatusEventArgs : System.EventArgs
        {
            private ECUState _state;

            public ECUState State
            {
                get { return _state; }
                set { _state = value; }
            }

            private string _info;

            public string Info
            {
                get { return _info; }
                set { _info = value; }
            }

            private int _percentage;

            public int Percentage
            {
                get { return _percentage; }
                set { _percentage = value; }
            }

            public StatusEventArgs(string info, int percentage, ECUState state)
            {
                this._info = info;
                this._percentage = percentage;
                this._state = state;
            }
        }

    }
}
