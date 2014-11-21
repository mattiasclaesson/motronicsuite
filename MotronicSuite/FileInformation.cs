using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MotronicTools;

namespace MotronicSuite
{
    public class FileInformation
    {
        private SymbolCollection m_symbols = new SymbolCollection();

        public SymbolCollection Symbols
        {
            get { return m_symbols; }
            set { m_symbols = value; }
        }
        private AxisCollection m_axis = new AxisCollection();

        public AxisCollection Axis
        {
            get { return m_axis; }
            set { m_axis = value; }
        }
    }
}
