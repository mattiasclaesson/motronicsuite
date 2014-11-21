using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Forms;
using Sim8051Sharp;


namespace MotronicSuite
{
    public class Disassembler
    {

        public enum ProgressType : int
        {
            DisassemblingVectors,
            DisassemblingFunctions,
            TranslatingVectors,
            AddingLabels,
            TranslatingLabels,
            SortingData,
            PassOne
        }

        public class ProgressEventArgs : System.EventArgs
        {
            private ProgressType _type;

            public ProgressType Type
            {
                get { return _type; }
                set { _type = value; }
            }

            private int _percentage;

            public int Percentage
            {
                get { return _percentage; }
                set { _percentage = value; }
            }

            private string _info;

            public string Info
            {
                get { return _info; }
                set { _info = value; }
            }

            public ProgressEventArgs(string info, int percentage, ProgressType type)
            {
                this._info = info;
                this._percentage = percentage;
                this._type = type;
            }
        }

        public delegate void Progress(object sender, ProgressEventArgs e);
        public event Disassembler.Progress onProgress;


        private void mRecreateAllExecutableResources()
        {
            // Get Current Assembly refrence
            Assembly currentAssembly = Assembly.GetExecutingAssembly();
            // Get all imbedded resources
            string[] arrResources = currentAssembly.GetManifestResourceNames();

            foreach (string resourceName in arrResources)
            {
                if (resourceName.EndsWith(".exe"))
                { //or other extension desired
                    //Name of the file saved on disk
                    string saveAsName = resourceName;
                    FileInfo fileInfoOutputFile = new FileInfo(System.Windows.Forms.Application.StartupPath + "\\" + saveAsName);
                    //CHECK IF FILE EXISTS AND DO SOMETHING DEPENDING ON YOUR NEEDS
                    if (fileInfoOutputFile.Exists)
                    {
                        //overwrite if desired  (depending on your needs)
                        //fileInfoOutputFile.Delete();
                    }
                    //OPEN NEWLY CREATING FILE FOR WRITTING
                    FileStream streamToOutputFile = fileInfoOutputFile.OpenWrite();
                    //GET THE STREAM TO THE RESOURCES
                    Stream streamToResourceFile =
                                        currentAssembly.GetManifestResourceStream(resourceName);

                    //---------------------------------
                    //SAVE TO DISK OPERATION
                    //---------------------------------
                    const int size = 4096;
                    byte[] bytes = new byte[4096];
                    int numBytes;
                    while ((numBytes = streamToResourceFile.Read(bytes, 0, size)) > 0)
                    {
                        streamToOutputFile.Write(bytes, 0, numBytes);
                    }

                    streamToOutputFile.Close();
                    streamToResourceFile.Close();
                }//end_if

            }//end_foreach
        }//end_mRecreateAllExecutableResources 


        public Disassembler()
        {


        }

        private byte[] readdatafromfile(string filename, int address, int length)
        {
            byte[] retval = new byte[length];
            FileStream fsi1 = File.OpenRead(filename);
            while (address > fsi1.Length) address -= (int)fsi1.Length;
            BinaryReader br1 = new BinaryReader(fsi1);
            fsi1.Position = address;
            string temp = string.Empty;
            for (int i = 0; i < length; i++)
            {
                retval.SetValue(br1.ReadByte(), i);
            }
            fsi1.Flush();
            br1.Close();
            fsi1.Close();
            fsi1.Dispose();
            return retval;
        }

        public string DisassembleFileSim8051(string m_currentfile)
        {
            Sim8051Dasm dasm = new Sim8051Dasm();
            frmProgress progress = new frmProgress();
            string outputfilename = Path.Combine(Path.GetDirectoryName(m_currentfile), Path.GetFileNameWithoutExtension(m_currentfile) + ".asm");
            progress.SetProgress("Initializing disassembler");
            progress.SetProgressPercentage(10);
            progress.Show();
            try
            {
                dasm.Initialize(readdatafromfile(m_currentfile, 0, 0x10000));
                SimError err;
                progress.SetProgress("Running disassembler");
                progress.SetProgressPercentage(20);
                string[] result = dasm.Disassemble(true, 0, out err);
                progress.SetProgress("Outputting data");
                progress.SetProgressPercentage(10);

                int linecount = 0;
                if (File.Exists(outputfilename))
                {
                    File.Delete(outputfilename);
                }
                using (StreamWriter sw = new StreamWriter(outputfilename))
                {
                    foreach (string s in result)
                    {
                        progress.SetProgressPercentage(((linecount++ * 80) / result.Length) + 20);
                        sw.WriteLine(s);

                    }
                }
            }
            catch (Exception E)
            {
                Console.WriteLine(E.Message);
            }
            progress.Close();
            return outputfilename;
        }

        public string DisassembleFile(string filename)
        {
            string retval = string.Empty;
            mRecreateAllExecutableResources();
            // run the disassembler on the inpout file
            string Exename = Path.Combine(Application.StartupPath, "T7.decode.exe");
            string outputfilename = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + ".asm");
            ProcessStartInfo startinfo = new ProcessStartInfo(Exename);
            startinfo.CreateNoWindow = true;
            startinfo.WindowStyle = ProcessWindowStyle.Hidden;
            startinfo.WorkingDirectory = Application.StartupPath;
            startinfo.Arguments = "-if " + filename + " -of " + outputfilename;
            System.Diagnostics.Process conv_proc = System.Diagnostics.Process.Start(startinfo);
            conv_proc.WaitForExit(10000); // wait for 10 seconds max
            if (conv_proc.HasExited)
            {
                retval = outputfilename;
            }
            else conv_proc.Kill();
            return retval;

        }

        
    }
}
