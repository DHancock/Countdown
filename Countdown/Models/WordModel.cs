using Countdown.Utils;

namespace Countdown.Models;

internal class WordModel
{
    private const string cResourceName = "Countdown.Resources.wordlist.dat";

    public const int cMinLetters = 4;
    public const int cMaxLetters = 9;
    public const int cLetterCount = cMaxLetters;

    // conundrum words are 9 letters long with only one solution
    private readonly Dictionary<string, string[]> conundrumWords = new();
    private readonly Dictionary<string, string[]> otherWords = new();

    private readonly ConsonantList consonantList = new ConsonantList();
    private readonly VowelList vowelList = new VowelList();

    private readonly ManualResetEventSlim loadingEvent = new ManualResetEventSlim();

    public WordModel()
    {
        Task.Run(LoadResourceFile).ContinueWith(x => loadingEvent.Set());
    }

    public char GetVowel() => vowelList.GetLetter();

    public char GetConsonant() => consonantList.GetLetter();

    public IList<char> GenerateLettersData(int vowelCount)
    {
        char[] list = new char[cLetterCount];

        for (int index = 0; index < cLetterCount; index++)
        {
            if (index < vowelCount)
            {
                list[index] = GetVowel();
            }
            else
            {
                list[index] = GetConsonant();
            }
        }

        return list.Shuffle();
    }

    public List<string> SolveLetters(char[] letters)
    {
        Debug.Assert(letters is not null && (letters.Length == cMaxLetters) && letters.All(c => char.IsLower(c)));

        loadingEvent.Wait();  // until finished loading resources

        List<string> results = new List<string>();

        for (int k = cMaxLetters; k >= cMinLetters; --k)
        {
            foreach (char[] chars in new Combinations<char>(letters, k))
            {
                string key = new string(chars);

                AddDictionaryWordsToList(key, otherWords, results);

                if (k == cMaxLetters)
                {
                    AddDictionaryWordsToList(key, conundrumWords, results);
                }
            }
        }

        return results;
    }

    private static void AddDictionaryWordsToList(string key, Dictionary<string, string[]> dictionary, List<string> list)
    {
        if (dictionary.TryGetValue(key, out string[]? data) && (data is not null))
        {
            list.AddRange(data);
        }
    }

    public string SolveConundrum(char[] letters)
    {
        Debug.Assert(letters is not null && (letters.Length == cMaxLetters) && letters.All(c => char.IsLower(c)));

        loadingEvent.Wait();  // until finished loading resources

        Array.Sort(letters);
        string key = new string(letters);

        if (conundrumWords.TryGetValue(key, out string[]? data) && (data is not null))
        {
            return data[0];
        }

        return string.Empty;
    }

    public IList<char> GenerateConundrum()
    {
        loadingEvent.Wait();  // until finished loading resources

        Debug.Assert(conundrumWords.Count > 0);

        if (conundrumWords.Count > 0)
        {
            // move a random distance into dictionary
            int index = new Random().Next(conundrumWords.Count);

            IEnumerator<string[]> e = conundrumWords.Values.GetEnumerator();

            while (e.MoveNext() && (index-- > 0)) { }; // empty statement

            return e.Current[0].ToCharArray().Shuffle();
        }

        return new string(' ', cMaxLetters).ToCharArray();
    }

    public bool HasConundrums
    {
        get
        {
            loadingEvent.Wait();  // until finished loading resources
            return conundrumWords.Count > 0;
        }
    }

    private void LoadResourceFile()
    {
        Stream? resourceStream = typeof(App).Assembly.GetManifestResourceStream(cResourceName);

        if (resourceStream is not null)
        {
            using (DeflateStream stream = new DeflateStream(resourceStream, CompressionMode.Decompress))
            {
                using (StreamReader sr = new StreamReader(stream, Encoding.ASCII))
                {
                    string? line;

                    while ((line = sr.ReadLine()) != null)
                    {
                        string[] words = line.Split();

                        // make key
                        char[] c = words[0].ToCharArray();
                        Array.Sort(c);
                        string key = new string(c);

                        // add to dictionary
                        if ((c.Length == cMaxLetters) && (words.Length == 1))
                        {
                            conundrumWords[key] = words;
                        }
                        else
                        {
                            otherWords[key] = words;
                        }
                    }
                }
            }
        }
    }
}
