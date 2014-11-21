using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MotronicSuite
{
    class SymbolTranslator
    {
        public string TranslateSymbolToHelpText(string symbolname, out string helptext, out string category, out string subcategory)
        {
            if (symbolname.EndsWith("!")) symbolname = symbolname.Substring(0, symbolname.Length - 1);
            helptext = "";
            category = "";
            subcategory = "";
            string description = "";
            switch (symbolname)
            {
                case "Boost map":
                    helptext = description = "Boost target map";
                    break;
            }
            return description;
        }

    }
}
