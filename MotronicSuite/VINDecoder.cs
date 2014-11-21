using System;
using System.Collections.Generic;
using System.Text;

namespace MotronicSuite
{
    public enum VINCarModel : int
    {
        Unknown = 0,
        Volvo_240 = 1,
        Volvo_260 = 2,
        Volvo_XC90 = 3,
        Volvo_740 = 4,
        Volvo_760 = 5,
        Volvo_780 = 6,
        Volvo_940 = 7,
        Volvo_960 = 8,
        Volvo_850 = 9,
        Volvo_S40 = 10,
        Volvo_S60 = 11,
        Volvo_V70 = 12,
        Volvo_S80 = 13,
        Volvo_V40 = 14,
        Volso_S70 = 15,
        Volvo_XC60
    }

    public enum VINEngineType : int
    {
        Unknown,
        B4204S2,
        B5244S4,
        B5244S7,
        B5202S,
        B5252S,
        B5254T4,
        B5234T3,
        B5244T5,
        B5254T,
        B5244T,
        B5244T3,
        B5254T2,
        B5244S,
        B5244S6,
        B5244S2,
        B5254T3,
        D4192T3,
        D4192T,
        B5252T,
        D4192T2,
        D4192T4,
        B8444S,
        B6299S,
        B6324S,
        B6304T4,
        B5234T4,
        B5234T5,
        B63042S,
        B5234T1
    }

    public enum VINTurboModel : int
    {
        None,
        TD0415G,
        TD0416T,
        TD0418T
    }

    public class VINDecoder
    {
        public VINCarInfo DecodeVINNumber(string VINNumber)
        {
            VINCarInfo _carInfo = new VINCarInfo();
            if (VINNumber.StartsWith("YV") || VINNumber.StartsWith("4V")) 
            {
                _carInfo.Makeyear = DecodeMakeyear(VINNumber);
                _carInfo.CarModel = DecodeCarModel(VINNumber, _carInfo.Makeyear);
                _carInfo.ExtraInfo = string.Empty;
                string addInfo = string.Empty;
                _carInfo.EngineType = DecodeEngineType(VINNumber, _carInfo.Makeyear, out addInfo);
                _carInfo.ExtraInfo = addInfo;
                _carInfo.PlantInfo = DecodePlantInfo(VINNumber, _carInfo.Makeyear);
                _carInfo.Series = DecodeSeries(VINNumber, _carInfo.Makeyear);
                _carInfo.TurboModel = DecodeTurboModel(_carInfo.EngineType, _carInfo.CarModel, _carInfo.Makeyear);
                _carInfo.Valid = true;
            }
            
            return _carInfo;
        }

        private VINTurboModel DecodeTurboModel(VINEngineType vINEngineType, VINCarModel carModel, int makeyear)
        {
            // depending on enginetype and vehicletype, determine turbo type
            switch (vINEngineType)
            {
                case VINEngineType.B5234T3:
                    return VINTurboModel.TD0415G;
                case VINEngineType.B5234T4: // all Motronic 4.4
                    return VINTurboModel.TD0416T;
                case VINEngineType.B5234T1:
                    return VINTurboModel.TD0415G;
                case VINEngineType.B5234T5:
                    return VINTurboModel.TD0415G;
                case VINEngineType.B5244S:
                    return VINTurboModel.None;
                case VINEngineType.B5244T: // Turbo model!! Which turbo
                    break;

            }
            return VINTurboModel.None;
        }

        private string DecodeTransmissionType(string VINNumber)
        {
            if (VINNumber.Length < 7) return string.Empty;
            else if (VINNumber[6] == '4') return "4 speed manual";
            else if (VINNumber[6] == '5') return "5-speed manual / front wheel drive";
            else if (VINNumber[6] == '6') return "3 speed automatic";
            else if (VINNumber[6] == '8') return "4-speed automatic";
            else if (VINNumber[6] == '9') return "5 speed automatic";
            return string.Empty;
        }

        

        private string DecodeSeries(string VINNumber, int makeyear)
        {
            if (VINNumber.Length < 5) return string.Empty;
            if (makeyear <= 1991)
            {
                if (VINNumber[4] == 'A') return "Air Bag + 3-Point Safety Harness (Seat Belt)";
                else if (VINNumber[4] == 'X') return "3-Point Safety Harness (Seat Belt)";
            }
            if (makeyear <= 1998)
            {
                if (VINNumber[4] == 'S') return "Sedan (4-door) with Air Bag & 3-Point Safety Harness (Seat Belt)";
                else if (VINNumber[4] == 'W') return "Wagon (5-door) with Air Bag & 3-Point Safety Harness (Seat Belt)";
                else if (VINNumber[4] == 'T') return "Sedan (4-door) 3-Point Safety Harness (Seat Belt) - Canada";
                else if (VINNumber[4] == 'X') return "Wagon (5-door) 3-Point Safety Harness (Seat Belt) - Canada";
            }
            else
            {
                if (VINNumber[4] == 'C') return "All-New C70";
                else if (VINNumber[4] == 'H') return "S40 AWD, S60 AWD, S80 AWD";
                else if (VINNumber[4] == 'J') return "V50 AWD";
                else if (VINNumber[4] == 'K') return "C30 FWD";
                else if (VINNumber[4] == 'L') return "XC60 2WD";
                else if (VINNumber[4] == 'M') return "XC90 5-Seater AWD";
                else if (VINNumber[4] == 'N') return "XC90 5-Seater FWD";
                else if (VINNumber[4] == 'S') return "S40 FWD, S60 FWD, S80 FWD";
                else if (VINNumber[4] == 'W') return "V50 FWD, V70 FWD, V70 AWD";
                else if (VINNumber[4] == 'Y') return "XC90 7-Seater FWD";
                else if (VINNumber[4] == 'Z') return "XC70 AWD, XC90 7-Seater AWD";
            }
            return string.Empty;
        }

        private string DecodePlantInfo(string VINNumber, int makeyear)
        {
            if (VINNumber.Length < 11) return string.Empty;
            else if (VINNumber[10] == '0') return "[Sweden] Kalmar Plant";
            else if (VINNumber[10] == '1') return "[Sweden] Torslanda Plant VCT 21(Volvo Torslandaverken) (Gothenburg)";
            else if (VINNumber[10] == '2') return "[Belgium] Ghent Plant VCG 22";
            else if (VINNumber[10] == '3') return "[Canada] Halifax Plant";
            else if (VINNumber[10] == '4') return "[Italy] - Bertone models 240";
            else if (VINNumber[10] == '5') return "[Malaysia]";
            else if (VINNumber[10] == '6') return "[Australia]";
            else if (VINNumber[10] == '7') return "[Indonesia]";
            else if (VINNumber[10] == 'A') return "[Sweden] Uddevalla Plant (Volvo Cars/TWR (Tom Walkinshaw Racing))";
            else if (VINNumber[10] == 'B') return "[Italy] - Bertone Chongq 31";
            else if (VINNumber[10] == 'D') return "[Italy] - Bertone models 780";
            else if (VINNumber[10] == 'E') return "[Singapore]";
            else if (VINNumber[10] == 'F') return "[The Netherlands] Born Plant (NEDCAR)";
            else if (VINNumber[10] == 'J') return "[Sweden] Uddevalla Plant VCU 38 (Volvo Cars/ Pininfarina Sverige AB)";
            else if (VINNumber[10] == 'M') return "PVÖ 53";
          
            return string.Empty;
        }

        private VINCarModel DecodeCarModel(string VINNumber, int makeyear)
        {
            if (VINNumber.Length < 4) return VINCarModel.Unknown;
            else if (VINNumber[3] == 'A')
            {
                if(makeyear <= 1998) return VINCarModel.Volvo_240;
                else return VINCarModel.Volvo_S80;
            }
            else if (VINNumber[3] == 'B')
            {
                if (makeyear <= 1998) return VINCarModel.Volvo_260;
                else return VINCarModel.Volvo_V70;
            }
            else if (VINNumber[3] == 'C') return VINCarModel.Volvo_XC90;
            else if (VINNumber[3] == 'D') return VINCarModel.Volvo_XC60;
            else if (VINNumber[3] == 'F')
            {
                if (makeyear <= 1998) return VINCarModel.Volvo_740;
                else return VINCarModel.Volvo_S60;
            }
            else if (VINNumber[3] == 'G') return VINCarModel.Volvo_760;
            else if (VINNumber[3] == 'H') return VINCarModel.Volvo_780;
            else if (VINNumber[3] == 'J') return VINCarModel.Volvo_940;
            else if (VINNumber[3] == 'K') return VINCarModel.Volvo_960;
            else if (VINNumber[3] == 'L')
            {
                if (makeyear < 1998) return VINCarModel.Volvo_850;
                else if (makeyear <= 2000) return VINCarModel.Volso_S70;
                else return VINCarModel.Volvo_V70;
            }
            else if (VINNumber[3] == 'M') return VINCarModel.Volvo_S40;
            else if (VINNumber[3] == 'R') return VINCarModel.Volvo_S60;
            else if (VINNumber[3] == 'S') return VINCarModel.Volvo_V70;
            else if (VINNumber[3] == 'T') return VINCarModel.Volvo_S80;
            else if (VINNumber[3] == 'V') return VINCarModel.Volvo_V40;
            else return VINCarModel.Unknown;
        }

        private VINEngineType DecodeEngineType(string VINNumber, int makeyear, out string emissions)
        {
            emissions = string.Empty;
            if (VINNumber.Length < 8) return VINEngineType.Unknown;
            else if (VINNumber[5] == '1' && VINNumber[6] == '7') return VINEngineType.B4204S2;
            else if (VINNumber[5] == '3' && VINNumber[6] == '8') return VINEngineType.B5244S4;
            else if (VINNumber[5] == '3' && VINNumber[6] == '9') return VINEngineType.B5244S7;
            else if (VINNumber[5] == '4' && VINNumber[6] == '1') return VINEngineType.B5202S;
            else if (VINNumber[5] == '5' && VINNumber[6] == '0')
            {
                emissions = "w/o EGR, w/o airpump, w/elec control evap, Motronic 4.3";
                return VINEngineType.B5234T4;
            }
            else if (VINNumber[5] == '5' && VINNumber[6] == '1')
            {
                emissions = "w/o EGR, w/o airpump, w/vac control evap, Fenix 5.2";
                return VINEngineType.B5252S;
            }
            else if (VINNumber[5] == '5' && VINNumber[6] == '2')
            {
                return VINEngineType.B5254T4;
            }
            else if (VINNumber[5] == '5' && VINNumber[6] == '3') return VINEngineType.B5234T3;
            else if (VINNumber[5] == '5' && VINNumber[6] == '4') return VINEngineType.B5244T5;
            else if (VINNumber[5] == '5' && VINNumber[6] == '5')
            {
                emissions = "w/ EGR, w/o airpump, w/OBDII, w/elec control evap, Motronic 4.3";
                return VINEngineType.B5244S;
            }
            else if (VINNumber[5] == '5' && VINNumber[6] == '6') return VINEngineType.B5254T;
            else if (VINNumber[5] == '5' && VINNumber[6] == '7')
            {
                emissions = "w/o EGR, w/o airpump, w/elec control evap, Motronic 4.3";
                return VINEngineType.B5234T1;
            }
            else if (VINNumber[5] == '5' && VINNumber[6] == '8')
            {
                emissions = "w/o EGR, w/o airpump, w/OBDII, w/elec control evap, Motronic 4.3";
                return VINEngineType.B5234T5;
            }
            else if (VINNumber[5] == '5' && VINNumber[6] == '9') return VINEngineType.B5254T2;

            else if (VINNumber[5] == '6' && VINNumber[6] == '1') return VINEngineType.B5244S;
            else if (VINNumber[5] == '6' && VINNumber[6] == '4') return VINEngineType.B5244S6;
            else if (VINNumber[5] == '6' && VINNumber[6] == '5') return VINEngineType.B5244S2;
            else if (VINNumber[5] == '6' && VINNumber[6] == '7') return VINEngineType.B5244S4;
            else if (VINNumber[5] == '6' && VINNumber[6] == '8') return VINEngineType.B5254T3;

            else if (VINNumber[5] == '7' && VINNumber[6] == '0') return VINEngineType.D4192T3;
            else if (VINNumber[5] == '7' && VINNumber[6] == '1') return VINEngineType.D4192T;
            else if (VINNumber[5] == '7' && VINNumber[6] == '2') return VINEngineType.B5252T;
            else if (VINNumber[5] == '7' && VINNumber[6] == '3') return VINEngineType.D4192T2;
            else if (VINNumber[5] == '7' && VINNumber[6] == '8') return VINEngineType.D4192T4;

            else if (VINNumber[5] == '8' && VINNumber[6] == '5') return VINEngineType.B8444S;
            else if (VINNumber[5] == '9' && VINNumber[6] == '6')
            {
                emissions = "w/o EGR, w/ airpump, w/OBDII, w/elec control evap, Motronic 4.4";
                return VINEngineType.B63042S;
            }
            else if (VINNumber[5] == '9' && VINNumber[6] == '7') return VINEngineType.B6299S;
            else if (VINNumber[5] == '9' && VINNumber[6] == '8') return VINEngineType.B6324S;
            else if (VINNumber[5] == '9' && VINNumber[6] == '9') return VINEngineType.B6304T4;
            
            return VINEngineType.Unknown;
        }

        private int DecodeMakeyear(string VINNumber)
        {
            if (VINNumber.Length < 10) return 0;
            else if (VINNumber[9] == 'A') return 2010;
            else if (VINNumber[9] == 'B') return 2011;
            else if (VINNumber[9] == 'C') return 2012;
            else if (VINNumber[9] == 'D') return 2013;
            else if (VINNumber[9] == 'E') return 2014;
            else if (VINNumber[9] == 'F') return 2015;
            else if (VINNumber[9] == 'G') return 1986;
            else if (VINNumber[9] == 'H') return 1987;
            else if (VINNumber[9] == 'J') return 1988;
            else if (VINNumber[9] == 'K') return 1989;
            else if (VINNumber[9] == 'L') return 1990;
            else if (VINNumber[9] == 'M') return 1991;
            else if (VINNumber[9] == 'N') return 1992;
            else if (VINNumber[9] == 'P') return 1993;
            else if (VINNumber[9] == 'R') return 1994;
            else if (VINNumber[9] == 'S') return 1995;
            else if (VINNumber[9] == 'T') return 1996;
            else if (VINNumber[9] == 'V') return 1997;
            else if (VINNumber[9] == 'W') return 1998;
            else if (VINNumber[9] == 'X') return 1999;
            else if (VINNumber[9] == 'Y') return 2000;
            else if (VINNumber[9] == '1') return 2001;
            else if (VINNumber[9] == '2') return 2002;
            else if (VINNumber[9] == '3') return 2003;
            else if (VINNumber[9] == '4') return 2004;
            else if (VINNumber[9] == '5') return 2005;
            else if (VINNumber[9] == '6') return 2006;
            else if (VINNumber[9] == '7') return 2007;
            else if (VINNumber[9] == '8') return 2008;
            else if (VINNumber[9] == '9') return 2009;
            return 0;
        }
    }

    public class VINCarInfo
    {

        private string _extraInfo = string.Empty;

        public string ExtraInfo
        {
            get { return _extraInfo; }
            set { _extraInfo = value; }
        }

        private string _series = string.Empty;

        public string Series
        {
            get { return _series; }
            set { _series = value; }
        }

        private string _plantInfo = string.Empty;

        public string PlantInfo
        {
            get { return _plantInfo; }
            set { _plantInfo = value; }
        }

        private Int32 _makeyear = 0;

        public Int32 Makeyear
        {
            get { return _makeyear; }
            set { _makeyear = value; }
        }


        private bool _valid = false;

        public bool Valid
        {
            get { return _valid; }
            set { _valid = value; }
        }
        private VINCarModel _carModel = VINCarModel.Unknown;

        public VINCarModel CarModel
        {
            get { return _carModel; }
            set { _carModel = value; }
        }
        private VINEngineType _engineType = VINEngineType.Unknown;

        public VINEngineType EngineType
        {
            get { return _engineType; }
            set { _engineType = value; }
        }
        private VINTurboModel _turboModel = VINTurboModel.None;

        public VINTurboModel TurboModel
        {
            get { return _turboModel; }
            set { _turboModel = value; }
        }

    }
}
