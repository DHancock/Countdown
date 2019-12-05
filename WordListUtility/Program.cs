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

  
        static int Main(string[] args)
        {
            try
            {
                App app = new App(args);
                return app.Run();
            }
            catch
            {
            }
          
            return error_fail;
        }



        private class App
        {
            private string InputDir { get; }
            private string OutputFile { get; }
            private Dictionary<string, List<string>> WordLists { get; } = new Dictionary<string, List<string>>();


            public App(string[] args)
            {
                if (args.Length != 2)
                    throw new ArgumentOutOfRangeException();

                InputDir = args[0];
                OutputFile = args[1];

                if (!Directory.Exists(InputDir))
                    throw new DirectoryNotFoundException();

                if (!Directory.Exists(Path.GetDirectoryName(OutputFile)))
                    throw new DirectoryNotFoundException();
            }


            public int Run()
            {
                foreach (string file in Directory.EnumerateFiles(InputDir, "*.txt"))
                {
                    ReadSource(file);
                }

                if (WordLists.Count > 0)
                {
                    WriteCompressedData();
                    return error_success;
                }
            
                return error_fail;
            }



            private static bool IsLetter(char c)
            {
                return (c >= 'a') && (c <= 'z');
            }



            private void ReadSource(string path)
            {
                const int min_word_length = 4;
                const int max_word_length = 9;

                FileStream fs = File.OpenRead(path);

                if (fs != null)
                {
                    using StreamReader sr = new StreamReader(fs);
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

                            if (WordLists.TryGetValue(key, out List<string> list))
                            {
                                int index = list.BinarySearch(data);

                                if (index < 0)
                                    list.Insert(~index, data);
                            }
                            else
                                WordLists[key] = new List<string>() { data };
                        }
                    }
                }
            }


           
            private void WriteCompressedData()
            {
                const string word_seperator = " ";
                const string line_seperator = ".";

                FileStream fs = File.Create(OutputFile);

                if (fs != null)
                {
                    using DeflateStream ds = new DeflateStream(fs, CompressionLevel.Optimal);

                    foreach (List<string> list in WordLists.Values)
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
