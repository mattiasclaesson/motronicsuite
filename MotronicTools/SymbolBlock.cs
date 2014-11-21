using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MotronicTools
{
    public class SymbolBlock
    {
        private int _start_Address = 0;

        public int Start_Address
        {
            get { return _start_Address; }
            set { _start_Address = value; }
        }
        private int _end_Address = 0;

        public int End_Address
        {
            get { return _end_Address; }
            set { _end_Address = value; }
        }
        private int _length = 0;

        public int Length
        {
            get { return _length; }
            set { _length = value; }
        }
    }
}
