using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Configuration;
using System.Collections.Specialized;
using System.Collections;

namespace rspatch
{
    class Patch
    {
        static byte[] HexToBytes(string hexString)
        {
            string[] hexValuesSplit = hexString.Trim().Split(' ');
            byte[] array = new byte[hexValuesSplit.Length];
            int i = 0;
            foreach (String hex in hexValuesSplit)
            {
                byte value = Convert.ToByte(hex, 16);
                array[i] = value;
                i++;
            }
            return array;
        }

        public Patch(string k, string v)
        {
            Desc = k;
            string[] words = v.Split('>');
            Filename = words[0].Replace("%ProgramFiles%",
                Environment.GetEnvironmentVariable("PROGRAMFILES(X86)") ??
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
            );
            Offset = Int32.Parse(words[1], System.Globalization.NumberStyles.HexNumber);
            Srch = HexToBytes(words[2]);
            Repl = HexToBytes(words[3]);

            Exists = File.Exists(Filename);
            Suitable = Exists && CheckFile();

        }

        public bool CheckFile()
        {
            byte[] inp = new byte[Srch.Length];
            using (var stream = new FileStream(Filename, FileMode.Open, FileAccess.Read))
            {
                stream.Position = Offset;
                stream.Read(inp, 0, Srch.Length);
                //Console.WriteLine(String.Format("{0:X}: {1}", Offset, BitConverter.ToString(inp)));
            }
            for (int i = 0; i < Srch.Length; i++)
                if (inp[i] != Srch[i]) return false;
            
            return true;
        }

        public bool Backup()
        {
            try
            {
                File.Copy(Filename, Filename + ".bak");
                return true;
            }
            catch (Exception)
            {
                Console.WriteLine(Filename + " Backup failed");
                throw;
            }

        }

        public bool Apply()
        {
            try
            {
                using (var stream = new FileStream(Filename, FileMode.Open, FileAccess.ReadWrite))
                {
                    stream.Position = Offset;
                    stream.Write(Repl, 0, Srch.Length);
                }
                return true;
            }
            catch (Exception)
            {
                Console.WriteLine(Filename + " Patch failed");
                throw;
            }
            
        }

        public string Desc;
        public string Filename;
        public long Offset;
        public byte[] Srch;
        public byte[] Repl;
        public bool Exists;
        public bool Suitable;
    }

    

    class Program
    {
        static void Main(string[] args)
        {
            List<Patch> patches = new List<Patch>();

            NameValueCollection appSettings = ConfigurationManager.AppSettings;  
            IEnumerator appSettingsEnum = appSettings.Keys.GetEnumerator();

            int i = 0;
            while (appSettingsEnum.MoveNext())
            {
                string k = appSettings.Keys[i];
                string v = appSettings[k];

                patches.Add(new Patch(k, v));

                i++;
            }

            List<Patch> actions = new List<Patch>();
            foreach (Patch p in patches)
            {
                Console.WriteLine(p.Desc);
                Console.WriteLine(String.Format("{0} {1} {2}", p.Filename, p.Exists, p.Suitable));
                //Console.WriteLine(String.Format("{0:X}: {1} > {2}", a.Offset, BitConverter.ToString(a.Srch), BitConverter.ToString(a.Repl)));
                Console.WriteLine("-------------");

                if (p.Suitable) actions.Add(p);
            }

            Console.WriteLine("-------------");

            if (actions.Count < 1)
            {
                Console.WriteLine("No files");
                Console.ReadLine();
                return;
            }

            foreach (Patch a in actions)
            {
                Console.WriteLine(a.Desc);
                Console.WriteLine(String.Format("{0}", a.Filename));
                //Console.WriteLine(String.Format("{0:X}: {1} > {2}", a.Offset, BitConverter.ToString(a.Srch), BitConverter.ToString(a.Repl)));
                Console.WriteLine("-------------");
            }

            Console.WriteLine("Patch?");
            if (String.Equals(Console.ReadLine(), "y", StringComparison.CurrentCultureIgnoreCase))
            
                foreach (Patch a in actions)
                {
                    try
                    {
                        a.Backup();
                        a.Apply();
                        Console.WriteLine("OK");
                    }
                    catch (Exception)
                    {
                        continue;
                        throw;
                    }
                }

            Console.ReadLine();
        }
    }
}
