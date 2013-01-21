using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MotronicSuite
{
    public class DTCCodeTranslator
    {
        public string TranslateDTCCode(int code)
        {
            string retval = "Unknown code: " + code.ToString();
            switch (code)
            {
                case 111:
                    retval = "1-1-1 No DTC set";
                    break;
                case 112:
                    retval = "1-1-2 MFI Control Module (CM) fault";
                    break;
                case 115:
                    retval = "1-1-5 Injector 1";
                    break;
                case 121:
                    retval = "1-2-1 MAF sensor signal";
                    break;
                case 123:
                    retval = "1-2-3 ECT sensor signal";
                    break;
                case 125:
                    retval = "1-2-5 Injector 2";
                    break;
                case 131:
                    retval = "1-3-1 RPM sensor signal";
                    break;
                case 132:
                    retval = "1-3-2 Battery voltage";
                    break;
                case 135:
                    retval = "1-3-5 Injector 3";
                    break;
                case 143:
                    retval = "1-4-3 Front knock sensor (KS)";
                    break;
                case 153:
                    retval = "1-5-3 HO2S sensor signal, rear";
                    break;
                case 154:
                    retval = "1-5-4 EGR system leakage";
                    break;
                case 155:
                    retval = "1-5-5 Injector 5";
                    break;
                case 212:
                    retval = "2-1-2 HO2S sensor signal, front";
                    break;
                case 214:
                    retval = "2-1-4 RPM sensor signal sporadic faulty";
                    break;
                case 223:
                    retval = "2-2-3 Idle Air Control (IAC) valve";
                    break;
                case 225:
                    retval = "2-2-5 A/C pressure sensor signal";
                    break;
                case 231:
                    retval = "2-3-1 Long term fuel trim, part load";
                    break;
                case 232:
                    retval = "2-3-2 Long term fuel trim, idling";
                    break;
                case 233:
                    retval = "2-3-3 Long term idle air trim";
                    break;
                case 241:
                    retval = "2-4-1 EGR system";
                    break;
                case 245:
                    retval = "2-4-5 IAC valve closing signal";
                    break;
                case 311:
                    retval = "3-1-1 Vehicle speed sensor (VSS) signal";
                    break;
                case 314:
                    retval = "3-1-4 Camshaft Position sensor (CMP) signal";
                    break;
                case 315:
                    retval = "3-1-5 EVAP system";
                    break;
                case 325:
                    retval = "3-2-5 Memory failure";
                    break;
                case 332:
                    retval = "3-3-2 TP potentiometer, when throttle is moved to/from CTP";
                    break;
                case 335:
                    retval = "3-3-5 Request for MIL lighting from TCM";
                    break;
                case 411:
                    retval = "4-1-1 Throttle Position (TP) sensor signal";
                    break;
                case 413:
                    retval = "4-1-3 EGR temperature sensor signal";
                    break;
                case 414:
                    retval = "4-1-4 Boost pressure regulation";
                    break;
                case 416:
                    retval = "4-1-6 Boost pressure reduction from TCM";
                    break;
                case 432:
                    retval = "4-3-2 Temperature warning, level 1";
                    break;
                case 433:
                    retval = "4-3-3 Rear knock sensor (KS)";
                    break;
                case 435:
                    retval = "4-3-5 Front HO2S";
                    break;
                case 436:
                    retval = "4-3-6 Rear HO2S";
                    break;
                case 443:
                    retval = "4-4-3 TWC efficiency";
                    break;
                case 444:
                    retval = "4-4-4 Acceleration sensor signal";
                    break;
                case 451:
                    retval = "4-5-1 Misfire cylinder 1";
                    break;
                case 452:
                    retval = "4-5-2 Misfire cylinder 2";
                    break;
                case 453:
                    retval = "4-5-3 Misfire cylinder 3";
                    break;
                case 454:
                    retval = "4-5-4 Misfire cylinder 4";
                    break;
                case 455:
                    retval = "4-5-5 Misfire cylinder 5";
                    break;
                case 513:
                    retval = "5-1-3 Temperature warning, level 2";
                    break;
                case 514:
                    retval = "5-1-4 Engine coolant fan, low speed";
                    break;
                case 521:
                    retval = "5-2-1 Front HO2S, preheating";
                    break;
                case 522:
                    retval = "5-2-2 Rear HO2S, preheating";
                    break;
                case 531:
                    retval = "5-3-1 Power stage group A";
                    break;
                case 532:
                    retval = "5-3-2 Power stage group B";
                    break;
                case 533:
                    retval = "5-3-3 Power stage group C";
                    break;
                case 534:
                    retval = "5-3-4 Power stage group D";
                    break;
                case 535:
                    retval = "5-3-5 TC control valve signal";
                    break;
                case 541:
                    retval = "5-4-1 EVAP valve signal";
                    break;
                case 542:
                    retval = "5-4-2 Misfire, more than 1 cylinder";
                    break;
                case 543:
                    retval = "5-4-3 Misfire, at least 1 cylinder";
                    break;
                case 544:
                    retval = "5-4-4 Misfire, more than 1 cylinder";
                    break;
                case 545:
                    retval = "5-4-5 Misfire, at least 1 cylinder";
                    break;
                case 551:
                    retval = "5-5-1 Misfire, cylinder 1";
                    break;
                case 552:
                    retval = "5-5-2 Misfire, cylinder 2";
                    break;
                case 553:
                    retval = "5-5-3 Misfire, cylinder 3";
                    break;
                case 554:
                    retval = "5-5-4 Misfire, cylinder 4";
                    break;
                case 555:
                    retval = "5-5-5 Misfire, cylinder 5";
                    break;
            }
            return retval;
        }

    }
}
