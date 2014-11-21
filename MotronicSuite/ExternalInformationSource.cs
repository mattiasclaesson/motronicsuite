using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MotronicTools;
using System.IO;

// must provide data from damos files and *.csv files in a certain format

namespace MotronicSuite
{
    public enum SourceType : int
    {
        Damos,
        CSV
    }

    public class ExternalInformationSource
    {
        public void FillSymbolCollection(string sourcefile, SourceType type, SymbolCollection symbols, AxisCollection axis, bool applyOffset)
        {
            switch (type)
            {
                case SourceType.CSV:
                    ParseCSVFile(sourcefile, symbols, axis, applyOffset);
                    break;
                case SourceType.Damos:
                    ParseDamosFile(sourcefile, symbols, axis, applyOffset);
                    break;
            }
        }

        private void ParseDamosFile(string sourcefile, SymbolCollection symbols, AxisCollection axis, bool applyOffset)
        {
            // parses information from .damos file
            char[] sep = new char[1];
            sep.SetValue(',', 0);
            int symbolindex = 0;
            bool _nextLineIsAddress = false;
            bool _isAxisType = false;
            bool _isMapType = false;
            string currMapName = string.Empty;
            if (File.Exists(sourcefile))
            {

                string[] lines = File.ReadAllLines(sourcefile, Encoding.GetEncoding("ISO-8859-1"));
                foreach (string line in lines)
                {
                    string[] values = line.Split(sep);
                    // check if 1st value is numeric and number of values >= 4
                    if (values.Length >= 2)
                    {
                        if (_nextLineIsAddress)
                        {
                            // $C8FE,$C8FE
                            //Console.WriteLine(line);
                            if (line.StartsWith("$"))
                            {
                                _nextLineIsAddress = false;
                                int fromAddress = 0;
                                int uptoAddress = 0;
                                try
                                {
                                    fromAddress = Convert.ToInt32(values.GetValue(0).ToString().Trim().Replace("$", ""), 16);
                                    uptoAddress = Convert.ToInt32(values.GetValue(1).ToString().Trim().Replace("$", ""), 16);
                                    if (applyOffset)
                                    {
                                        if (uptoAddress < 0x10000)
                                        {
                                            uptoAddress += 0xDE;
                                            fromAddress += 0xDE;
                                        }
                                        else
                                        {
                                            uptoAddress -= 0x01;
                                            fromAddress -= 0x01;
                                        }
                                    }
                                    if (fromAddress != 0 && uptoAddress != 0)
                                    {
                                        bool _fnd = false;
                                        foreach (SymbolHelper sh in symbols)
                                        {
                                            if (sh.Flash_start_address == uptoAddress)
                                            {
                                                sh.Varname/*.UserDescription*/ = currMapName;
                                                //Console.WriteLine("Userdescription for : " + sh.Varname);
                                                if (sh.Varname/*.UserDescription*/.Contains("."))
                                                {
                                                    try
                                                    {
                                                        sh.Category = sh.Varname/*.UserDescription*/.Substring(0, sh.Varname/*UserDescription*/.IndexOf("."));
                                                    }
                                                    catch (Exception cE)
                                                    {
                                                        Console.WriteLine("Failed to assign category to symbol: " + sh.Varname + " err: " + cE.Message);
                                                    }
                                                }
                                                if (sh.Length == 0x3B)
                                                {
                                                    //sh.Cols = 9;
                                                    //sh.Rows = 5;
                                                    sh.Length = 42;
                                                    sh.Cols = 7;
                                                    sh.Rows = 6; // overrule PID maps
                                                }
                                                _fnd = true;
                                                break;
                                            }
                                        }
                                        foreach (AxisHelper ah in axis)
                                        {
                                            if (ah.Addressinfile == uptoAddress)
                                            {
                                                ah.Descr = currMapName;
                                                ah.M44DamosID = symbolindex;
                                                _fnd = true;
                                                break;
                                            }
                                        }
                                        if (!_fnd)
                                        {
                                            SymbolHelper sh = new SymbolHelper();
                                            sh.Varname = uptoAddress.ToString("X4");
                                            sh.Varname/*.UserDescription*/ = currMapName;
                                            sh.Flash_start_address = uptoAddress;
                                            sh.Length = uptoAddress - fromAddress + 1;
                                            if (sh.Length == 1) // if offset ... settings are distorted, we need another damos file for this
                                            {
                                                //Console.WriteLine("Len 1: " + currMapName + " " + " at " + sh.Flash_start_address.ToString("X8"));
                                                sh.Varname/*.UserDescription*/ = "Settings and options." + sh.Varname/*.UserDescription*/;
                                            }
                                            if (sh.Length <= 0x100)
                                            {
                                               // Console.WriteLine("Adding : " + sh.Varname);
                                                if (sh.Varname/*.UserDescription*/.Contains("."))
                                                {
                                                    try
                                                    {
                                                        sh.Category = sh.Varname/*.UserDescription*/.Substring(0, sh.Varname/*.UserDescription*/.IndexOf("."));
                                                    }
                                                    catch (Exception cE)
                                                    {
                                                        Console.WriteLine("Failed to assign category to symbol: " + sh.Varname + " err: " + cE.Message);
                                                    }
                                                }
                                                if (sh.Length > 1 || !applyOffset)
                                                {
                                                    symbols.Add(sh);
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (Exception E)
                                {
                                    Console.WriteLine("Failed to interpret " + line + " as addressline: " + E.Message);
                                }
                            }
                        }
                        
                        else if(!line.StartsWith("/"))
                        {
                            if (values.Length >= 4)
                            {
                                _isMapType = false;
                                _isAxisType = false;
                                if (Int32.TryParse(values.GetValue(0).ToString(), out symbolindex))
                                {
                                    
                                    currMapName = values.GetValue(3).ToString().Replace("{", "").Replace("}", "").Trim();
                                    currMapName = ReplaceForeignCharacters(currMapName);
                                   /* if (currMapName == "Erhohung Luftvorsteuerung b Kat-Heizung")
                                    {
                                        Console.WriteLine("Erhohung Luftvorsteuerung b Kat-Heizung");
                                    }*/
                                    _nextLineIsAddress = true;

                                    int mapType = 0;
                                    if (values.Length > 4)
                                    {
                                        if (Int32.TryParse(values.GetValue(4).ToString(), out mapType))
                                        {
                                            // mapType 6 means support points... try to add these later to the maps
                                            if (mapType == 6)
                                            {
                                                _isAxisType = true;
                                            }
                                            else
                                            {
                                                _isMapType = true;
                                            }
                                        }
                                    }
                                    /*else
                                    {
                                        // toevoegen?
                                        currMapName = values.GetValue(3).ToString().Replace("{", "").Replace("}", "").Trim();
                                        currMapName = ReplaceForeignCharacters(currMapName);
                                        Console.WriteLine("Ignoring: " + currMapName);
                                       // _nextLineIsAddress = true;
                                    }*/
                                }
                            }
                        }
                        if (_isAxisType)
                        {
                            /*if (line.StartsWith("/SPX"))
                            {
                                _isAxisType = false;
                                // which one did it belong to?
                                try
                                {
                                    int axisID = Convert.ToInt32(values.GetValue(1));
                                    // assign it to the current map
                                    foreach (AxisHelper ah in axis)
                                    {
                                        if (ah.Descr == currMapName)
                                        {
                                            ah.M44DamosID = axisID;
                                        }
                                    }
                                }
                                catch (Exception E)
                                {
                                    Console.WriteLine("Failed to determine axisID: " + line);
                                }
                            }*/
                        }
                        if(_isMapType)
                        {

                            if (line.StartsWith("/SPX"))
                            {
                                // which one did it belong to?
                                try
                                {
                                    int axisID = Convert.ToInt32(values.GetValue(5));
                                    // assign it to the current map
                                    foreach (SymbolHelper sh in symbols)
                                    {
                                        if (sh.Varname/*.UserDescription*/ == currMapName)
                                        {
                                            sh.M44DamosXAxisID = axisID;
                                            break;
                                        }
                                    }
                                }
                                catch (Exception E)
                                {
                                    Console.WriteLine("Failed to determine map X axisID: " + line);
                                }
                            }
                            else if(line.StartsWith("/SPY"))
                            {
                                _isMapType = false; // we got it.
                                // which one did it belong to?
                                try
                                {
                                    int axisID = Convert.ToInt32(values.GetValue(5));
                                    // assign it to the current map
                                    foreach (SymbolHelper sh in symbols)
                                    {
                                        if (sh.Varname/*.UserDescription*/ == currMapName)
                                        {
                                            sh.M44DamosYAxisID = axisID;
                                            break;
                                        }
                                    }
                                }
                                catch (Exception E)
                                {
                                    Console.WriteLine("Failed to determine map Y axisID: " + line);
                                }
                            }
                        }
                    }
                }

                // now link all axis and maps together
                foreach (SymbolHelper sh in symbols)
                {
                    if (sh.M44DamosXAxisID > 0)
                    {
                        foreach (AxisHelper ah in axis)
                        {
                            if (ah.M44DamosID == sh.M44DamosYAxisID && sh.Rows == ah.Length) // length
                            {
                                sh.X_axis_address = ah.Addressinfile;
                                sh.X_axis_length = ah.Length;
                                //ah.CalculateRealValues();
                               // sh.XDescr = ah.Descr;
                                break;
                            }
                        }
                    }
                    if (sh.M44DamosYAxisID > 0)
                    {
                        foreach (AxisHelper ah in axis)
                        {
                            if (ah.M44DamosID == sh.M44DamosXAxisID && sh.Cols == ah.Length)
                            {
                                sh.Y_axis_address = ah.Addressinfile;
                                sh.Y_axis_length = ah.Length;
                                //ah.CalculateRealValues();
                               // sh.YDescr = ah.Descr;
                                break;
                            }
                        }
                    }
                    
                }
            }
        }

        private string ReplaceForeignCharacters(string currMapName)
        {
            string retval = currMapName;
            return retval;
            retval = retval.Replace("\x94", "o");
            retval = retval.Replace("\x9A", "U");
            retval = retval.Replace("\x81", "u");
            retval = retval.Replace("\x84", "a");
            retval = retval.Replace("\x8E", "A");
            retval = retval.Replace("\x8F", "A");
            retval = retval.Replace("\x99", "O");
            retval = retval.Replace("\xE1", "ss");
            retval = retval.Replace(".", ""); // to prevent category assignment
            if (retval == "Zundwinkelkennfeld") retval = "Ignition.Ignition map";
            else if (retval == "Sollwertkennfeld fur LDR") retval = "Boost.Boost map";
            else if (retval == "Lambdakennfeld bei Teillast") retval = "Fuel.VE map: part load";
            else if (retval == "Wiederholkaltstartfaktor") retval = "Fuel.Restart factor (cold)";
            else if (retval == "Wiederholstartzeitfaktor") retval = "Fuel.Restart time factor";
            else if (retval == "Tastverhaltnisvorsteuerung fur LDR") retval = "Boost.Bias values on BCV";
            else if (retval == "Klopferkennungsfaktorkennfeld") retval = "Knock.Knock detection threshold";
            else if (retval == "Kennfeld Winkel Einspritzbeginn") retval = "Fuel.Injection start angle";
            else if (retval == "Schliesswinkelkennfeld") retval = "Fuel.Injection end angle";
            else if (retval == "Vollastkorrektur") retval = "Fuel.WOT injection map";
            else if (retval == "I - Kennfeld") retval = "Lambda.I factors for PID controller";
            else if (retval == "P - Kennfeld") retval = "Lambda.P factors for PID controller";
            else if (retval == "TV - Kennfeld") retval = "Lambda.D factors for PID controller";
            else if (retval == "weiche Geschwindigkeitsbegrenzung") retval = "Limiters.Soft vehicle speed limiter";
            else if (retval == "Geschwindigkeitsschwelle fur Schubabschalten") retval = "Limiters.Vehicle speed limit for fuelcut";
            else if (retval == "Zeit fur Ladedruckbegrenzung") retval = "Limiters.Time for boost pressure limiter";
            else if (retval == "Drehzahlbegrenzung") retval = "Limiters.Engine speed (RPM) limit";
            else if (retval == "Drosselklappenwinkelschwelle fur Vollasterkennung") retval = "Misc.TPS opening for WOT detection";
            else if (retval.ToLower().Contains("stuetzstellen")) retval = "Supportpoints." + retval;
            else if (retval.ToLower().Contains("stutzstellen")) retval = "Supportpoints." + retval;
            else
            {
                return retval;
                retval = retval.ToLower();

                retval = retval.Replace("drehzahl", "RPM");
                retval = retval.Replace("schwelle", "threshold");
                retval = retval.Replace("geschwindigkeits", "speed");
                retval = retval.Replace("geschwindigkeit", "speed");
                retval = retval.Replace("weiche", "soft");
                retval = retval.Replace("begrenzung", "limiter");
                retval = retval.Replace("tankentluftung", "tank venting");
                retval = retval.Replace("motortemperatur", "engine temperature");
                retval = retval.Replace("warmstarttemeratur", "warmstart temperature");
                retval = retval.Replace(" fur ", " for ");
                retval = retval.Replace("fehler", "error");
                retval = retval.Replace("lampe", "light");
                retval = retval.Replace("druck", "pressure");
                retval = retval.Replace("differenz", "difference");
                retval = retval.Replace("zundungs", "ignition");
                retval = retval.Replace("zundung", "ignition");
                retval = retval.Replace("kraftstoff", "fuel");
                retval = retval.Replace("last", "load");
                retval = retval.Replace("erkennung", "detection");
                retval = retval.Replace("temperatur", "temperature");
                retval = retval.Replace("untere", "lower");
                retval = retval.Replace("obere", "upper");
                retval = retval.Replace("tastverhaltnist", "duty cycle");
                retval = retval.Replace("sekundar", "secondary");
                retval = retval.Replace("luft", "air");
                retval = retval.Replace("erkennung", "detection");
                retval = retval.Replace("beladung", "load");
                retval = retval.Replace("versorgungs", "delivery");
                retval = retval.Replace("drosselklappen", "throttle plate");
                retval = retval.Replace("kompensations", "compensation");
                retval = retval.Replace("gradienten", "gradients");
                retval = retval.Replace("aktiv", "active");
                retval = retval.Replace("regelung", "control");
                retval = retval.Replace("zundwinkel", "ignition angle");
                retval = retval.Replace("winkel", "angle");
                retval = retval.Replace("ladedruck", "boostpressure");
                retval = retval.Replace("sonden", "probe");
                retval = retval.Replace("ausgabe", "output");
                retval = retval.Replace("regelung", "control");
                retval = retval.Replace("vor", "before");
                retval = retval.Replace("kat", "catalyst");
                retval = retval.Replace("potentialversatz", "voltage offset");
                retval = retval.Replace("hochohmiger", "high impedance");
                retval = retval.Replace("nach masse", "to ground");
                retval = retval.Replace("nach ubat", "to battery voltage");
                retval = retval.Replace("bereich", "range");
                retval = retval.Replace("periodendauer", "period (duration) ");

                retval = retval.Replace("kurztest", "shorttest");
                retval = retval.Replace("obere", "upper");
                retval = retval.Replace("untere", "lower");
                retval = retval.Replace("zeit", "time");
                retval = retval.Replace("maximale", "maximum");
                retval = retval.Replace("minimale", "minimum");
                retval = retval.Replace("abschaltung", "shutdown");
                retval = retval.Replace("motorairersteuerung", "engine airmass control");
                retval = retval.Replace("frequenzumschaltung", "frequency switch");
                retval = retval.Replace("airmengen", "airmass");
                retval = retval.Replace(" zur ", " to ");
                retval = retval.Replace("activierung", "activation");
                retval = retval.Replace("dynamik", "dynamic");
                retval = retval.Replace("dauerhaft", "permanently");
                retval = retval.Replace("offen", "open");
                retval = retval.Replace("ausschalt", "switchoff");
                retval = retval.Replace("zahler", "counter");
                retval = retval.Replace(" zu ", " to ");
                retval = retval.Replace("umschaltung", "switchover");
                retval = retval.Replace("fahrzeug", "vehicle");
                retval = retval.Replace("fahr", "driving");
                retval = retval.Replace("summe", "sum");
                retval = retval.Replace("spatverstellung", "ignition retard");
                retval = retval.Replace("spatverst", "ignition retard");
                retval = retval.Replace("regel", "control");
                retval = retval.Replace("nebenschluss", "shunt");
                retval = retval.Replace("kurzschluss", "short circuit");
                retval = retval.Replace(" bei der ", " at ");
                retval = retval.Replace(" bei ", " at ");
                retval = retval.Replace("neustart", "restart");
                retval = retval.Replace("sollwertes", "target value");
                retval = retval.Replace("sollwert", " target value");
                retval = retval.Replace("laufunruhe", "rough running");
                retval = retval.Replace("berechnung", "calculation");
                retval = retval.Replace("einschalt", "switch-on");
                retval = retval.Replace("abgas", "exhaust");
                retval = retval.Replace("abschalt", "switch-off");
                retval = retval.Replace("hinter", "rear");
                retval = retval.Replace("heizungs", "heating");
                retval = retval.Replace("heizung", "heating");
                retval = retval.Replace("widerstand", "resistance");
                retval = retval.Replace("handschalt-fz", "manual gearbox car");
                retval = retval.Replace("automatic-fz", "automatic gearbox car");
                retval = retval.Replace("handschalt", "manual gearbox");
                retval = retval.Replace("solldrehzahl", "target engine speed");
                retval = retval.Replace("uberwachung", "check");
                retval = retval.Replace("funktionsprufung", "functioncheck");
                retval = retval.Replace("fremdbestimmt", "other-directed");
                retval = retval.Replace("messfenster", "measurement window");
                retval = retval.Replace("maximalwert", "maximum value");
                retval = retval.Replace("minimalwert", "minimum value");
                retval = retval.Replace("regelabweichung", "control deveation");
                retval = retval.Replace("ersatzwert", "replacement value");
                retval = retval.Replace("einschaltbereitschaft", "ready to start");
                retval = retval.Replace("klopf", "knock");
                retval = retval.Replace("entprellung", "debounce");
                retval = retval.Replace("abbruch", "cancellation");
                retval = retval.Replace("zahnscheiben", "toothed ring");
                retval = retval.Replace("anderung", "change");
                retval = retval.Replace("verbot", "prohibit");
                retval = retval.Replace("feinleckprufung", "small leak check");
                retval = retval.Replace("amplitudenverhaltnis", "amplitude ratio");
                retval = retval.Replace("schwapp", "splash");
                retval = retval.Replace("rucksetzen", "reset");
                retval = retval.Replace("ubergang", "transition");
                retval = retval.Replace("leck", "leak");
                retval = retval.Replace("hohenabhangige", "altitude dependent");
                retval = retval.Replace("hohe", "high");
                retval = retval.Replace("absenkung", "lowering");
                retval = retval.Replace("getriebe", "transmission");
                retval = retval.Replace("eingriff", "intervention");
                retval = retval.Replace("absteuerungsfaktorumschalt", "control settings factor for switchover");
                retval = retval.Replace("nachstart", "afterstart");
                retval = retval.Replace("abhebung", "increase");
                retval = retval.Replace("betriebsbereitschaft", "operational readiness");
                retval = retval.Replace("magerem", "lean");
                retval = retval.Replace("fettem", "rich");
                retval = retval.Replace("fett", "rich");
                retval = retval.Replace("mager", "lean");
                retval = retval.Replace(" von ", " of ");
                retval = retval.Replace("gemisch", "mixture");
                retval = retval.Replace(" des ", " of ");
                retval = retval.Replace("adernschluss", "core circuit");
                retval = retval.Replace("batterie", "battery");
                retval = retval.Replace("spannungs", "voltage");
                retval = retval.Replace("spanning", "voltage");
                retval = retval.Replace("konstante", "constant");
                retval = retval.Replace(" beim ", " when ");
                retval = retval.Replace("kraft", "fuel");
                retval = retval.Replace("grund", "basic");
                retval = retval.Replace("offen", "open");
                retval = retval.Replace("eingeschwungenen", "steady");
                retval = retval.Replace("kalt", "cold");
                retval = retval.Replace("heiss", "hot");
                retval = retval.Replace("zw-fruhverstellung", "ignition advance");
                retval = retval.Replace("fruhverstellung", "advance");
                retval = retval.Replace("lastdynamik", "load dynamics");
                retval = retval.Replace("nachspritzer", "supplementary injection");
                retval = retval.Replace("ersatz", "replacement");
                retval = retval.Replace("abweichung", "deviation");
                retval = retval.Replace("erfassung", "aquisition");
                retval = retval.Replace("ausserhalb", "outside of");
                retval = retval.Replace("plausibilitatsprufung", "plausibility check");
                retval = retval.Replace("schub", "thrust");
                retval = retval.Replace("beschleunigungs", "acceleration");
                retval = retval.Replace("beschleunigung", "acceleration");
                retval = retval.Replace("anreicherung", "enrichment");
                retval = retval.Replace("referenz", "reference");
                retval = retval.Replace("basiswert", "basevalue");
                retval = retval.Replace("kennfeld", "map");
                retval = retval.Replace("leerlauf", "idle");
                retval = retval.Replace("einspritz", "injection");
                retval = retval.Replace("beginn", "start");
                retval = retval.Replace("zusatz", "additional");
                retval = retval.Replace("tastverhaltnis", "duty cycle");
                retval = retval.Replace("korrektur", "correction");
                retval = retval.Replace("klimakompressors", "climate control");
                retval = retval.Replace("klimakompressor", "climate control");
                retval = retval.Replace("schliess", "closing");
                retval = retval.Replace("volumenstrom", "airflow");
                retval = retval.Replace("teillast", "part-load");
                retval = retval.Replace("kurz", "short");
                retval = retval.Replace("lang", "long");
                retval = retval.Replace("anteil", "part");
                //retval = retval.Replace("", "");
                //retval = retval.Replace("", "");

                retval = retval.Replace("Codewort Tester", "Tester.Codeword ");
            }

            /*
Lambdakennfeld bei Teillast = Fuel.VE map: part load
Wiederholkaltstartfaktor = Fuel.Restart factor (cold)
Wiederholstartzeitfaktor = Fuel.Restart time factor
Tastverhaltnisvorsteuerung fur LDR = Boost.Bias values on BCV
Klopferkennungsfaktorkennfeld = Knock.Knock detection threshold
Kennfeld Winkel Einspritzbeginn = Fuel.Injection start angle
Schliesswinkelkennfeld = Fuel.Injection end angle
Vollastkorrektur = Fuel.WOT injection map
             * */
            //TODO: add main ignition map and boost map
            return retval;
        }

        private void ParseCSVFile(string sourcefile, SymbolCollection symbols, AxisCollection axis, bool applyOffset)
        {
            // parses information from a CSV file
            char[] sep = new char[1];
            sep.SetValue(',', 0);
            if (File.Exists(sourcefile))
            {
                string[] lines = File.ReadAllLines(sourcefile);
                //"ID","Address","Name","Size","Organization","Description","Units","X Units","Y Units","Scale","X Scale","Y Scale","Value min","Value max","Value min*1","Value max*1",
                //"A0","0x15b28","Übertragungsfunktionskoeffizient","1x1","16 Bit (LoHi)","","-","-","-","1.9073486328125E-6","1.0","1.0","0.0308074951171875","0.0308074951171875","0x3f18","0x3f18",
                foreach (string line in lines)
                {
                    string[] values = line.Split(sep);
                    int addressinFile = Convert.ToInt32(values.GetValue(1).ToString().Trim().Replace("0x", ""), 16);
                    string name = values.GetValue(2).ToString().Trim();
                    string description = values.GetValue(5).ToString().Trim();
                    foreach (SymbolHelper sh in symbols)
                    {
                        if (sh.Flash_start_address == addressinFile)
                        {
                            sh.Varname/*.UserDescription*/ = ReplaceForeignCharacters(name);
                            sh.Description = ReplaceForeignCharacters(description);
                            if (sh.Varname/*.UserDescription*/.Contains("."))
                            {
                                try
                                {
                                    sh.Category = sh.Varname/*.UserDescription*/.Substring(0, sh.Varname/*.UserDescription*/.IndexOf("."));
                                }
                                catch (Exception cE)
                                {
                                    Console.WriteLine("Failed to assign category to symbol: " + sh.Varname + " err: " + cE.Message);
                                }
                            }

                            break;
                        }
                    }
                }

            }
        }
    }
}
