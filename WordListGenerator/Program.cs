using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace WordListGenerator;

internal sealed class Program
{
    const int error_success = 0;
    const int error_fail = 1;

    static int Main(string[] args)
    {
        //System.Diagnostics.Debugger.Launch();

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

    private sealed class App
    {
        private readonly string inputDir;
        private readonly string outputFile;
        private readonly Dictionary<string, List<string>> wordLists = new();

        public App(string[] args)
        {
            ArgumentOutOfRangeException.ThrowIfNotEqual(args.Length, 2, nameof(args));

            inputDir = args[0];
            outputFile = args[1];
        }

        public int Run()
        {
            foreach (string file in Directory.EnumerateFiles(inputDir, "*.txt"))
            {
                ReadSource(file);
            }

            if (wordLists.Count > 0)
            {
                WriteCompressedData();
                return error_success;
            }
        
            return error_fail;
        }

        private static bool IsLetter(char c) => c is >= 'a' and <= 'z';

        private void ReadSource(string path)
        {
            const int min_word_length = 4;
            const int max_word_length = 9;

            using (FileStream fs = File.OpenRead(path))
            {
                using (StreamReader sr = new StreamReader(fs))
                {
                    string? line;

                    while ((line = sr.ReadLine()) != null)
                    {
                        string data = line.Trim().ToLower();

                        if ((data.Length >= min_word_length) && (data.Length <= max_word_length) && data.All(c => IsLetter(c)))
                        {
                            string key = string.Create(data.Length, data, (chars, state) =>
                            {
                                state.AsSpan().CopyTo(chars);
                                chars.Sort();
                            });

                            if (wordLists.TryGetValue(key, out List<string>? list))
                            {
                                Debug.Assert(list is not null);
                                int index = list.BinarySearch(data);

                                if (index < 0)
                                {
                                    list.Insert(~index, data);
                                }
                            }
                            else
                            {
                                wordLists[key] = new List<string>() { data };
                            }
                        }
                    }
                }
            }
        }
       
        private void WriteCompressedData()
        {
            const char word_seperator = ' ';
            const char line_seperator = '\n';

            StringBuilder sb = new StringBuilder(100);

            using (FileStream fs = File.Create(outputFile))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    foreach (List<string> list in wordLists.Values)
                    {
                        if (list.Count > 0)
                        {
                            sb.Clear();
                            sb.AppendJoin(word_seperator, list);
                            sb.Append(line_seperator);

                            byte[] bytes = Encoding.ASCII.GetB‌​ytes(sb.ToString());
                            ms.Write(bytes, 0, bytes.Length);
                        }
                    }

                    using (DeflateStream ds = new DeflateStream(fs, CompressionLevel.Optimal))
                    {
                        ms.WriteTo(ds);
                    }
                }
            }
        }
    }
}
