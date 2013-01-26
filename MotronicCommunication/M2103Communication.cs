using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MotronicTools;
using System.Drawing;

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

        public override bool CommunicationRunning
        {
            get { return m_j1979.CommunicationRunning; }
            set { m_j1979.CommunicationRunning = value; }
        }

        public override bool IsWaitingForResponse //rt timer tick needs this
        {
            get { return m_j1979.IsWaitingForResponse; }
            set { m_j1979.IsWaitingForResponse = value; }
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
        public override List<byte> ReadSensor(int pid, out bool success)
        {
            return m_j1979.readSensor(pid, out success);
        }

        public override SymbolCollection ReadSupportedSensors()
        {
            //TODO: supported sensors can be asked from the ECU

            SymbolCollection rt_symbolCollection = new SymbolCollection();
            SymbolHelper shrpm = new SymbolHelper();
            shrpm.Varname = "Engine speed";
            shrpm.Description = "Engine speed";
            shrpm.Start_address = 0x0C; //this is actually the PID, not any memory address
            shrpm.Length = 2; //2 bytes long
            shrpm.CorrectionFactor = 1/4;
            shrpm.MaxValue = 8000;
            shrpm.CorrectionOffset = 0;
            shrpm.Units = "Rpm";
            shrpm.MinValue = 0;
            shrpm.Color = Color.Green;
            rt_symbolCollection.Add(shrpm);

            SymbolHelper shcoolant = new SymbolHelper();
            shcoolant.Varname = "Engine temperature";
            shcoolant.Description = "Engine temperature";
            shcoolant.Start_address = 0x05;
            shcoolant.Length = 1;
            shcoolant.MinValue = -40;
            shcoolant.MaxValue = 120;
            shcoolant.Units = "Degrees";
            shcoolant.CorrectionFactor = 1F;
            shcoolant.CorrectionOffset = -40F;
            shcoolant.Color = Color.Orange;
            rt_symbolCollection.Add(shcoolant);

            SymbolHelper shtps = new SymbolHelper();
            shtps.Varname = "Throttle position";
            shtps.Description = "Throttle position";
            shtps.Start_address = 0x11;
            shtps.Length = 1;
            shtps.MinValue = 0;
            shtps.MaxValue = 100;
            shtps.Units = "TPS %";
            shtps.CorrectionFactor = 0.39216F;
            shtps.CorrectionOffset = 0;
            shtps.Color = Color.DimGray;
            rt_symbolCollection.Add(shtps);

            return rt_symbolCollection;
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
