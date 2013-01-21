using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MotronicCommunication
{
    class SAEJ1979
    {
        private IKLineDevice m_dev = new DumbKLineDevice();

        public void initialize(string comportnumber, int ecuaddr, int baudrate)
        {
            m_dev.slowInit(comportnumber, ecuaddr, baudrate);
        }
    }
}
