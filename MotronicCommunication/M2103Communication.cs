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

        public override event ICommunication.DTCInfo onDTCInfo
        {
            add { m_j1979.onDTCInfo += value; }
            remove { m_j1979.onDTCInfo -= value; }
        }
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
            m_j1979.readDTCs();
        }

        public override void ClearDTC(int timeout)
        {
            m_j1979.clearDTC();
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
            m_j1979.initialize(comportnumber, INIT_ECU_ADDR, ECU_BAUDRATE);
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
            shrpm.CorrectionFactor = 0.25F;
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

            SymbolHelper shignadv = new SymbolHelper();
            shignadv.Varname = "Ignition advance";
            shignadv.Description = "Ignition advance";
            shignadv.Start_address = 0x0E;
            shignadv.Length = 1;
            shignadv.MinValue = -20;
            shignadv.MaxValue = 40;
            shignadv.Units = "Degrees";
            shignadv.CorrectionFactor = 0.5F;
            shignadv.CorrectionOffset = -128 * 0.5F;
            shignadv.Color = Color.LightBlue;
            rt_symbolCollection.Add(shignadv);

            SymbolHelper shload = new SymbolHelper();
            shload.Varname = "Engine load";
            shload.Description = "Engine load";
            shload.Start_address = 0x04;
            shload.Length = 1;
            shload.Units = "%";
            shload.MinValue = 0;
            shload.MaxValue = 100;
            shload.CorrectionFactor = 0.39216F;
            shload.CorrectionOffset = 0;
            shload.Color = Color.Red;
            rt_symbolCollection.Add(shload);

            SymbolHelper shafr = new SymbolHelper();
            shafr.Varname = "Air flow rate";
            shafr.Description = "Air flow rate";
            shafr.Start_address = 0x10;
            shafr.Length = 2;
            shafr.Units = "g/s";
            shafr.MinValue = 0;
            shafr.MaxValue = 656;
            shafr.CorrectionFactor = 0.01F;
            shafr.CorrectionOffset = 0;
            shafr.Color = Color.Ivory;
            rt_symbolCollection.Add(shafr);

            SymbolHelper shlambda = new SymbolHelper();
            shlambda.Varname = "Lambda voltage";
            shlambda.Description = "Lambda voltage";
            shlambda.Start_address = 0x14;
            shlambda.Length = 1;
            shlambda.Units = "V";
            shlambda.MinValue = 0;
            shlambda.MaxValue = 1.3F;
            shlambda.CorrectionFactor = 0.005F;
            shlambda.CorrectionOffset = 0;
            shlambda.Color = Color.Ivory;
            rt_symbolCollection.Add(shlambda);

            SymbolHelper shvspeed = new SymbolHelper();
            shvspeed.Varname = "Vehicle speed";
            shvspeed.Description = "Vehicle speed";
            shvspeed.Start_address = 0x0d;
            shvspeed.Length = 1;
            shvspeed.Units = "km/h";
            shvspeed.MinValue = 0;
            shvspeed.MaxValue = 210;
            shvspeed.CorrectionFactor = 1;
            shvspeed.CorrectionOffset = 0;
            shvspeed.Color = Color.Ivory;
            rt_symbolCollection.Add(shvspeed);

            SymbolHelper shshort = new SymbolHelper();
            shshort.Varname = "Short term trim";
            shshort.Description = "Short term trim";
            shshort.Start_address = 0x06;
            shshort.Length = 1;
            shshort.Units = "%";
            shshort.MinValue = -100;
            shshort.MaxValue = 100;
            shshort.CorrectionFactor = 0.78125F;
            shshort.CorrectionOffset = -128F * 0.78125F;
            shshort.Color = Color.Ivory;
            rt_symbolCollection.Add(shshort);

            SymbolHelper shlong = new SymbolHelper();
            shlong.Varname = "Long term trim";
            shlong.Description = "Long term trim";
            shlong.Start_address = 0x07;
            shlong.Length = 1;
            shlong.Units = "%";
            shlong.MinValue = -100;
            shlong.MaxValue = 100;
            shlong.CorrectionFactor = 0.78125F;
            shlong.CorrectionOffset = -128F * 0.78125F;
            shlong.Color = Color.Ivory;
            rt_symbolCollection.Add(shlong);

            return rt_symbolCollection;
        }

    }
}
