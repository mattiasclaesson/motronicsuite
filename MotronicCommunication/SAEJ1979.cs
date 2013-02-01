using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MotronicCommunication
{
    class SAEJ1979
    {
        private const byte READDTC_SID = 0x03;
        private const byte CLEARDTC_SID = 0x04;

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

        public void readDTCs()
        {
            if (!requestDTCs())
            {
                CastDTCInfo("EMPTY");
            }
        }

        public void clearDTC()
        {
            List<byte> msg = new List<byte>() { 0x68, 0x6a, 0xf1, CLEARDTC_SID };
            msg.Add(calculateCS(msg));

            //send the request
            if (!m_dev.send(msg))
            {
                Console.WriteLine("Sending error");
                return;
            }

            List<byte> rcv = m_dev.receive();

            List<byte> data;
            //check if the message is valid
            if (!isMessageValid(rcv, CLEARDTC_SID, 0x00, out data, true))
            {
                Console.WriteLine("Receive error: message not valid");
            }
        }

        private bool requestDTCs()
        {
            //need to send SID = 0x01 PID = 0x01 first
            List<byte> msg = new List<byte>() { 0x68, 0x6a, 0xf1, 0x01, 0x01 };
            msg.Add(calculateCS(msg));

            //send the request, loop if currently sending idle message
            int k = 0;
            for(k = 0; k < 10; ++k)
            {
                if (m_dev.send(msg))
                {
                    break;
                }
                Thread.Sleep(500);
            }

            //timeout...
            if (k == 10)
                return false;

            //this data is not needed...
            List<byte> rcv = m_dev.receive();

            //then request the actual codes
            msg = new List<byte>() { 0x68, 0x6a, 0xf1, READDTC_SID };
            msg.Add(calculateCS(msg));

            //send the request
            if (!m_dev.send(msg))
            {
                return false;
            }
            rcv = m_dev.receive();

            //List<byte> rcv = new List<byte>() { 0x48, 0x6b, 0x10, (byte)(0x40 + READDTC_SID), 0x01, 0x02, 0x13, 0x01, 0x12, 0x11};
            //List<byte> rcv = new List<byte>() { 0x48, 0x6b, 0x10, (byte)(0x40 + READDTC_SID), 0x01, 0x02, 0x00, 0x00, 0x00, 0x00 };
            //rcv.Add(calculateCS(rcv));

            List<byte> data;
            //check if the message is valid
            if (!isMessageValid(rcv, READDTC_SID, 0x00, out data, true))
            {
                Console.WriteLine("Receive error: message not valid");
                return false;
            }

            return parseDTCs(data);
        }

        private bool parseDTCs(List<byte> data)
        {
            List<string> codes = new List<string>();

            bool found = false;
            int i = 0;
            string code;
            while (i < data.Count)
            {
                switch ((data[i] >> 6) & 0x03)
                {
                    case 0:
                        code = "P";
                        break;
                    case 1:
                        code = "C";
                        break;
                    case 2:
                        code = "B";
                        break;
                    case 3:
                        code = "U";
                        break;
                    default:
                        code = "X";
                        return false;
                }

                int n = data[i] >> 4 & 0x03;
                code += n.ToString();

                n = data[i]& 0x0F;
                code += n.ToString();

                n = data[i + 1] >> 4 & 0x0F;
                code += n.ToString();

                n = data[i + 1] & 0x0F;
                code += n.ToString();

                if (code != "P0000")
                {
                    CastDTCInfo(code);
                    found = true;
                }
                i += 2;
            }

            return found;
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

        private bool isMessageValid(List<byte> msg, byte sid, byte pid, out List<byte> data, bool nopid)
        {
            int i = 0;
            data = new List<byte>();
            byte checksum = 0;

            if (msg.Count < 1)
                return false;

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
                        if (nopid)
                        {
                            data.Add(msg[i]);
                            break;
                        }
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

        //byte test = 0;
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

            //List<byte> rcv = new List<byte>(){0x48, 0x6b, 0x10, (byte)(0x40 + sid), pid, ++test, 0x10};
            //rcv.Add(calculateCS(rcv));
            //System.Threading.Thread.Sleep(100);

            List<byte> data;
            //check if the message is valid
            if (!isMessageValid(rcv, sid, pid, out data, false))
            {
                Console.WriteLine("Receive error: message not valid");
                return null;
            }

            success = true;
            return data;
        }

        private void CastDTCInfo(string strcode)
        {
            if (onDTCInfo != null)
            {
                onDTCInfo(this, new ICommunication.DTCEventArgs(strcode));
            }
        }
    }
}


