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
            if (args.Length != 2)
                throw new ArgumentOutOfRangeException(nameof(args));

            inputDir = args[0];
            outputFile = args[1];

            if (!Directory.Exists(inputDir))
                throw new DirectoryNotFoundException();

            if (!Directory.Exists(Path.GetDirectoryName(outputFile)))
                throw new DirectoryNotFoundException();
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

            using FileStream fs = File.OpenRead(path);

            if (fs != null)
            {
                using StreamReader sr = new StreamReader(fs);
                string? line;

                while ((line = sr.ReadLine()) != null)
                {
                    string data = line.Trim().ToLower();

                    if ((data.Length >= min_word_length) && (data.Length <= max_word_length) && data.All(c => IsLetter(c)))
                    {
                        // construct the key
                        char[] a = data.ToCharArray();
                        Array.Sort(a);
                        string key = new string(a);

                        if (wordLists.TryGetValue(key, out List<string>? list))
                        {
                            Debug.Assert(list is not null);
                            int index = list.BinarySearch(data);

                            if (index < 0)
                                list.Insert(~index, data);
                        }
                        else
                            wordLists[key] = new List<string>() { data };
                    }
                }
            }
        }
       
        private void WriteCompressedData()
        {
            const string word_seperator = " ";
            const string line_seperator = ".";

            using FileStream fs = File.Create(outputFile);

            if (fs != null)
            {
                using DeflateStream ds = new DeflateStream(fs, CompressionLevel.Optimal);

                foreach (List<string> list in wordLists.Values)
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
