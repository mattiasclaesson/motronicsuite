using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MotronicCommunication
{
    class SAEJ1979
    {
        private DumbKLineDevice m_dev = new DumbKLineDevice();

        public void initialize(string comportnumber, int ecuaddr, int baudrate)
        {
            List<byte> msg = new List<byte>() { 0x68, 0x6a, 0xf1, 0x01, 0x00 };
            msg.Add(calculateCS(msg));

            m_dev.setIdleMessage(msg);
            m_dev.slowInit(comportnumber, ecuaddr, baudrate);
        }

        public void requestDiagnosticData(int pid)
        {
            sendRequest((byte)0x01, (byte)pid);
        }

        public void getSupportedPIDs()
        {

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

        private void sendRequest(byte sid, byte pid)
        {
            List<byte> msg = new List<byte>() { 0x68, 0x6a, 0xf1, sid, pid};
            msg.Add(calculateCS(msg));

            //send the request
            m_dev.send(msg);

            //wait for a response
            //List<byte> rcv = m_dev.receive();

            //check if the message is valid
        }
    }
}
