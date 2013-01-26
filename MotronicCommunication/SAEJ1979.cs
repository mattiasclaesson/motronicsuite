using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MotronicCommunication
{
    class SAEJ1979
    {
        private DumbKLineDevice m_dev;

        public SAEJ1979()
        {
            m_dev = new DumbKLineDevice();
        }

        public SAEJ1979(DumbKLineDevice dev)
        {
            m_dev = dev;
        }

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

        private bool _IsWaitingForResponse = false;
        public bool IsWaitingForResponse
        {
            get { return _IsWaitingForResponse; }
            set { _IsWaitingForResponse = value; }
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

        public List<byte> readSensor(int pid, out bool success)
        {
            return sendRequest((byte)0x01, (byte)pid, out success);
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

        private bool isMessageValid(List<byte> msg, byte sid, byte pid, out List<byte> data)
        {
            int i = 0;

            byte checksum = 0;
            data = new List<byte>();
            for (i = 0; i < msg.Count - 1; ++i)
            {
                switch (i)
                {
                    case 0:
                        if (msg[i] != 0x48) return false;
                        break;
                    case 1:
                        if (msg[i] != 0x6b) return false;
                        break;
                    case 2:
                        //dont care about address
                        break;
                    case 3:
                        if (msg[i] != sid + 0x40) return false;
                        break;
                    case 4:
                        if (msg[i] != pid) return false;
                        break;
                    default:
                        //extract the actual data
                        data.Add(msg[i]);
                        break;
                }
                checksum += msg[i];
            }

            if (msg[i] != checksum)
            {
                Console.WriteLine("Checksum wrong!");
                return false;
            }

            return true;

        }

        private List<byte> sendRequest(byte sid, byte pid, out bool success)
        {
            success = false;

            List<byte> msg = new List<byte>() { 0x68, 0x6a, 0xf1, sid, pid};
            msg.Add(calculateCS(msg));

            //send the request
            if (!m_dev.send(msg))
            {
                return null;
            }

            //wait for a response
            List<byte> rcv = m_dev.receive();

            List<byte> data;
            //check if the message is valid
            if (!isMessageValid(rcv, sid, pid, out data))
            {
                Console.WriteLine("Receive error: message not valid");
                return null;
            }

            success = true;
            return data;
        }
    }
}
