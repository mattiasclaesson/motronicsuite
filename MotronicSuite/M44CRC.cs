using System;
using System.Collections.Generic;
using System.Text;

namespace MotronicSuite
{
    class M44CRC
    {
        private uint volvocrc1 = 0;

        public uint Volvocrc1
        {
            get { return volvocrc1; }
            set { volvocrc1 = value; }
        }
        private uint volvocrc2 = 0;

        public uint Volvocrc2
        {
            get { return volvocrc2; }
            set { volvocrc2 = value; }
        }
    }
}
