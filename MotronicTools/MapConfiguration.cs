using System;
using System.Collections.Generic;
using System.Text;

namespace MotronicTools
{
    class MapConfiguration
    {
        private float m_correctionfactor = 1;

        public float Correctionfactor
        {
            get { return m_correctionfactor; }
            set { m_correctionfactor = value; }
        }
        private float m_correctionoffset = 0;

        public float Correctionoffset
        {
            get { return m_correctionoffset; }
            set { m_correctionoffset = value; }
        }
        private string m_description = string.Empty;

        public string Description
        {
            get { return m_description; }
            set { m_description = value; }
        }

        private string m_units = string.Empty;

        public string Units
        {
            get { return m_units; }
            set { m_units = value; }
        }

        public void GetCorrectionFactorForMap(int id, bool isM44, bool isLH242, bool isM18)
        {
            m_correctionfactor = 1;
            m_correctionoffset = 0;
            m_description = "Unknown: " + id.ToString("X2");
            switch (id)
            {
                case 0xCA: // Internal load signal M1.8
                    m_correctionfactor = 0.05F;
                    m_correctionoffset = 0;
                    m_description = "Internal load signal";
                    m_units = "ms";
                    break;
                case 0x3C:
                    if (isM18)
                    {
                        // coolant temperature
                        m_correctionfactor = 1;
                        m_correctionoffset = -80;
                        m_description = "Coolant temperature";
                        m_units = "Degrees celcius";
                    }
                    break;
                case 0x39:
                    if (isM18)
                    {
                        // batt. voltage
                        m_correctionfactor = 0.064257F;
                        //0,064257028112449799196787148594378
                        m_description = "Battery voltage";
                        m_units = "Volt";
                    }
                    break;
                case 0x4B:
                    if (isM18)
                    {
                        // rpm
                        m_correctionfactor = 40;
                        m_correctionoffset = 0;
                        m_description = "Engine speed";
                        m_units = "RPM";
                    }
                    break;
                case 0x04:
                    // unknown, maybe IAT
                    m_correctionfactor = 1;
                    m_correctionoffset = -80;
                    m_description = "Intake Air temperature";
                    m_units = "Degrees celcius";
                    break;
                case 0x30:
                    // MAF signal ??
                    m_correctionfactor = 0.41667F;
                    //m_correctionoffset = -5.34F;
                    m_description = "Throttle position angle";
                    m_units = "Degrees";
                    //m_description = "MAF";

                    break;
                case 0x31:
                    if (isLH242)
                    {
                        m_description = "Engine speed";
                        m_correctionfactor = 40;
                        m_units = "RPM";
                    }
                    else
                    {
                        m_correctionfactor = 0.433546F; //?
                        m_description = "Throttle position angle";
                        m_units = "Degrees";
                    }
                    break;
                case 0x36: // Battery voltage
                    m_correctionfactor = 0.0704F;
                    m_description = "Battery voltage";
                    m_units = "Volt";
                    break;
                case 0x38: // Coolant temperature
                    m_correctionfactor = 1;
                    m_correctionoffset = -80;
                    m_description = "Coolant temperature";
                    m_units = "Degrees celcius";
                    break;
                case 0x3B: // Engine speed (rpm)
                    m_correctionfactor = 40;
                    if (isM44)
                    {
                        m_correctionfactor = 30;
                    }
                    m_correctionoffset = 0;
                    m_description = "Engine speed";
                    m_units = "RPM";
                    break;
                case 0x40: // Internal load signal
                case 0xF8B1:
                    m_correctionfactor = 0.05F;
                    if (isM44)
                    {
                        m_correctionfactor = 0.048F;
                    }
                    m_correctionoffset = 0;
                    m_description = "Internal load signal";
                    m_units = "ms";
                    break;
                case 0x4C: // MAF sensor signal (Mass Air Flow)
                    m_correctionfactor = 0.01952F;
                    m_correctionoffset = 0;
                    m_description = "MAF signal";
                    m_units = "Volt";
                    break;
                case 0x4F: // Internal load signal LH242
                    if (isLH242)
                    {
                        m_correctionfactor = 0.05F;
                        m_correctionoffset = 0;
                        m_description = "Internal load signal";
                        m_units = "ms";
                    }
                    break;
                case 0x55: // Ignition advance
                    if (isLH242)
                    {
                        m_description = "Coolant temperature";
                        //m_correctionoffset = -40;
                        //m_correctionfactor = 0.55F;
                        //m_correctionfactor = 2.0714F;
                        //m_correctionoffset = -176.4268F;

                        m_units = "Degrees";
                    }
                    else if (isM18)
                    {
                        m_correctionfactor = -0.375F;
                        m_correctionoffset = 60;
                        m_description = "Ignition advance";
                        m_units = "Degrees";
                    }
                    else
                    {
                        m_correctionfactor = -0.75F;
                        m_correctionoffset = 78;
                        m_description = "Ignition advance";
                        m_units = "Degrees";
                    }
                    break;
                case 0x67: // IAC valve opening (Idle Air Control)
                    //case 0x68:
                    m_correctionfactor = 0.0125F;
                    m_correctionoffset = 0;
                    m_description = "IAC position";
                    m_units = "%";
                    break;
                case 0x6F: // Injection time
                    //case 0x70:
                    m_correctionfactor = 0.001513F;
                    m_correctionoffset = 0;
                    m_description = "Injection duration";
                    m_units = "ms";
                    break;
                case 0x8E: // EVAP duty cycle
                    m_correctionfactor = 1.4730F;
                    m_correctionoffset = 0;
                    m_description = "Injection duration";
                    m_units = "ms";
                    break;
                case 0x99: // Airmass
                    m_correctionfactor = 1.6F;
                    m_correctionoffset = 0;
                    m_description = "Airmass";
                    m_units = "kg/h";
                    break;
                case 0xB8: // Vehicle speed
                    m_correctionfactor = 1;
                    m_correctionoffset = 0;
                    m_description = "Vehicle speed";
                    m_units = "km/h";
                    break;
                case 0xBC: // Turbo duty cycle (solenoid valve)
                    m_correctionfactor = 0.391F;
                    m_correctionoffset = 0;
                    m_description = "Boost valve DC";
                    m_units = "%";
                    break;
                case 0x00: // long term fuel trim
                    m_correctionfactor = 0.0078125F;
                    m_correctionoffset = -128;
                    m_description = "Long term fuel trim";
                    m_units = "";
                    break;
                case 0x03: // idle fuel trim
                    m_correctionfactor = 0.0078125F;
                    m_correctionoffset = -128;
                    m_description = "Idle fuel trim";
                    m_units = "";
                    break;
                case 0x0F: // short term fuel trim
                    m_correctionfactor = 0.0078125F;
                    m_correctionoffset = -128;
                    m_description = "Short term fuel trim";
                    m_units = "";
                    break;
                case 0x49: // IAC adaption, Airmass 
                    m_correctionfactor = 0.0078125F;
                    m_correctionoffset = -128;
                    m_description = "IAC airmass adaption";
                    m_units = "";
                    break;
                case 0x75: // A/C pressure
                    m_correctionfactor = 13.5351F;
                    m_correctionoffset = -175.9823F;
                    m_description = "A/C Pressure";
                    m_units = "kPa";
                    break;
                case 0xC4: // ECT signal sensor
                    m_correctionfactor = 1F;
                    m_correctionoffset = 0;
                    m_description = "ECT signal sensor";
                    m_units = "Volt";
                    break;
                case 0xD1: // ECU temperature
                    m_correctionfactor = 2.0714F;
                    m_correctionoffset = -176.4268F;
                    m_description = "ECU temperature";
                    m_units = "Degrees celcius";
                    break;
                case 0x22: // TPS angle
                    m_correctionfactor = 0.41667F;
                    m_correctionoffset = -5.34F;
                    m_description = "Throttle position angle";
                    m_units = "Degrees";
                    break;
                case 0x2D: // TPS voltage
                    m_correctionfactor = 0.01952F;
                    m_correctionoffset = 0;
                    m_description = "TPS voltage";
                    m_units = "Volt";
                    break;
                case 0x70: // front O2 sensor voltage
                    m_correctionfactor = 0.0049F;
                    m_correctionoffset = -0.1772F;
                    m_description = "Front O2 sensor";
                    m_units = "Volt";
                    break;
                case 0x79: // rear O2 sensor voltage
                    m_correctionfactor = 0.0049F;
                    m_correctionoffset = -0.1772F;
                    m_description = "Rear O2 sensor";
                    m_units = "Volt";
                    break;
                case 0x89: //EGR duty cycle 
                    m_correctionfactor = 1.5625F;
                    m_correctionoffset = -1.5625F;
                    m_description = "EGR duty cycle";
                    m_units = "%";
                    break;
                case 0x8D: //EGR temperature 
                    m_correctionfactor = 1F;
                    m_correctionoffset = -80F;
                    m_description = "EGR temperature";
                    m_units = "Degrees celcius";
                    break;
                case 0x92: //EGR voltage
                    m_correctionfactor = 0.01952F;
                    m_correctionoffset = 0;
                    m_description = "EGR sensor voltage";
                    m_units = "Volt";
                    break;
                case 0xD4: //Accelerometer
                    m_correctionfactor = 0.01952F;
                    m_correctionoffset = 0;
                    m_description = "Accelerometer DC";
                    m_units = "DC Volt";
                    break;
                case 0x17: //Rear knock sensor signal
                    m_correctionfactor = -1;
                    m_correctionoffset = 6;
                    m_description = "Rear knock sensor";
                    m_units = "";
                    break;
                case 0x19: //Front knock sensor signal
                    m_correctionfactor = -1;
                    m_correctionoffset = 6;
                    m_description = "Front knock sensor";
                    m_units = "";
                    break;
                case 0xA2: //Ignition retard by knock
                    m_correctionfactor = 0.15F;
                    m_correctionoffset = 0;
                    m_description = "Ignition retard (knock)";
                    m_units = "Degrees";
                    break;
                case 0xAD: //Boost pressure reduce
                    m_correctionfactor = 1;
                    m_correctionoffset = 0;
                    m_description = "Boost pressure reduce (knock)";
                    m_units = "";
                    break;
                case 0xB0: //Fuel enrichment
                    m_correctionfactor = 1;
                    m_correctionoffset = 0;
                    m_description = "Fuel enrichment";
                    m_units = "";
                    break;
                case 0x44: //Accelerometer AC voltage
                    m_correctionfactor = 0.0195F;
                    m_correctionoffset = -2.5F;
                    m_description = "Accelerometer AC";
                    m_units = "Volt";
                    break;
                case 0xC0: //Front lambda max voltage
                    m_correctionfactor = 0.0049F;
                    m_correctionoffset = -0.1772F;
                    m_description = "Front O2 sensor max voltage";
                    m_units = "Volt";
                    break;
                case 0xC1: //Front lambda min voltage
                    m_correctionfactor = 0.0049F;
                    m_correctionoffset = -0.1772F;
                    m_description = "Front O2 sensor min voltage";
                    m_units = "Volt";
                    break;
                case 0xC9: //Front lambda switching period
                    m_correctionfactor = 0.04F;
                    m_correctionoffset = 0;
                    m_description = "Front O2 sensor switching period";
                    m_units = "seconds";
                    break;
                case 0xFFDC: //TPS M4.4
                    m_correctionfactor = 0.416667F;
                    m_correctionoffset = 13.333333F;
                    m_description = "Throttle position sensor";
                    m_units = "degrees";
                    break;
               

            }
            //return mc;
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
