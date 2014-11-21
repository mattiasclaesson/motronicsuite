using System;
using System.Collections.Generic;
using System.Text;


namespace MotronicSuite
{
    class PartNumberConverter
    {
        public PartNumberConverter()
        {

        }
        
        public ECUInformation GetECUInfo(string partnumber, string enginetype)
        {
            ECUInformation returnvalue = new ECUInformation();
            switch (partnumber)
            {
                #region M4.3
                case "0261203850":
                    returnvalue.CarDescription = "850 20T-5 B5204FT";
                    returnvalue.Is2point3liter = false;
                    returnvalue.Isturbo = true;
                    returnvalue.Automatic_gearbox = true;
                    returnvalue.Valid = true;
                    returnvalue.Bhp = 210;
                    returnvalue.Enginetype = EngineType.B5204T;
                    returnvalue.Carmodel = CarModel.Volvo850;
                    returnvalue.Turbomodel = TurboModel.TD0415G;
                    returnvalue.Torque = 300;
                    returnvalue.Swversions = new string[1];
                    returnvalue.Swversions.SetValue("1267358983", 0);
                    break;
                case "0261204041":
                    returnvalue.CarDescription = "850 20T B5204T";
                    returnvalue.Is2point3liter = false;
                    returnvalue.Isturbo = true;
                    returnvalue.Automatic_gearbox = true;
                    returnvalue.Valid = true;
                    returnvalue.Bhp = 210;
                    returnvalue.Enginetype = EngineType.B5204T;
                    returnvalue.Carmodel = CarModel.Volvo850;
                    returnvalue.Turbomodel = TurboModel.TD0415G;
                    returnvalue.Torque = 300;
                    returnvalue.Swversions = new string[1];
                    returnvalue.Swversions.SetValue("2227355899", 0);
                    break;
                case "0261203071":
                    returnvalue.CarDescription = "850 23T-5 B5234FT";
                    returnvalue.Is2point3liter = true;
                    returnvalue.Isturbo = true;
                    returnvalue.Automatic_gearbox = true;
                    returnvalue.Valid = true;
                    returnvalue.Bhp = 225;
                    returnvalue.Enginetype = EngineType.B5234T;
                    returnvalue.Carmodel = CarModel.Volvo850;
                    returnvalue.Turbomodel = TurboModel.TD0415G;
                    returnvalue.Torque = 300;
                    returnvalue.Swversions = new string[1];
                    returnvalue.Swversions.SetValue("1267358074", 0);
                    break;
                case "0261203072":
                    returnvalue.CarDescription = "850 23T B5234FT";
                    returnvalue.Is2point3liter = true;
                    returnvalue.Isturbo = true;
                    returnvalue.Automatic_gearbox = true;
                    returnvalue.Valid = true;
                    returnvalue.Bhp = 225;
                    returnvalue.Enginetype = EngineType.B5234T;
                    returnvalue.Carmodel = CarModel.Volvo850;
                    returnvalue.Turbomodel = TurboModel.TD0415G;
                    returnvalue.Torque = 300;
                    returnvalue.Swversions = new string[3];
                    returnvalue.Swversions.SetValue("1267355652", 0);
                    returnvalue.Swversions.SetValue("1267358232", 1);
                    returnvalue.Swversions.SetValue("1267358074", 2);
                    break;
                case "0261200548":
                    returnvalue.CarDescription = "850 20T B5204T";
                    returnvalue.Is2point3liter = false;
                    returnvalue.Isturbo = true;
                    returnvalue.Automatic_gearbox = true;
                    returnvalue.Valid = true;
                    returnvalue.Bhp = 210;
                    returnvalue.Enginetype = EngineType.B5204T;
                    returnvalue.Carmodel = CarModel.Volvo850;
                    returnvalue.Turbomodel = TurboModel.TD0415G;
                    returnvalue.Torque = 300;
                    returnvalue.Swversions = new string[4];
                    returnvalue.Swversions.SetValue("1267358229", 0);
                    returnvalue.Swversions.SetValue("1267355823", 1);
                    returnvalue.Swversions.SetValue("1267358638", 2);
                    returnvalue.Swversions.SetValue("1267358087", 3);
                    break;
                case "0261204188":
                    returnvalue.CarDescription = "850 20L GLT B5204T2";
                    returnvalue.Is2point3liter = false;
                    returnvalue.Isturbo = false;
                    returnvalue.Automatic_gearbox = true;
                    returnvalue.Valid = true;
                    returnvalue.Bhp = 170;
                    returnvalue.Enginetype = EngineType.B5204T;
                    returnvalue.Carmodel = CarModel.Volvo850;
                    returnvalue.Turbomodel = TurboModel.None;
                    returnvalue.Torque = 0;
                    returnvalue.Swversions = new string[1];
                    returnvalue.Swversions.SetValue("2537355997", 0);
                    break;
                case "0261203074":
                    returnvalue.CarDescription = "850 2.3 T5 Automatic";
                    returnvalue.Is2point3liter = true;
                    returnvalue.Isturbo = true;
                    returnvalue.Automatic_gearbox = true;
                    returnvalue.Valid = true;
                    returnvalue.Bhp = 225;
                    returnvalue.Enginetype = EngineType.B5234T;
                    returnvalue.Carmodel = CarModel.Volvo850;
                    returnvalue.Turbomodel = TurboModel.TD0415G;
                    returnvalue.Torque = 300;
                    returnvalue.Swversions = new string[5];
                    returnvalue.Swversions.SetValue("1037358589", 0);
                    returnvalue.Swversions.SetValue("1267358641", 1);
                    returnvalue.Swversions.SetValue("2227355825", 2);
                    returnvalue.Swversions.SetValue("1267355825", 3);
                    returnvalue.Swversions.SetValue("1267358075", 4);
                    break;
                case "0261200549":
                    returnvalue.CarDescription = "850 2.3 T5 Manual";
                    returnvalue.Is2point3liter = true;
                    returnvalue.Isturbo = true;
                    returnvalue.Automatic_gearbox = false;
                    returnvalue.Valid = true;
                    returnvalue.Bhp = 225;
                    returnvalue.Enginetype = EngineType.B5234T;
                    returnvalue.Carmodel = CarModel.Volvo850;
                    returnvalue.Turbomodel = TurboModel.TD0415G;
                    returnvalue.Torque = 300;
                    returnvalue.Swversions = new string[2];
                    returnvalue.Swversions.SetValue("1267358639", 0);
                    returnvalue.Swversions.SetValue("1267358073", 1);
                    break;
                case "0261203627":
                    returnvalue.CarDescription = "850 T5R Automatic (Euro spec)";
                    returnvalue.Is2point3liter = true;
                    returnvalue.Isturbo = true;
                    returnvalue.Automatic_gearbox = true;
                    returnvalue.Valid = true;
                    returnvalue.Bhp = 240;
                    returnvalue.Enginetype = EngineType.B5234T5;
                    returnvalue.Carmodel = CarModel.Volvo850;
                    returnvalue.Turbomodel = TurboModel.TD0415G;
                    returnvalue.Torque = 300;
                    returnvalue.Swversions = new string[2];
                    returnvalue.Swversions.SetValue("1267358234", 0);
                    returnvalue.Swversions.SetValue("2227355828", 1);
                    break;
                case "0261203628":
                    returnvalue.CarDescription = "850 T5R Automatic (US spec)";
                    returnvalue.Is2point3liter = true;
                    returnvalue.Isturbo = true;
                    returnvalue.Automatic_gearbox = true;
                    returnvalue.Valid = true;
                    returnvalue.Bhp = 240;
                    returnvalue.Enginetype = EngineType.B5234T5;
                    returnvalue.Carmodel = CarModel.Volvo850;
                    returnvalue.Turbomodel = TurboModel.TD0415G;
                    returnvalue.Torque = 300;
                    returnvalue.Swversions = new string[1];
                    returnvalue.Swversions.SetValue("2227355802", 0);
                    break;
                case "0261203626":
                    returnvalue.CarDescription = "850 T5R Manual";
                    returnvalue.Is2point3liter = true;
                    returnvalue.Isturbo = true;
                    returnvalue.Automatic_gearbox = false;
                    returnvalue.Valid = true;
                    returnvalue.Bhp = 240;
                    returnvalue.Enginetype = EngineType.B5234T5;
                    returnvalue.Carmodel = CarModel.Volvo850;
                    returnvalue.Turbomodel = TurboModel.TD0415G;
                    returnvalue.Torque = 330;
                    returnvalue.Swversions = new string[2];
                    returnvalue.Swversions.SetValue("1267358233", 0);
                    returnvalue.Swversions.SetValue("2227355827", 1);
                    break;
                case "0261204134":
                    returnvalue.CarDescription = "850R Automatic";
                    returnvalue.Is2point3liter = true;
                    returnvalue.Isturbo = true;
                    returnvalue.Automatic_gearbox = true;
                    returnvalue.Valid = true;
                    returnvalue.Bhp = 240;
                    returnvalue.Enginetype = EngineType.B5234T5;
                    returnvalue.Carmodel = CarModel.Volvo850;
                    returnvalue.Turbomodel = TurboModel.TD0415G;
                    returnvalue.Torque = 300;
                    returnvalue.Swversions = new string[2];
                    returnvalue.Swversions.SetValue("1037358586", 0);
                    returnvalue.Swversions.SetValue("2537355830", 1);
                    break;
                case "0261204225":
                    returnvalue.CarDescription = "850R Manual";
                    returnvalue.Is2point3liter = true;
                    returnvalue.Isturbo = true;
                    returnvalue.Automatic_gearbox = false;
                    returnvalue.Valid = true;
                    returnvalue.Bhp = 250;
                    returnvalue.Enginetype = EngineType.B5234T4;
                    returnvalue.Carmodel = CarModel.Volvo850;
                    returnvalue.Turbomodel = TurboModel.TD0416T;
                    returnvalue.Torque = 350;
                    returnvalue.Swversions = new string[1];
                    returnvalue.Swversions.SetValue("1037355277", 0);
                    break;
                case "0261203852":
                    returnvalue.CarDescription = "850 2.3 T5 Automatic";
                    returnvalue.Is2point3liter = true;
                    returnvalue.Isturbo = true;
                    returnvalue.Automatic_gearbox = true;
                    returnvalue.Valid = true;
                    returnvalue.Bhp = 225;
                    returnvalue.Enginetype = EngineType.B5234T;
                    returnvalue.Carmodel = CarModel.Volvo850;
                    returnvalue.Turbomodel = TurboModel.TD0415G;
                    returnvalue.Torque = 300;
                    returnvalue.Swversions = new string[1];
                    returnvalue.Swversions.SetValue("1267358985", 0);
                    break;
                case "0261203851":
                    returnvalue.CarDescription = "850 2.3 T5 Manual";
                    returnvalue.Is2point3liter = true;
                    returnvalue.Isturbo = true;
                    returnvalue.Automatic_gearbox = false;
                    returnvalue.Valid = true;
                    returnvalue.Bhp = 225;
                    returnvalue.Enginetype = EngineType.B5234T;
                    returnvalue.Carmodel = CarModel.Volvo850;
                    returnvalue.Turbomodel = TurboModel.TD0415G;
                    returnvalue.Torque = 300;
                    returnvalue.Swversions = new string[1];
                    returnvalue.Swversions.SetValue("1267358984", 0);
                    break;
                case "0261203962":
                    returnvalue.CarDescription = "850 2.0 T5 Automatic";
                    returnvalue.Is2point3liter = false;
                    returnvalue.Isturbo = true;
                    returnvalue.Automatic_gearbox = true;
                    returnvalue.Valid = true;
                    returnvalue.Bhp = 210;
                    returnvalue.Enginetype = EngineType.B5204T;
                    returnvalue.Carmodel = CarModel.Volvo850;
                    returnvalue.Turbomodel = TurboModel.TD0415G;
                    returnvalue.Torque = 300;
                    returnvalue.Swversions = new string[2];
                    returnvalue.Swversions.SetValue("2227355651", 0);
                    returnvalue.Swversions.SetValue("1267358965", 1);
                    break;
                case "0261203189":
                    returnvalue.CarDescription = "850 2.0 GLT Automatic";
                    returnvalue.Is2point3liter = false;
                    returnvalue.Isturbo = true;
                    returnvalue.Automatic_gearbox = true;
                    returnvalue.Valid = true;
                    returnvalue.Bhp = 210;
                    returnvalue.Enginetype = EngineType.B5204T;
                    returnvalue.Carmodel = CarModel.Volvo850;
                    returnvalue.Turbomodel = TurboModel.TD0415G;
                    returnvalue.Torque = 300;
                    returnvalue.Swversions = new string[1];
                    returnvalue.Swversions.SetValue("", 0);
                    break;
                #endregion
                #region M4.4
                /*
VOLVO 850 25T B5254LT 1275384 140KW M441 CHK:19D3 BOSCH  0261204305 1037358409
VOLVO C70/S70/V70 20T B5204T2 1275557 132KW M441 CHK:C780 BOSCH 0261204442 1037358966
VOLVO C70/S70/V70 20T B5204T2 1275557 132KW M441 CHK:C780 BOSCH 0261204442 1037358966
VOLVO C70/S70/V70 20T B5204T2 1275558 132KW M441 CHK:CD27 BOSCH 0261204443 1037358967
VOLVO C70/S70/V70 20T B5204T2 9155746 132KW M441 CHK:C5A6 BOSCH 0261204442 1037357513
VOLVO C70/S70/V70 20T B5204T3 1275209 166KW M441 CHK:296B BOSCH 0261204446 1037358980
VOLVO C70/S70/V70 20T B5204T3 1275386 166KW M441 CHK:1B2C BOSCH 0261204444 1037358968
VOLVO C70/S70/V70 20T B5204T3 9155750 166KW M441 CHK:1968 BOSCH 0261204444 1037357515
VOLVO C70/S70/V70 20T B5204T3 9155752 166KW M441 CHK:24B4 BOSCH 0261204446 1037357516
VOLVO C70/S70/V70 23T B5234T3 1275523 176KW (TME) M441 CHK:48B6 BOSCH 0261204450 1037358984
VOLVO C70/S70/V70 23T-5 B5234T3 9125818 176KW M441 CHK:A041 BOSCH 0261204449 1037357756
VOLVO C70/S70/V70 23T-5 B5234T3 9155757  DAM1 M441 CHK:A3D3 BOSCH 0261204449 1037357519
VOLVO C70/S70/V70 23T-5 B5234T3 9155757  DAM2 M441 CHK:A3D3 BOSCH 0261204449 1037357519
VOLVO C70/S70/V70 23T-5 B5234T3 9155757 DAM3 M441 CHK:A3D3 BOSCH 0261204449      1037357519
VOLVO C70/S70/V70 23T-5 B5234T3 9155757 DAM4 M441 CHK:A3D3 BOSCH 0261204449      1037357519
VOLVO C70/S70/V70 23T-5 B5234T3 9155757 DAM4 M441 CHK:A3D3 BOSCH 0261204449 1037357519
VOLVO C70/S70/V70 23T-5 B5234T3 9155757 176KW M441 CHK:A1EE BOSCH 0261204449 1037357519
VOLVO C70/S70/V70 23T-5 B5234T3 9155761  M441 CHK:C6C9 BOSCH 28F0 0261204608 1037357527
VOLVO C70/S70/V70 23T-5 B5234T3 9155763  M441 CHK:6F31 BOSCH 28F0 0261204451 1037357521
VOLVO C70/S70/V70 23T-5 B5234T3 9155773 176KW M441 CHK:55B4 BOSCH 0261204448 1037357755
VOLVO C70/S70/V70 23T-5 B5234T3 9155801 176KW M441 CHK:5CF7 BOSCH 0261204448 1037357518
VOLVO C70/S70/V70 23T-5 B5234T3 9155801 176KW M441 CHK:5EDC BOSCH 0261204448 1037357518
VOLVO C70/S70/V70 23T-5 B5234T3 9155801 176KW M441 CHK:7037 BOSCH 0261204448 1037357982
VOLVO C70/S70/V70 23T-5 B5234T3 9155876 M441 CHK:063F BOSCH 28F0 0261204607      1037357780
VOLVO C70/S70/V70 23T-5 B5234T3 9486121 176KW M441 CHK:4310 BOSCH 0261204449 1037359866
VOLVO C70/S70/V70 23T-5 B5234T3 9486123 176KW M441 CHK:EA19 BOSCH 0261204607 1037359868
VOLVO C70/S70/V70 25L B5254S 9202013 DAM1 M441 CHK:65A9 BOSCH 28 0261204570      1037358289
VOLVO C70/S70/V70 25L B5254S 9202013 DAM2 121KW M441 CHK:65A9 BOSCH 0261204570 1037358289
VOLVO C70/S70/V70 25T B5254LT 9490034 140KW M441 CHK:52B6 BOSCH 0261204609 1037359880
VOLVO C70/S70/V70 25T B5254T 1275555 142KW M441 CHK:960E BOSCH  0261204459 1037358991
VOLVO C70/S70/V70 25T B5254T 1275560 142KW M441 CHK:B681 BOSCH  0261204457 1037358989
VOLVO C70/S70/V70 25T B5254T 9155779 142KW M441 CHK:A177 BOSCH  0261204456 1037357522
VOLVO C70/S70/V70 25T B5254T 9155779 142KW M441 CHK:A3CB BOSCH  0261204456 1037358988
VOLVO C70/S70/V70 25T B5254T 9155781 142KW M441 CHK:AC56 BOSCH  0261204457 1037357523
VOLVO C70/S70/V70 25T B5254T 9155781 142KW M441 CHK:AE3B BOSCH  0261204457 1037357523
VOLVO C70/S70/V70 25T B5254T 9155781 142KW M441 CHK:BFAD BOSCH  0261204457 1037357523
VOLVO C70/S70/V70 25T B5254T 9155787 142KW M441 CHK:8E24 BOSCH  0261204459 1037357525
VOLVO C70/S70/V70 25T B5254T 9155787 142KW M441 CHK:9009 BOSCH  0261204459 1037357525
VOLVO C70/S70/V70 25T B5254T 9155795 142KW M441 CHK:8E74 BOSCH  0261204765 1037357759
VOLVO C70/S70/V70 25T B5254T 9486103 142KW M441 CHK:4323 BOSCH  0261204456 1037359875
VOLVO C70/S70/V70 25T B5254T 9486105 142KW M441 CHK:4E2C BOSCH  0261204457 1037359876
VOLVO C70/S70/V70 25T B5254T 9486109 142KW M441 CHK:2FEC BOSCH  0261204765 1037359878
VOLVO C70/S70/V70 25T-R B5234T4 9155775 184KW M441 CHK:0A6E BOSCH 0261204611 1037357530
VOLVO C70/S70/V70 25T-R B5234T4 9155775 184KW M441 CHK:7DE2 BOSCH 0261204611 1037357530
VOLVO C70/S70/V70 25T-R B5234T4 9155775 184KW M441 CHK:7E26 BOSCH 0261204611 1037357530
VOLVO C70/S70/V70 25T-R B5234T4 9155777 184KW M441 CHK:7B46 BOSCH 0261204612 1037357531
VOLVO C70/S70/V70 25T-R B5234T4 9155777 184KW M441 CHK:7D2B BOSCH 0261204612 1037357531
VOLVO C70/S70/V70 25T-R B5234T4 9155777 184KW POT M441 CHK:8856 BOSCH 0261204612 1037357531
VOLVO C70/S70/V70 25T-R B5234T6 9186410 184KW M441 CHK:7C67 BOSCH 0261204606 1037357287
VOLVO C70/S70/V70 25T-R B5234T6 9486125 184KW M441 CHK:0268 BOSCH 0261204606 1037359872
                     * */
                case "0261204305":
                    returnvalue.CarDescription = "850 25T B5254LT";
                    returnvalue.Is2point3liter = true;
                    returnvalue.Isturbo = true;
                    returnvalue.Automatic_gearbox = true;
                    returnvalue.Valid = true;
                    returnvalue.Bhp = 210;
                    returnvalue.Enginetype = EngineType.B5254T;
                    returnvalue.Carmodel = CarModel.Volvo850;
                    returnvalue.Turbomodel = TurboModel.TD0415G;
                    returnvalue.Torque = 300;
                    returnvalue.Swversions = new string[1];
                    returnvalue.Swversions.SetValue("1037358409", 0);
                 break;
                case "0261204442":
                 returnvalue.CarDescription = "C70/S70/V70 2.0T B5204T2";
                 returnvalue.Is2point3liter = true;
                 returnvalue.Isturbo = true;
                 returnvalue.Automatic_gearbox = true;
                 returnvalue.Valid = true;
                 returnvalue.Bhp = 175;
                 returnvalue.Enginetype = EngineType.B5204T2;
                 returnvalue.Carmodel = CarModel.CSV70;
                 returnvalue.Turbomodel = TurboModel.TD0415G;
                 returnvalue.Torque = 0;
                 returnvalue.Swversions = new string[2];
                 returnvalue.Swversions.SetValue("1037358966", 0);
                 returnvalue.Swversions.SetValue("1037357513", 1);
                 break;
                case "0261204443":
                 returnvalue.CarDescription = "C70/S70/V70 2.0T B5204T2";
                 returnvalue.Is2point3liter = true;
                 returnvalue.Isturbo = true;
                 returnvalue.Automatic_gearbox = true;
                 returnvalue.Valid = true;
                 returnvalue.Bhp = 175;
                 returnvalue.Enginetype = EngineType.B5204T2;
                 returnvalue.Carmodel = CarModel.CSV70;
                 returnvalue.Turbomodel = TurboModel.TD0415G;
                 returnvalue.Torque = 0;
                 returnvalue.Swversions = new string[1];
                 returnvalue.Swversions.SetValue("1037358967", 0);
                 break;
                case "0261204444":
                 returnvalue.CarDescription = "C70/S70/V70 2.0T B5204T3";
                 returnvalue.Is2point3liter = true;
                 returnvalue.Isturbo = true;
                 returnvalue.Automatic_gearbox = true;
                 returnvalue.Valid = true;
                 returnvalue.Bhp = 225;
                 returnvalue.Enginetype = EngineType.B5204T3;
                 returnvalue.Carmodel = CarModel.CSV70;
                 returnvalue.Turbomodel = TurboModel.TD0415G;
                 returnvalue.Torque = 300;
                 returnvalue.Swversions = new string[2];
                 returnvalue.Swversions.SetValue("1037358968", 0);
                 returnvalue.Swversions.SetValue("1037357515", 1);
                 break;
                case "0261204446":
                 returnvalue.CarDescription = "C70/S70/V70 2.0T B5204T3";
                 returnvalue.Is2point3liter = true;
                 returnvalue.Isturbo = true;
                 returnvalue.Automatic_gearbox = true;
                 returnvalue.Valid = true;
                 returnvalue.Bhp = 225;
                 returnvalue.Enginetype = EngineType.B5204T3;
                 returnvalue.Carmodel = CarModel.CSV70;
                 returnvalue.Turbomodel = TurboModel.TD0415G;
                 returnvalue.Torque = 300;
                 returnvalue.Swversions = new string[2];
                 returnvalue.Swversions.SetValue("1037358980", 0);
                 returnvalue.Swversions.SetValue("1037357516", 1);
                 break;
                case "0261204448":
                 returnvalue.CarDescription = "C70/S70/V70 23T-5 B5234T3";
                 returnvalue.Is2point3liter = true;
                 returnvalue.Isturbo = true;
                 returnvalue.Automatic_gearbox = true;
                 returnvalue.Valid = true;
                 returnvalue.Bhp = 235;
                 returnvalue.Enginetype = EngineType.B5234T3;
                 returnvalue.Carmodel = CarModel.CSV70;
                 returnvalue.Turbomodel = TurboModel.TD0415G;
                 returnvalue.Torque = 300;
                 returnvalue.Swversions = new string[3];
                 returnvalue.Swversions.SetValue("1037357755", 0);
                 returnvalue.Swversions.SetValue("1037357518", 1);
                 returnvalue.Swversions.SetValue("1037357982", 2);
                 break;
                case "0261204449":
                 returnvalue.CarDescription = "C70/S70/V70 23T-5 B5234T3";
                 returnvalue.Is2point3liter = true;
                 returnvalue.Isturbo = true;
                 returnvalue.Automatic_gearbox = true;
                 returnvalue.Valid = true;
                 returnvalue.Bhp = 235;
                 returnvalue.Enginetype = EngineType.B5234T3;
                 returnvalue.Carmodel = CarModel.CSV70;
                 returnvalue.Turbomodel = TurboModel.TD0415G;
                 returnvalue.Torque = 300;
                 returnvalue.Swversions = new string[3];
                 returnvalue.Swversions.SetValue("1037357756", 0);
                 returnvalue.Swversions.SetValue("1037357519", 1);
                 returnvalue.Swversions.SetValue("1037359866", 2);
                 break;
                case "0261204450":
                 returnvalue.CarDescription = "C70/S70/V70 23T B5234T3";
                 returnvalue.Is2point3liter = true;
                 returnvalue.Isturbo = true;
                 returnvalue.Automatic_gearbox = true;
                 returnvalue.Valid = true;
                 returnvalue.Bhp = 235;
                 returnvalue.Enginetype = EngineType.B5234T3;
                 returnvalue.Carmodel = CarModel.CSV70;
                 returnvalue.Turbomodel = TurboModel.TD0415G;
                 returnvalue.Torque = 300;
                 returnvalue.Swversions = new string[1];
                 returnvalue.Swversions.SetValue("1037358984", 0);
                 break;
                case "0261204451":
                 returnvalue.CarDescription = "C70/S70/V70 23T-5 B5234T3";
                 returnvalue.Is2point3liter = true;
                 returnvalue.Isturbo = true;
                 returnvalue.Automatic_gearbox = true;
                 returnvalue.Valid = true;
                 returnvalue.Bhp = 235;
                 returnvalue.Enginetype = EngineType.B5234T3;
                 returnvalue.Carmodel = CarModel.CSV70;
                 returnvalue.Turbomodel = TurboModel.TD0415G;
                 returnvalue.Torque = 300;
                 returnvalue.Swversions = new string[1];
                 returnvalue.Swversions.SetValue("1037357521", 0);
                 break;
                case "0261204456":
                 returnvalue.CarDescription = "C70/S70/V70 25T B5234T";
                 returnvalue.Is2point3liter = true;
                 returnvalue.Isturbo = true;
                 returnvalue.Automatic_gearbox = true;
                 returnvalue.Valid = true;
                 returnvalue.Bhp = 190;
                 returnvalue.Enginetype = EngineType.B5234T;
                 returnvalue.Carmodel = CarModel.CSV70;
                 returnvalue.Turbomodel = TurboModel.TD0415G;
                 returnvalue.Torque = 0;
                 returnvalue.Swversions = new string[3];
                 returnvalue.Swversions.SetValue("1037357522", 0);
                 returnvalue.Swversions.SetValue("1037358988", 1);
                 returnvalue.Swversions.SetValue("1037359875", 2);
                 break;
                case "0261204457":
                 returnvalue.CarDescription = "C70/S70/V70 25T B5234T";
                 returnvalue.Is2point3liter = true;
                 returnvalue.Isturbo = true;
                 returnvalue.Automatic_gearbox = true;
                 returnvalue.Valid = true;
                 returnvalue.Bhp = 190;
                 returnvalue.Enginetype = EngineType.B5234T;
                 returnvalue.Carmodel = CarModel.CSV70;
                 returnvalue.Turbomodel = TurboModel.TD0415G;
                 returnvalue.Torque = 0;
                 returnvalue.Swversions = new string[3];
                 returnvalue.Swversions.SetValue("1037358989", 0);
                 returnvalue.Swversions.SetValue("1037357523", 1);
                 returnvalue.Swversions.SetValue("1037359876", 2);
                 break;
                case "0261204459":
                 returnvalue.CarDescription = "C70/S70/V70 25T B5234T";
                 returnvalue.Is2point3liter = true;
                 returnvalue.Isturbo = true;
                 returnvalue.Automatic_gearbox = true;
                 returnvalue.Valid = true;
                 returnvalue.Bhp = 190;
                 returnvalue.Enginetype = EngineType.B5234T;
                 returnvalue.Carmodel = CarModel.CSV70;
                 returnvalue.Turbomodel = TurboModel.TD0415G;
                 returnvalue.Torque = 0;
                 returnvalue.Swversions = new string[2];
                 returnvalue.Swversions.SetValue("1037358991", 0);
                 returnvalue.Swversions.SetValue("1037357525", 1);
                 break;
                case "0261204570":
                 returnvalue.CarDescription = "C70/S70/V70 25L B5234S";
                 returnvalue.Is2point3liter = true;
                 returnvalue.Isturbo = true;
                 returnvalue.Automatic_gearbox = true;
                 returnvalue.Valid = true;
                 returnvalue.Bhp = 0;
                 returnvalue.Enginetype = EngineType.B5234S;
                 returnvalue.Carmodel = CarModel.CSV70;
                 returnvalue.Turbomodel = TurboModel.TD0415G;
                 returnvalue.Torque = 0;
                 returnvalue.Swversions = new string[1];
                 returnvalue.Swversions.SetValue("1037358289", 0);
                 break;
                case "0261204606":
                 returnvalue.CarDescription = "C70/S70/V70 25TR B5234T6";
                 returnvalue.Is2point3liter = true;
                 returnvalue.Isturbo = true;
                 returnvalue.Automatic_gearbox = true;
                 returnvalue.Valid = true;
                 returnvalue.Bhp = 250;
                 returnvalue.Enginetype = EngineType.B5234T6;
                 returnvalue.Carmodel = CarModel.CSV70;
                 returnvalue.Turbomodel = TurboModel.TD0415G;
                 returnvalue.Torque = 0;
                 returnvalue.Swversions = new string[2];
                 returnvalue.Swversions.SetValue("1037357287", 0);
                 returnvalue.Swversions.SetValue("1037359872", 1);
                 break;
                case "0261204607":
                 returnvalue.CarDescription = "C70/S70/V70 25T-5 B5234T3";
                 returnvalue.Is2point3liter = true;
                 returnvalue.Isturbo = true;
                 returnvalue.Automatic_gearbox = true;
                 returnvalue.Valid = true;
                 returnvalue.Bhp = 235;
                 returnvalue.Enginetype = EngineType.B5234T3;
                 returnvalue.Carmodel = CarModel.CSV70;
                 returnvalue.Turbomodel = TurboModel.TD0415G;
                 returnvalue.Torque = 0;
                 returnvalue.Swversions = new string[2];
                 returnvalue.Swversions.SetValue("1037357780", 0);
                 returnvalue.Swversions.SetValue("1037359868", 1);
                 break;
                case "0261204608":
                 returnvalue.CarDescription = "C70/S70/V70 25T-5 B5234T3";
                 returnvalue.Is2point3liter = true;
                 returnvalue.Isturbo = true;
                 returnvalue.Automatic_gearbox = true;
                 returnvalue.Valid = true;
                 returnvalue.Bhp = 235;
                 returnvalue.Enginetype = EngineType.B5234T3;
                 returnvalue.Carmodel = CarModel.CSV70;
                 returnvalue.Turbomodel = TurboModel.TD0415G;
                 returnvalue.Torque = 0;
                 returnvalue.Swversions = new string[1];
                 returnvalue.Swversions.SetValue("1037357527", 0);
                 break;
                case "0261204609":
                 returnvalue.CarDescription = "C70/S70/V70 25T B5234LT";
                 returnvalue.Is2point3liter = true;
                 returnvalue.Isturbo = true;
                 returnvalue.Automatic_gearbox = true;
                 returnvalue.Valid = true;
                 returnvalue.Bhp = 175;
                 returnvalue.Enginetype = EngineType.B5234T;
                 returnvalue.Carmodel = CarModel.CSV70;
                 returnvalue.Turbomodel = TurboModel.TD0415G;
                 returnvalue.Torque = 0;
                 returnvalue.Swversions = new string[1];
                 returnvalue.Swversions.SetValue("1037359880", 0);
                 break;
                case "0261204611":
                 returnvalue.CarDescription = "C70/S70/V70 25T-R B5234T4";
                 returnvalue.Is2point3liter = true;
                 returnvalue.Isturbo = true;
                 returnvalue.Automatic_gearbox = true;
                 returnvalue.Valid = true;
                 returnvalue.Bhp = 250;
                 returnvalue.Enginetype = EngineType.B5234T4;
                 returnvalue.Carmodel = CarModel.CSV70;
                 returnvalue.Turbomodel = TurboModel.TD0415G;
                 returnvalue.Torque = 0;
                 returnvalue.Swversions = new string[1];
                 returnvalue.Swversions.SetValue("1037357530", 0);
                 break;
                case "0261204612":
                 returnvalue.CarDescription = "C70/S70/V70 25T-R B5234T4";
                 returnvalue.Is2point3liter = true;
                 returnvalue.Isturbo = true;
                 returnvalue.Automatic_gearbox = true;
                 returnvalue.Valid = true;
                 returnvalue.Bhp = 250;
                 returnvalue.Enginetype = EngineType.B5234T4;
                 returnvalue.Carmodel = CarModel.CSV70;
                 returnvalue.Turbomodel = TurboModel.TD0415G;
                 returnvalue.Torque = 0;
                 returnvalue.Swversions = new string[1];
                 returnvalue.Swversions.SetValue("1037357531", 0);
                 break;
                case "0261204765":
                 returnvalue.CarDescription = "C70/S70/V70 25T B5234T";
                 returnvalue.Is2point3liter = true;
                 returnvalue.Isturbo = true;
                 returnvalue.Automatic_gearbox = true;
                 returnvalue.Valid = true;
                 returnvalue.Bhp = 190;
                 returnvalue.Enginetype = EngineType.B5234T;
                 returnvalue.Carmodel = CarModel.CSV70;
                 returnvalue.Turbomodel = TurboModel.TD0415G;
                 returnvalue.Torque = 0;
                 returnvalue.Swversions = new string[2];
                 returnvalue.Swversions.SetValue("1037357759", 0);
                 returnvalue.Swversions.SetValue("1037359878", 1);
                 break;
                #endregion
            }
            return returnvalue;
        }
    }

    enum CarModel : int
    {
        Unknown = 0,
        Volvo850,
        CSV70
    }

    enum EngineType : int
    {
        Unknown,
        B5234T, // 2.3 T5
        B5234T3,
        B5234T4, // 2.3R manual 
        B5234T5, // 2.3 T5R + 2.3R automatic
        B5252S,
        B5202S, 
        B5204T, // 2.0 T5
        B5204T3, // 2.0 T5R
        B5254T, //2.4T
        B5204T2,
        B5234S,
        B5234T6
    }

    enum TurboModel : int
    {
        None,
        TD0415G,
        TD0416T,
        TD0418T
    }

    class ECUInformation
    {
        private string[] swversions = new string[1];

        public string[] Swversions
        {
            get { return swversions; }
            set { swversions = value; }
        }

        private string _carDescription = string.Empty;

        public string CarDescription
        {
            get { return _carDescription; }
            set { _carDescription = value; }
        }

        private EngineType _enginetype = EngineType.Unknown;

        internal EngineType Enginetype
        {
            get { return _enginetype; }
            set { _enginetype = value; }
        }

        private CarModel _carmodel = CarModel.Unknown;

        internal CarModel Carmodel
        {
            get { return _carmodel; }
            set { _carmodel = value; }
        }

        private TurboModel _turbomodel = TurboModel.None;

        internal TurboModel Turbomodel
        {
            get { return _turbomodel; }
            set { _turbomodel = value; }
        }


        private bool _valid = false;

        public bool Valid
        {
            get { return _valid; }
            set { _valid = value; }
        }
        private bool _isturbo = false;

        public bool Isturbo
        {
            get { return _isturbo; }
            set { _isturbo = value; }
        }
        
        private bool _isfpt = false;

        public bool Isfpt
        {
            get { return _isfpt; }
            set { _isfpt = value; }
        }
        private bool _is2point3liter = false;

        public bool Is2point3liter
        {
            get { return _is2point3liter; }
            set { _is2point3liter = value; }
        }

        private int _bhp = 0;

        public int Bhp
        {
            get { return _bhp; }
            set { _bhp = value; }
        }

        private int _torque = 0;

        public int Torque
        {
            get { return _torque; }
            set { _torque = value; }
        }

       

        private bool _automatic_gearbox = false;

        public bool Automatic_gearbox
        {
            get { return _automatic_gearbox; }
            set { _automatic_gearbox = value; }
        }

        private string _softwareID = string.Empty;

        public string SoftwareID
        {
            get { return _softwareID; }
            set { _softwareID = value; }
        }

    }
}
