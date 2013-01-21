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

        private bool m_IsME7 = false;

        public bool IsME7
        {
            get { return m_IsME7; }
            set { m_IsME7 = value; }
        }

        private bool m_IsMotronic44 = false;

        public bool IsMotronic44
        {
            get { return m_IsMotronic44; }
            set { m_IsMotronic44 = value; }
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
            MapConfiguration mc = new MapConfiguration();
            mc.GetCorrectionFactorForMap(this.Identifier, m_IsMotronic44, m_IsLH242, m_IsM18);
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
                MapConfiguration mc = new MapConfiguration();
                mc.GetCorrectionFactorForMap(this.Identifier, m_IsMotronic44, m_IsLH242, m_IsM18);
                m_descr = mc.Description;
                if(mc.Units != "") m_descr += " [" + mc.Units + "]";


                if (!this.IsMotronic44)
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
                else
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
            }
        }

        private MapConfiguration GetCorrectionFactorForMap(int id, bool isM44, bool isLH242)
        {
            MapConfiguration mc = new MapConfiguration();
            mc.Correctionfactor = 1;
            mc.Correctionoffset = 0;
            mc.Description = "Unknown: " + id.ToString("X2");
            switch (id)
            {
                case 0x31:
                    if (isLH242)
                    {
                        mc.Description = "Engine speed";
                        mc.Correctionfactor = 40;
                        mc.Units = "RPM";
                    }
                    break;
                case 0x36: // Battery voltage
                    mc.Correctionfactor = 0.0704F;
                    mc.Description = "Battery voltage";
                    mc.Units = "Volt";
                    break;
                case 0x38: // Coolant temperature
                    mc.Correctionfactor = 1;
                    mc.Correctionoffset = -80;
                    mc.Description = "Coolant temperature";
                    mc.Units = "Degrees celcius";
                    break;
                case 0x3B: // Engine speed (rpm)

                    mc.Correctionfactor = 40;
                    if (isM44)
                    {
                        mc.Correctionfactor = 30;
                    }
                    mc.Correctionoffset = 0;
                    mc.Description = "Engine speed";
                    mc.Units = "RPM";
                    break;
                case 0x40: // Internal load signal
                    mc.Correctionfactor = 0.05F;
                    mc.Correctionoffset = 0;
                    mc.Description = "Internal load signal";
                    mc.Units = "ms";
                    break;
                case 0x4C: // MAF sensor signal (Mass Air Flow)
                    mc.Correctionfactor = 0.01952F;
                    mc.Correctionoffset = 0;
                    mc.Description = "MAF signal";
                    mc.Units = "Volt";
                    break;
                case 0x4F: // Internal load signal LH242
                    if (isLH242)
                    {
                        mc.Correctionfactor = 0.05F;
                        mc.Correctionoffset = 0;
                        mc.Description = "Internal load signal";
                        mc.Units = "ms";
                    }
                    break;
                case 0x55: // Ignition advance
                    if (isLH242)
                    {
                        mc.Description = "Coolant temperature";
                        mc.Units = "Degrees";
                        //mc.Correctionoffset = 70;
                        //mc.Correctionfactor = 2.0714F;
                        //mc.Correctionoffset = -40;
                        //mc.Correctionfactor = 0.55F;

                    }
                    else
                    {
                        mc.Correctionfactor = -0.75F;
                        mc.Correctionoffset = 78;
                        mc.Description = "Ignition advance";
                        mc.Units = "Degrees";
                    }
                    break;
                case 0x67: // IAC valve opening (Idle Air Control)
                //case 0x68:
                    mc.Correctionfactor = 0.0125F;
                    mc.Correctionoffset = 0;
                    mc.Description = "IAC position";
                    mc.Units = "%";
                    break;
                case 0x6F: // Injection time
                //case 0x70:
                    mc.Correctionfactor = 0.001513F;
                    mc.Correctionoffset = 0;
                    mc.Description = "Injection duration";
                    mc.Units = "ms";
                    break;
                case 0x8E: // EVAP duty cycle
                    mc.Correctionfactor = 1.4730F;
                    mc.Correctionoffset = 0;
                    mc.Description = "Injection duration";
                    mc.Units = "ms";
                    break;
                case 0x99: // Airmass
                    mc.Correctionfactor = 1.6F;
                    mc.Correctionoffset = 0;
                    mc.Description = "Airmass";
                    mc.Units = "kg/h";
                    break;
                case 0xB8: // Vehicle speed
                    mc.Correctionfactor = 1;
                    mc.Correctionoffset = 0;
                    mc.Description = "Vehicle speed";
                    mc.Units = "km/h";
                    break;
                case 0xBC: // Turbo duty cycle (solenoid valve)
                    mc.Correctionfactor = 0.391F;
                    mc.Correctionoffset = 0;
                    mc.Description = "Boost valve DC";
                    mc.Units = "%";
                    break;
                case 0x00: // long term fuel trim
                    mc.Correctionfactor = 0.0078125F;
                    mc.Correctionoffset = -128;
                    mc.Description = "Long term fuel trim";
                    mc.Units = "";
                    break;
                case 0x03: // idle fuel trim
                    mc.Correctionfactor = 0.0078125F;
                    mc.Correctionoffset = -128;
                    mc.Description = "Idle fuel trim";
                    mc.Units = "";
                    break;
                case 0x0F: // short term fuel trim
                    mc.Correctionfactor = 0.0078125F;
                    mc.Correctionoffset = -128;
                    mc.Description = "Short term fuel trim";
                    mc.Units = "";
                    break;
                case 0x49: // IAC adaption, Airmass 
                    mc.Correctionfactor = 0.0078125F;
                    mc.Correctionoffset = -128;
                    mc.Description = "IAC airmass adaption";
                    mc.Units = "";
                    break;
                case 0x75: // A/C pressure
                    mc.Correctionfactor = 13.5351F;
                    mc.Correctionoffset = -175.9823F;
                    mc.Description = "A/C Pressure";
                    mc.Units = "kPa";
                    break;
                case 0xC4: // ECT signal sensor
                    mc.Correctionfactor = 1F;
                    mc.Correctionoffset = 0;
                    mc.Description = "ECT signal sensor";
                    mc.Units = "Volt";
                    break;
                case 0xD1: // ECU temperature
                    mc.Correctionfactor = 2.0714F;
                    mc.Correctionoffset = -176.4268F;
                    mc.Description = "ECU temperature";
                    mc.Units = "Degrees celcius";
                    break;
                case 0x22: // TPS angle
                    mc.Correctionfactor = 0.41667F;
                    mc.Correctionoffset = -5.34F;
                    mc.Description = "Throttle position angle";
                    mc.Units = "Degrees";
                    break;
                case 0x2D: // TPS voltage
                    mc.Correctionfactor = 0.01952F;
                    mc.Correctionoffset = 0;
                    mc.Description = "TPS voltage";
                    mc.Units = "Volt";
                    break;
                case 0x70: // front O2 sensor voltage
                    mc.Correctionfactor = 0.0049F;
                    mc.Correctionoffset = -0.1772F;
                    mc.Description = "Front O2 sensor";
                    mc.Units = "Volt";
                    break;
                case 0x79: // rear O2 sensor voltage
                    mc.Correctionfactor = 0.0049F;
                    mc.Correctionoffset = -0.1772F;
                    mc.Description = "Rear O2 sensor";
                    mc.Units = "Volt";
                    break;
                case 0x89: //EGR duty cycle 
                    mc.Correctionfactor = 1.5625F;
                    mc.Correctionoffset = -1.5625F;
                    mc.Description = "EGR duty cycle";
                    mc.Units = "%";
                    break;
                case 0x8D: //EGR temperature 
                    mc.Correctionfactor = 1F;
                    mc.Correctionoffset = -80F;
                    mc.Description = "EGR temperature";
                    mc.Units = "Degrees celcius";
                    break;
                case 0x92: //EGR voltage
                    mc.Correctionfactor = 0.01952F;
                    mc.Correctionoffset = 0;
                    mc.Description = "EGR sensor voltage";
                    mc.Units = "Volt";
                    break;
                case 0xD4: //Accelerometer
                    mc.Correctionfactor = 0.01952F;
                    mc.Correctionoffset = 0;
                    mc.Description = "Accelerometer DC";
                    mc.Units = "DC Volt";
                    break;
                case 0x17: //Rear knock sensor signal
                    mc.Correctionfactor = -1;
                    mc.Correctionoffset = 6;
                    mc.Description = "Rear knock sensor";
                    mc.Units = "";
                    break;
                case 0x19: //Front knock sensor signal
                    mc.Correctionfactor = -1;
                    mc.Correctionoffset = 6;
                    mc.Description = "Front knock sensor";
                    mc.Units = "";
                    break;
                case 0xA2: //Ignition retard by knock
                    mc.Correctionfactor = 0.15F;
                    mc.Correctionoffset = 0;
                    mc.Description = "Ignition retard (knock)";
                    mc.Units = "Degrees";
                    break;
                case 0xAD: //Boost pressure reduce
                    mc.Correctionfactor = 1;
                    mc.Correctionoffset = 0;
                    mc.Description = "Boost pressure reduce (knock)";
                    mc.Units = "";
                    break;
                case 0xB0: //Fuel enrichment
                    mc.Correctionfactor = 1;
                    mc.Correctionoffset = 0;
                    mc.Description = "Fuel enrichment";
                    mc.Units = "";
                    break;
                case 0x44: //Accelerometer AC voltage
                    mc.Correctionfactor = 0.0195F;
                    mc.Correctionoffset = -2.5F;
                    mc.Description = "Accelerometer AC";
                    mc.Units = "Volt";
                    break;
                case 0xC0: //Front lambda max voltage
                    mc.Correctionfactor = 0.0049F;
                    mc.Correctionoffset = -0.1772F;
                    mc.Description = "Front O2 sensor max voltage";
                    mc.Units = "Volt";
                    break;
                case 0xC1: //Front lambda min voltage
                    mc.Correctionfactor = 0.0049F;
                    mc.Correctionoffset = -0.1772F;
                    mc.Description = "Front O2 sensor min voltage";
                    mc.Units = "Volt";
                    break;
                case 0xC9: //Front lambda switching period
                    mc.Correctionfactor = 0.04F;
                    mc.Correctionoffset = 0;
                    mc.Description = "Front O2 sensor switching period";
                    mc.Units = "seconds";
                    break;

            }
            return mc;
/*            else if (this.m_identifier == 0x4F)
            {
                m_factor = 1;
                m_descr = "Unknown";
            }
            else if (this.m_identifier == 0x40)
            {
                // Load
                m_factor = 0.05F;
                //m_factor = 0.075F; ??
                m_descr = "Load (ms inj)";
            }
            else if (this.m_identifier == 0x3B)
            {
                // RPM
                m_factor = 40;
                m_descr = "RPM";
            }
            else if (this.m_identifier == 0x37 || this.m_identifier == 0x38)
            {
                m_factor = 1;
                m_descr = "IAT/ECT";
            }*/

        }

    }
}
