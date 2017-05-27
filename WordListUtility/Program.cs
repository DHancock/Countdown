using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace WordListUtility
{
    class Program
    {
        const int error_success = 0;
        const int error_fail = 1;

        const int min_word_length = 4;
        const int max_word_length = 9;
        

        static int Main(string[] args)
        {
            try
            {
                App app = new App();
                return app.Run();
            }
            catch (Exception)
            {  
            }
          
            return error_fail;
        }



        private class App
        {
            Dictionary<string, List<string>> dictionary = new Dictionary<string, List<string>>();

            public int Run()
            {
                // caution - brittle path code follows,
                // its only intended to be run from within visual studio
                string input = Path.GetFullPath(@"..\..\Input");

                if (Directory.Exists(input))
                {
                    foreach (string file in Directory.EnumerateFiles(input, "*.txt"))
                    {
                        ReadSource(file);
                    }

                    if (dictionary.Count > 0)
                    {
                        string output = Path.GetFullPath(@"..\..\Output");

                        if (Directory.Exists(output))
                        {
                            WriteCompressedData(Path.Combine(output, "wordlist.dat"));
                            return error_success;
                        }
                    }
                }

                return error_fail;
            }



            private static bool IsLetter(char c)
            {
                return (c >= 'a') && (c <= 'z');
            }



            private void ReadSource(string path)
            {
                FileStream fs = File.OpenRead(path);

                if (fs != null)
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        String line;

                        while ((line = sr.ReadLine()) != null)
                        {
                            string data = line.Trim().ToLower();
                            
                            if ((data.Length >= min_word_length) && (data.Length <= max_word_length) && data.All(c => IsLetter(c)))
                            {
                                // construct the key
                                char[] a = data.ToCharArray();
                                Array.Sort(a);
                                string key = new string(a);

                                if (dictionary.TryGetValue(key, out List<string> list))
                                {       
                                    int index = list.BinarySearch(data);

                                    if (index < 0)
                                        list.Insert(~index, data);
                                }
                                else
                                    dictionary[key] = new List<string>() { data };
                            }
                        }
                    }
                }
            }


           
            private void WriteCompressedData(string path)
            {
                const string word_seperator = " ";
                const string line_seperator = ".";

                FileStream fs = File.Create(path);

                if (fs != null)
                {
                    using (DeflateStream ds = new DeflateStream(fs, CompressionLevel.Optimal))
                    {
                        foreach (List<string> list in dictionary.Values)
                        {
                            if (list.Count > 0)
                            {
                                string line = list[0];

                                for (int index = 1; index < list.Count; ++index)
                                    line += word_seperator + list[index];

                                line += line_seperator;

                                byte[] bytes = Encoding.ASCII.GetB‌​ytes(line);
                                ds.Write(bytes, 0, bytes.Length);
                            }
                        }
                    }
                }
            }
        }
    }
}
