using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace MotronicCommunication
{
    public class M2103Communication : ICommunication
    {
        public static int INIT_ECU_ADDR = 0x13;
        public static int ECU_BAUDRATE = 9600;

        private SAEJ1979 m_j1979 = new SAEJ1979();

        public override event ICommunication.DTCInfo onDTCInfo;
        public override event ICommunication.ECUInfo onECUInfo
        {
            add { m_j1979.onECUInfo += value; }
            remove { m_j1979.onECUInfo -= value; }
        }
        public override event ICommunication.StatusChanged onStatusChanged
        {
            add { m_j1979.onStatusChanged += value; }
            remove { m_j1979.onStatusChanged -= value; }
        }

        private bool _communicationRunning = false; //important
        public override bool CommunicationRunning
        {
            get { return _communicationRunning; }
            set { _communicationRunning = value; }
        }

        private bool _IsWaitingForResponse = false;
        public override bool IsWaitingForResponse //rt timer tick needs this
        {
            get { return _IsWaitingForResponse; }
            set { _IsWaitingForResponse = value; }
        }

        private bool _enableLogging = false;

        public override bool EnableLogging
        {
            get { return _enableLogging; }
            set { _enableLogging = value; }
        }

        private string _logFolder = string.Empty;

        public override string LogFolder
        {
            get { return _logFolder; }
            set { _logFolder = value; }
        }

        public override void ReadDTCCodes(int timeout)
        {
        }

        public override void ClearDTC(int timeout)
        {
        }

        public override void ReadEprom(string filename, int timeout)
        {
        }

        public override bool WriteEprom(string filename, int timeout)
        {
            return false;
        }
        public override void setCANDevice(string adapterType)
        {

        }

        public override byte[] readSRAM(int address, int bytestoread, int timeout, out bool success)
        {
            success = true;
            return null;
        }

        public override void StartCommunication(string comportnumber, bool HighSpeed)
        {
            //m_j1979.initialize(comportnumber, INIT_ECU_ADDR, ECU_BAUDRATE);
            m_j1979.initialize(comportnumber, 0x33, 10400);
        }

        public override void StopCommunication()
        {
            m_j1979.stop();
        }

        private void CastDTCInfo(int dtcCode, int dtcState, int dtcCondition1, int dtcCondition2, int dtcCounter)
        {
            if (onDTCInfo != null)
            {
                onDTCInfo(this, new DTCEventArgs(dtcCode, dtcState, dtcCondition1, dtcCondition2, dtcCounter));
            }
        }

    }
}
