using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace MotronicSuite
{
    class PartnumberCollection
    {
        public DataTable GeneratePartNumberCollection()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Carmodel");
            dt.Columns.Add("Enginetype");
            dt.Columns.Add("Partnumber");
            dt.Columns.Add("Turbomodel");
            dt.Columns.Add("2300cc");
            dt.Columns.Add("FPT");
            dt.Columns.Add("Turbo");
            dt.Columns.Add("Power");
            dt.Columns.Add("Torque");
            dt.Columns.Add("CarDescription");
            dt.Columns.Add("SoftwareVersion");
            dt.Columns.Add("ECUType");

            PartNumberConverter pnc = new PartNumberConverter();
            ECUInformation ecuinfo = new ECUInformation();

            #region M4.3

            ecuinfo = pnc.GetECUInfo("0261203074", "");
            dt.Rows.Add(ecuinfo.Carmodel.ToString(),
                            ecuinfo.Enginetype.ToString(),
                            "0261203074",
                            ecuinfo.Turbomodel.ToString(),
                            ecuinfo.Is2point3liter.ToString(),
                            ecuinfo.Isfpt.ToString(),
                            ecuinfo.Isturbo.ToString(),
                            ecuinfo.Bhp.ToString(),
                            ecuinfo.Torque.ToString(),
                            ecuinfo.CarDescription,
                            "1037358589", "M4.3");
            dt.Rows.Add(ecuinfo.Carmodel.ToString(),
                            ecuinfo.Enginetype.ToString(),
                            "0261203074",
                            ecuinfo.Turbomodel.ToString(),
                            ecuinfo.Is2point3liter.ToString(),
                            ecuinfo.Isfpt.ToString(),
                            ecuinfo.Isturbo.ToString(),
                            ecuinfo.Bhp.ToString(),
                            ecuinfo.Torque.ToString(),
                            ecuinfo.CarDescription,
                            "1267358075", "M4.3");
            dt.Rows.Add(ecuinfo.Carmodel.ToString(),
                            ecuinfo.Enginetype.ToString(),
                            "0261203074",
                            ecuinfo.Turbomodel.ToString(),
                            ecuinfo.Is2point3liter.ToString(),
                            ecuinfo.Isfpt.ToString(),
                            ecuinfo.Isturbo.ToString(),
                            ecuinfo.Bhp.ToString(),
                            ecuinfo.Torque.ToString(),
                            ecuinfo.CarDescription,
                            "1267355825", "M4.3");
            dt.Rows.Add(ecuinfo.Carmodel.ToString(),
                            ecuinfo.Enginetype.ToString(),
                            "0261203074",
                            ecuinfo.Turbomodel.ToString(),
                            ecuinfo.Is2point3liter.ToString(),
                            ecuinfo.Isfpt.ToString(),
                            ecuinfo.Isturbo.ToString(),
                            ecuinfo.Bhp.ToString(),
                            ecuinfo.Torque.ToString(),
                            ecuinfo.CarDescription,
                            "1267358641", "M4.3");
            dt.Rows.Add(ecuinfo.Carmodel.ToString(),
                            ecuinfo.Enginetype.ToString(),
                            "0261203074",
                            ecuinfo.Turbomodel.ToString(),
                            ecuinfo.Is2point3liter.ToString(),
                            ecuinfo.Isfpt.ToString(),
                            ecuinfo.Isturbo.ToString(),
                            ecuinfo.Bhp.ToString(),
                            ecuinfo.Torque.ToString(),
                            ecuinfo.CarDescription,
                            "2227355825", "M4.3");

            ecuinfo = pnc.GetECUInfo("0261200549", "");
            dt.Rows.Add(ecuinfo.Carmodel.ToString(),
                            ecuinfo.Enginetype.ToString(),
                            "0261200549",
                            ecuinfo.Turbomodel.ToString(),
                            ecuinfo.Is2point3liter.ToString(),
                            ecuinfo.Isfpt.ToString(),
                            ecuinfo.Isturbo.ToString(),
                            ecuinfo.Bhp.ToString(),
                            ecuinfo.Torque.ToString(),
                            ecuinfo.CarDescription,
                            "1267358639", "M4.3");
            dt.Rows.Add(ecuinfo.Carmodel.ToString(),
                            ecuinfo.Enginetype.ToString(),
                            "0261200549",
                            ecuinfo.Turbomodel.ToString(),
                            ecuinfo.Is2point3liter.ToString(),
                            ecuinfo.Isfpt.ToString(),
                            ecuinfo.Isturbo.ToString(),
                            ecuinfo.Bhp.ToString(),
                            ecuinfo.Torque.ToString(),
                            ecuinfo.CarDescription,
                            "1267358073", "M4.3");

            ecuinfo = pnc.GetECUInfo("0261203627", "");
            dt.Rows.Add(ecuinfo.Carmodel.ToString(),
                            ecuinfo.Enginetype.ToString(),
                            "0261203627",
                            ecuinfo.Turbomodel.ToString(),
                            ecuinfo.Is2point3liter.ToString(),
                            ecuinfo.Isfpt.ToString(),
                            ecuinfo.Isturbo.ToString(),
                            ecuinfo.Bhp.ToString(),
                            ecuinfo.Torque.ToString(),
                            ecuinfo.CarDescription,
                            "1267358234", "M4.3");
            dt.Rows.Add(ecuinfo.Carmodel.ToString(),
                            ecuinfo.Enginetype.ToString(),
                            "0261203627",
                            ecuinfo.Turbomodel.ToString(),
                            ecuinfo.Is2point3liter.ToString(),
                            ecuinfo.Isfpt.ToString(),
                            ecuinfo.Isturbo.ToString(),
                            ecuinfo.Bhp.ToString(),
                            ecuinfo.Torque.ToString(),
                            ecuinfo.CarDescription,
                            "2227355828", "M4.3");

            ecuinfo = pnc.GetECUInfo("0261203628", "");
            dt.Rows.Add(ecuinfo.Carmodel.ToString(),
                            ecuinfo.Enginetype.ToString(),
                            "0261203628",
                            ecuinfo.Turbomodel.ToString(),
                            ecuinfo.Is2point3liter.ToString(),
                            ecuinfo.Isfpt.ToString(),
                            ecuinfo.Isturbo.ToString(),
                            ecuinfo.Bhp.ToString(),
                            ecuinfo.Torque.ToString(),
                            ecuinfo.CarDescription,
                            "2227355802", "M4.3");

            ecuinfo = pnc.GetECUInfo("0261203626", "");
            dt.Rows.Add(ecuinfo.Carmodel.ToString(),
                            ecuinfo.Enginetype.ToString(),
                            "0261203626",
                            ecuinfo.Turbomodel.ToString(),
                            ecuinfo.Is2point3liter.ToString(),
                            ecuinfo.Isfpt.ToString(),
                            ecuinfo.Isturbo.ToString(),
                            ecuinfo.Bhp.ToString(),
                            ecuinfo.Torque.ToString(),
                            ecuinfo.CarDescription,
                            "1267358233", "M4.3");
            dt.Rows.Add(ecuinfo.Carmodel.ToString(),
                            ecuinfo.Enginetype.ToString(),
                            "0261203626",
                            ecuinfo.Turbomodel.ToString(),
                            ecuinfo.Is2point3liter.ToString(),
                            ecuinfo.Isfpt.ToString(),
                            ecuinfo.Isturbo.ToString(),
                            ecuinfo.Bhp.ToString(),
                            ecuinfo.Torque.ToString(),
                            ecuinfo.CarDescription,
                            "2227355827", "M4.3");

            ecuinfo = pnc.GetECUInfo("0261204134", "");
            dt.Rows.Add(ecuinfo.Carmodel.ToString(),
                            ecuinfo.Enginetype.ToString(),
                            "0261204134",
                            ecuinfo.Turbomodel.ToString(),
                            ecuinfo.Is2point3liter.ToString(),
                            ecuinfo.Isfpt.ToString(),
                            ecuinfo.Isturbo.ToString(),
                            ecuinfo.Bhp.ToString(),
                            ecuinfo.Torque.ToString(),
                            ecuinfo.CarDescription,
                            "1037358586", "M4.3");
            dt.Rows.Add(ecuinfo.Carmodel.ToString(),
                            ecuinfo.Enginetype.ToString(),
                            "0261204134",
                            ecuinfo.Turbomodel.ToString(),
                            ecuinfo.Is2point3liter.ToString(),
                            ecuinfo.Isfpt.ToString(),
                            ecuinfo.Isturbo.ToString(),
                            ecuinfo.Bhp.ToString(),
                            ecuinfo.Torque.ToString(),
                            ecuinfo.CarDescription,
                            "2537355830", "M4.3");

            ecuinfo = pnc.GetECUInfo("0261204225", "");
            dt.Rows.Add(ecuinfo.Carmodel.ToString(),
                            ecuinfo.Enginetype.ToString(),
                            "0261204225",
                            ecuinfo.Turbomodel.ToString(),
                            ecuinfo.Is2point3liter.ToString(),
                            ecuinfo.Isfpt.ToString(),
                            ecuinfo.Isturbo.ToString(),
                            ecuinfo.Bhp.ToString(),
                            ecuinfo.Torque.ToString(),
                            ecuinfo.CarDescription,
                            "1037355277", "M4.3");

            ecuinfo = pnc.GetECUInfo("0261203852", "");
            dt.Rows.Add(ecuinfo.Carmodel.ToString(),
                            ecuinfo.Enginetype.ToString(),
                            "0261203852",
                            ecuinfo.Turbomodel.ToString(),
                            ecuinfo.Is2point3liter.ToString(),
                            ecuinfo.Isfpt.ToString(),
                            ecuinfo.Isturbo.ToString(),
                            ecuinfo.Bhp.ToString(),
                            ecuinfo.Torque.ToString(),
                            ecuinfo.CarDescription,
                            "1267358985", "M4.3");

            ecuinfo = pnc.GetECUInfo("0261203851", "");
            dt.Rows.Add(ecuinfo.Carmodel.ToString(),
                            ecuinfo.Enginetype.ToString(),
                            "0261203851",
                            ecuinfo.Turbomodel.ToString(),
                            ecuinfo.Is2point3liter.ToString(),
                            ecuinfo.Isfpt.ToString(),
                            ecuinfo.Isturbo.ToString(),
                            ecuinfo.Bhp.ToString(),
                            ecuinfo.Torque.ToString(),
                            ecuinfo.CarDescription,
                            "1267358984", "M4.3");

            ecuinfo = pnc.GetECUInfo("0261203962", "");
            dt.Rows.Add(ecuinfo.Carmodel.ToString(),
                            ecuinfo.Enginetype.ToString(),
                            "0261203962",
                            ecuinfo.Turbomodel.ToString(),
                            ecuinfo.Is2point3liter.ToString(),
                            ecuinfo.Isfpt.ToString(),
                            ecuinfo.Isturbo.ToString(),
                            ecuinfo.Bhp.ToString(),
                            ecuinfo.Torque.ToString(),
                            ecuinfo.CarDescription,
                            "2227355651", "M4.3");
            dt.Rows.Add(ecuinfo.Carmodel.ToString(),
                            ecuinfo.Enginetype.ToString(),
                            "0261203962",
                            ecuinfo.Turbomodel.ToString(),
                            ecuinfo.Is2point3liter.ToString(),
                            ecuinfo.Isfpt.ToString(),
                            ecuinfo.Isturbo.ToString(),
                            ecuinfo.Bhp.ToString(),
                            ecuinfo.Torque.ToString(),
                            ecuinfo.CarDescription,
                            "1267358965", "M4.3");

            ecuinfo = pnc.GetECUInfo("0261203189", "");
            dt.Rows.Add(ecuinfo.Carmodel.ToString(),
                            ecuinfo.Enginetype.ToString(),
                            "0261203189",
                            ecuinfo.Turbomodel.ToString(),
                            ecuinfo.Is2point3liter.ToString(),
                            ecuinfo.Isfpt.ToString(),
                            ecuinfo.Isturbo.ToString(),
                            ecuinfo.Bhp.ToString(),
                            ecuinfo.Torque.ToString(),
                            ecuinfo.CarDescription,
                            "", "M4.3");

            #endregion

            #region M4.4
            AddPartNumber(dt, pnc, ecuinfo, "0261204305_1037358409.BIN");
            AddPartNumber(dt, pnc, ecuinfo, "0261204442_1037357513.BIN");
            AddPartNumber(dt, pnc, ecuinfo, "0261204442_1037358966.BIN");
            AddPartNumber(dt, pnc, ecuinfo, "0261204443_1037358967.BIN");
            AddPartNumber(dt, pnc, ecuinfo, "0261204444_1037357515.BIN");
            AddPartNumber(dt, pnc, ecuinfo, "0261204444_1037358968.BIN");
            AddPartNumber(dt, pnc, ecuinfo, "0261204446_1037357516.BIN");
            AddPartNumber(dt, pnc, ecuinfo, "0261204446_1037358980.BIN");
            AddPartNumber(dt, pnc, ecuinfo, "0261204448_1037357518.BIN");
            AddPartNumber(dt, pnc, ecuinfo, "0261204448_1037357755.BIN");
            AddPartNumber(dt, pnc, ecuinfo, "0261204449_1037357519.BIN");
            AddPartNumber(dt, pnc, ecuinfo, "0261204449_1037357756.BIN");
            AddPartNumber(dt, pnc, ecuinfo, "0261204449_1037359866.BIN");
            AddPartNumber(dt, pnc, ecuinfo, "0261204451_1037357521.BIN");
            AddPartNumber(dt, pnc, ecuinfo, "0261204456_1037357522.BIN");
            AddPartNumber(dt, pnc, ecuinfo, "0261204456_1037359875.BIN");
            AddPartNumber(dt, pnc, ecuinfo, "0261204457_1037357523.BIN");
            AddPartNumber(dt, pnc, ecuinfo, "0261204457_1037358989.BIN");
            AddPartNumber(dt, pnc, ecuinfo, "0261204457_1037359876.BIN");
            AddPartNumber(dt, pnc, ecuinfo, "0261204459_1037357525.BIN");
            AddPartNumber(dt, pnc, ecuinfo, "0261204459_1037358991.BIN");
            AddPartNumber(dt, pnc, ecuinfo, "0261204606_1037357287.BIN");
            AddPartNumber(dt, pnc, ecuinfo, "0261204606_1037359872.BIN");
            AddPartNumber(dt, pnc, ecuinfo, "0261204607_1037357780.BIN");
            AddPartNumber(dt, pnc, ecuinfo, "0261204607_1037359868.BIN");
            AddPartNumber(dt, pnc, ecuinfo, "0261204608_1037357527.BIN");
            AddPartNumber(dt, pnc, ecuinfo, "0261204609_1037357528.BIN");
            AddPartNumber(dt, pnc, ecuinfo, "0261204609_1037359880.BIN");
            AddPartNumber(dt, pnc, ecuinfo, "0261204612_1037357531.BIN");
            AddPartNumber(dt, pnc, ecuinfo, "0261204765_1037357759.BIN");
            AddPartNumber(dt, pnc, ecuinfo, "0261204765_1037359878.BIN");
            #endregion

            return dt;
        }

        private void AddPartNumber(DataTable dt, PartNumberConverter pnc, ECUInformation ecuinfo, string filename)
        {
            char[] sep = new char[1];
            sep.SetValue('_', 0);
            if (filename.Length > 0)
            {
                string[] values = filename.Split(sep);
                if (values.Length >= 2)
                {
                    string partnumber = (string)values.GetValue(0);
                    string swversion = (string)values.GetValue(1);
                    swversion = swversion.Replace(".BIN", "");
                    ecuinfo = pnc.GetECUInfo(partnumber, "");
                    dt.Rows.Add(ecuinfo.Carmodel.ToString(), ecuinfo.Enginetype.ToString(), partnumber, ecuinfo.Turbomodel.ToString(), ecuinfo.Is2point3liter.ToString(), ecuinfo.Isfpt.ToString(), ecuinfo.Isturbo.ToString(), ecuinfo.Bhp.ToString(), ecuinfo.Torque.ToString(), ecuinfo.CarDescription, swversion, "M4.4");
                }
            }

        }

    }
}
