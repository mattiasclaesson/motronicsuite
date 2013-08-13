using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MotronicTools
{
    class MapConfigurationM210 : MapConfiguration
    {
        private float m_correctionfactor = 1;

        public override float Correctionfactor
        {
            get { return m_correctionfactor; }
            set { m_correctionfactor = value; }
        }
        private float m_correctionoffset = 0;

        public override float Correctionoffset
        {
            get { return m_correctionoffset; }
            set { m_correctionoffset = value; }
        }
        private string m_description = string.Empty;

        public override string Description
        {
            get { return m_description; }
            set { m_description = value; }
        }

        private string m_units = string.Empty;

        public override string Units
        {
            get { return m_units; }
            set { m_units = value; }
        }

        public override void GetCorrectionFactorForMap(int id, bool isM44, bool isLH242, bool isM18, bool isM210)
        {
            m_correctionfactor = 1;
            m_correctionoffset = 0;
            m_description = "Unknown: " + id.ToString("X2");
            switch (id)
            {
                case 0x04:
                    m_correctionfactor = 1;
                    m_correctionoffset = 0;
                    m_description = "Coolant temp ADC value";
                    m_units = "raw ADC";
                    break;
                case 0x36: // Battery voltage
                    m_correctionfactor = 0.0704F;
                    m_description = "Battery voltage";
                    m_units = "Volt";
                    break;
                case 0x37: // Coolant temperature
                    m_correctionfactor = 1;
                    m_correctionoffset = -80;
                    m_description = "Coolant temperature";
                    m_units = "Degrees celcius";
                    break;
                case 0x3A:
                    m_correctionfactor = 40;
                    m_correctionoffset = 0;
                    m_description = "Engine speed";
                    m_units = "RPM";
                    break;
                case 0x3F:
                    m_correctionfactor = 0.05F;
                    m_correctionoffset = 0;
                    m_description = "Internal load signal";
                    m_units = "ms";
                    break;
            }


        }
    }
}
