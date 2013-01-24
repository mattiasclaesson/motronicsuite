using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MotronicTools;
using System.Drawing;

namespace MotronicCommunication
{
    class SAEJ1979
    {
        private DumbKLineDevice m_dev = new DumbKLineDevice();

        public event ICommunication.DTCInfo onDTCInfo;
        public event ICommunication.ECUInfo onECUInfo
        {
            add { m_dev.onECUInfo += value; }
            remove { m_dev.onECUInfo -= value; }
        }
        public event ICommunication.StatusChanged onStatusChanged
        {
            add { m_dev.onStatusChanged += value; }
            remove { m_dev.onStatusChanged -= value; }
        }

        private bool _communicationRunning = false; //important
        public bool CommunicationRunning
        {
            get { return _communicationRunning; }
            set { _communicationRunning = value; }
        }

        public void initialize(string comportnumber, int ecuaddr, int baudrate)
        {
            List<byte> msg = new List<byte>() { 0x68, 0x6a, 0xf1, 0x01, 0x00 };
            msg.Add(calculateCS(msg));

            m_dev.setIdleMessage(msg);

            if (m_dev.slowInit(comportnumber, ecuaddr, baudrate))
            {
                _communicationRunning = true;
            }
        }

        public void stop()
        {
            m_dev.stop();
            _communicationRunning = false;
        }

        public int readSensor(int pid)
        {
            return sendRequest((byte)0x01, (byte)pid)[5];
        }

        public SymbolCollection getSupportedSensors()
        {
            //TODO: supported sensors can be asked from the ECU

            SymbolCollection rt_symbolCollection = new SymbolCollection();
            SymbolHelper shrpm = new SymbolHelper();
            shrpm.Varname = "Engine speed";
            shrpm.Description = "Engine speed";
            shrpm.Start_address = 0x0C; //this is actually the PID, not any memory address
            shrpm.Length = 1;
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

        private byte calculateCS(List<byte> buf)
        {
	        byte cs = 0;
	        int i;

            for (i = 0; i < buf.Count; ++i)
            {
                cs += buf[i];
            }

	        return cs;
        }

        private List<byte> sendRequest(byte sid, byte pid)
        {
            List<byte> msg = new List<byte>() { 0x68, 0x6a, 0xf1, sid, pid};
            msg.Add(calculateCS(msg));

            //send the request
            m_dev.send(msg);

            //wait for a response
            List<byte> rcv = m_dev.receive();

            //check if the message is valid

            return rcv;
        }
    }
}
