using System;
using System.Collections.Generic;
using System.Text;

namespace MotronicTools
{
    public class AxisHelper
    {
        private string m_descr = string.Empty;

        public string Descr
        {
            get { return m_descr; }
            set { m_descr = value; }
        }

        private int _M44DamosID = 0;

        public int M44DamosID
        {
            get { return _M44DamosID; }
            set { _M44DamosID = value; }
        }

        private bool m_IsLH242 = false;

        public bool IsLH242
        {
            get { return m_IsLH242; }
            set { m_IsLH242 = value; }
        }

        private bool m_IsM18 = false;

        public bool IsM18
        {
            get { return m_IsM18; }
            set { m_IsM18 = value; }
        }

        private bool m_IsM210 = false;

        public bool IsM210
        {
            get { return m_IsM210; }
            set { m_IsM210 = value; }
        }

        private bool m_IsMotronic44 = false;

        public bool IsMotronic44
        {
            get { return m_IsMotronic44; }
            set { m_IsMotronic44 = value; }
        }

        private bool m_IsME7 = false;

        public bool IsME7
        {
            get { return m_IsME7; }
            set { m_IsME7 = value; }
        }

        private bool m_IsME96 = false;

        public bool IsME96
        {
            get { return m_IsME96; }
            set { m_IsME96 = value; }
        }

        private int m_addressinfile = 0;

        public int Addressinfile
        {
            get { return m_addressinfile; }
            set { m_addressinfile = value; }
        }

        private int[] m_values = new int[1];

        public int[] Values
        {
            get { return m_values; }
            set { m_values = value; }
        }

        private int m_identifier = 0;

        public int Identifier
        {
            get { return m_identifier; }
            set { m_identifier = value; }
        }

        private int m_length;

        public int Length
        {
            get { return m_length; }
            set { m_length = value; }
        }

        private int[] m_calculcatedIntValues = new int[1];

        public int[] CalculcatedIntValues
        {
            get { return m_calculcatedIntValues; }
            set { m_calculcatedIntValues = value; }
        }


        private float[] m_calculcatedValues = new float[1];

        public float[] CalculcatedValues
        {
            get { return m_calculcatedValues; }
            set { m_calculcatedValues = value; }
        }

        private float m_maxvalue = 0;

        public float Maxvalue
        {
            get { return m_maxvalue; }
            set { m_maxvalue = value; }
        }

        public string ValuesAsString
        {
            get
            {
                string retval = string.Empty;
                foreach (float val in m_calculcatedValues)
                {
                    retval += val.ToString("F2") + " - ";
                }
                return retval;
            }
        }

        public int[] CalculateOriginalValues(float[] newvalues)
        {
            int[] retval = new int[newvalues.Length];

            MapConfiguration mc;
            if (m_IsM210)
            {
                mc = new MapConfigurationM210();
            }
            else
            {
                mc = new MapConfiguration();
            }

            mc.GetCorrectionFactorForMap(this.Identifier, m_IsMotronic44, m_IsLH242, m_IsM18, m_IsM210);
            int[] tempvals = new int[newvalues.Length];
            
            for (int i = 0; i < newvalues.Length; i++)
            {
                float val = newvalues[i];
                val -= mc.Correctionoffset;
                val /= mc.Correctionfactor;
                tempvals.SetValue(Convert.ToInt32(val), i);
            }

            foreach (int v in tempvals)
            {
                Console.WriteLine("phase1: " + v.ToString());
            }

            // 256 - x = 175
            // x = -175 + 256

            int _bmaxValue = (int)tempvals.GetValue(tempvals.Length -1);
            _bmaxValue = 256 - _bmaxValue;
            retval[tempvals.Length - 1] = _bmaxValue;
            int bPrevValue = _bmaxValue;
            for (int p = tempvals.Length - 2; p >= 0; p--)
            {
                int diff = tempvals[p + 1] - tempvals[p];
                retval.SetValue(diff, p);
            }
            foreach (int v in retval)
            {
                Console.WriteLine("phase2: " + v.ToString());
            }
            return retval;
        }

        public void CalculateRealValues()
        {
            float m_factor = 1;
            if (this.Length > 0)
            {
                // depends on identifier
                /*
                 * 3B is Engine speed
                 * 40 is Engine load
                 * 37 or 38 is ECT or IAT
                 * * */
                m_calculcatedValues = new float[this.m_length];
                m_calculcatedIntValues = new int[this.m_length];

                MapConfiguration mc;
                if (m_IsM210)
                {
                    mc = new MapConfigurationM210();
                }
                else
                {
                    mc = new MapConfiguration();
                }


                mc.GetCorrectionFactorForMap(this.Identifier, m_IsMotronic44, m_IsLH242, m_IsM18, m_IsM210);
                m_descr = mc.Description;
                if(mc.Units != "") m_descr += " [" + mc.Units + "]";

                if(this.IsMotronic44)
                {
                    // do all * correction factor + correction offset
                    for (int p = 0; p < m_length; p++)
                    {
                        float calcvalue = (float)Convert.ToDouble(m_values.GetValue(p));
                        calcvalue *= mc.Correctionfactor;
                        calcvalue += mc.Correctionoffset;
                        m_calculcatedValues.SetValue(calcvalue, p);
                        m_calculcatedIntValues.SetValue(Convert.ToInt32(calcvalue), p);
                    }
                }
                else
                {
                    //GetCorrectionFactorForMap(this.m_identifier);
                    // get the max value in the list, last value
                    float max_value = (int)m_values.GetValue(this.m_length - 1);
                    //max_value += mc.Correctionoffset;
                    //(256-100)x40

                    float max_calculated_value = ((float)(256 - max_value) * mc.Correctionfactor);
                    float actual_max_calculated_value = max_calculated_value + mc.Correctionoffset;
                    /*if (this.m_identifier == 0x40)
                    {
                        // different for load
                        max_calculated_value = ((float)(max_value) * m_factor);
                    }*/
                    m_calculcatedValues.SetValue((float)Convert.ToDouble(actual_max_calculated_value.ToString("F2")), this.m_length - 1);
                    m_calculcatedIntValues.SetValue(Convert.ToInt32(actual_max_calculated_value), this.m_length - 1);
                    float m_prev_value = max_calculated_value;
                    for (int p = this.m_length - 2; p >= 0; p--)
                    {
                        int this_value = (int)m_values.GetValue(p);

                        if (this.m_addressinfile == 0xefbc) Console.WriteLine("value: " + this_value.ToString("X2"));

                        float this_calculated_value = m_prev_value - ((float)this_value * mc.Correctionfactor);
                        if (this.m_addressinfile == 0xefbc) Console.WriteLine("prev_value: " + m_prev_value.ToString("F2"));
                        if (this.m_addressinfile == 0xefbc) Console.WriteLine("this_calculated_value: " + this_calculated_value.ToString("F2"));
                        float actually_calculated_value = this_calculated_value + mc.Correctionoffset;
                        if (this.m_addressinfile == 0xefbc) Console.WriteLine("actually_calculated_value: " + actually_calculated_value.ToString("F2"));
                        m_calculcatedValues.SetValue((float)Convert.ToDouble(actually_calculated_value.ToString("F2")), p);
                        m_calculcatedIntValues.SetValue(Convert.ToInt32(actually_calculated_value), p);
                        m_prev_value = this_calculated_value;
                        //6240-(16x40)
                    }
                }
            }
        }
    }
}
