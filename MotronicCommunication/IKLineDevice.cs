using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MotronicCommunication
{
    abstract public class IKLineDevice
    {
        public abstract bool slowInit(string comportnumber, int ecuaddr, int baudrate);
    }
}
