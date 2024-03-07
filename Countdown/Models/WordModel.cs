using Countdown.Utils;

namespace Countdown.Models;

internal class WordModel
{
    private const string cResourceName = "Countdown.Resources.wordlist.dat";
    private const byte cLine_seperator = 46; // full stop
    private const byte cWord_seperator = 32; // space 

    public const int cMinLetters = 4;
    public const int cMaxLetters = 9;
    public const int cLetterCount = cMaxLetters;

    // conundrum words are 9 letters long with only one solution
    private readonly Dictionary<string, byte[]> conundrumWords = new Dictionary<string, byte[]>();
    private readonly Dictionary<string, byte[]> otherWords = new Dictionary<string, byte[]>();

    private readonly ConsonantList consonantList = new ConsonantList();
    private readonly VowelList vowelList = new VowelList();

    private readonly ManualResetEventSlim loadingEvent = new ManualResetEventSlim();

    public WordModel()
    {
        Task.Factory.StartNew(() => LoadResourceFile()).ContinueWith((x) => loadingEvent.Set());
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

        for (int k = letters.Length; k >= cMinLetters; --k)
        {
            foreach (char[] chars in new Combinations<char>(letters, k))
            {
                string key = new string(chars);

                AddDictionaryWordsToList(key, otherWords, results);

                if (k == letters.Length)
                {
                    AddDictionaryWordsToList(key, conundrumWords, results);
                }
            }
        }

        return results;
    }

    private static void AddDictionaryWordsToList(string key, Dictionary<string, byte[]> dictionary, List<string> list)
    {
        if (dictionary.TryGetValue(key, out byte[]? data) && (data is not null))
        {
            string line = new string(GetChars(data, data.Length));

            if (line.Length == key.Length)
            {
                list.Add(line);
            }
            else
            {
                list.AddRange(line.Split((char)cWord_seperator));
            }
        }
    }

    public string SolveConundrum(char[] letters)
    {
        Debug.Assert(letters is not null && (letters.Length == cMaxLetters) && letters.All(c => char.IsLower(c)));

        loadingEvent.Wait();  // until finished loading resources

        Array.Sort(letters);
        string key = new string(letters);

        if (conundrumWords.TryGetValue(key, out byte[]? data) && (data is not null))
        {
            return new string(GetChars(data, data.Length, true));
        }

        return string.Empty;
    }

    public IList<char> GenerateConundrum()
    {
        loadingEvent.Wait();  // until finished loading resources

        if (conundrumWords.Count > 0)
        {
            // move a random distance into dictionary
            int index = new Random().Next(conundrumWords.Count);

            IEnumerator<byte[]> e = conundrumWords.Values.GetEnumerator();

            while (e.MoveNext() && (index-- > 0)) { }; // empty statement

            return GetChars(e.Current, e.Current.Length, true).Shuffle();
        }

        return Array.Empty<char>();
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
            using DeflateStream stream = new DeflateStream(resourceStream, CompressionMode.Decompress);

            DeflateStreamReader streamReader = new DeflateStreamReader(stream);
            ReadOnlySpan<byte> line;

            while ((line = streamReader.ReadLine()).Length > 0)
            {
                int keyLength = line.Length;

                // check for a word break within the line
                if (keyLength > (cMinLetters * 2))
                {
                    int pos = line.Slice(cMinLetters).IndexOf(cWord_seperator);

                    if (pos >= 0)
                    {
                        keyLength = pos + cMinLetters;
                    }
                }

                // make key
                char[] c = GetChars(line, keyLength);
                Array.Sort(c);
                string key = new string(c);

                // add to dictionary
                if ((keyLength == cMaxLetters) && (keyLength == line.Length))
                {
                    conundrumWords[key] = line.ToArray();
                }
                else
                {
                    otherWords[key] = line.ToArray();
                }
            }
        }
    }

    // a simple encoder, the source is known quantity
    private static char[] GetChars(ReadOnlySpan<byte> bytes, int count, bool toUpper = false)
    {
        if ((count < 0) || (count > bytes.Length))
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        char[] chars = new char[count];
        int index = 0;

        if (toUpper)
        {
            while (index < count)
            {
                chars[index] = (char)(bytes[index++] & ~0x20);
            }
        }
        else
        {
            while (index < count)
            {
                chars[index] = (char)bytes[index++];
            }
        }

        return chars;
    }

    /// <summary>
    /// Hides the complexity of reading lines from the stream.
    /// Assumes that the maximum line length is going to be 
    /// less than or equal to the buffer size.
    /// </summary>
    private sealed class DeflateStreamReader
    {
        private const int cBufferSize = 1024 * 8;

        private readonly DeflateStream stream;
        private readonly byte[] buffer = new byte[cBufferSize];

        private int dataSize = 0;
        private int position = 0;
        private bool endOfStream = false;

        public DeflateStreamReader(DeflateStream s)
        {
            stream = s;
        }

        public ReadOnlySpan<byte> ReadLine()
        {
            int length = SeekNextLine();

            if (length > 0) // simple case, the line is within the buffer
            {
                ReadOnlySpan<byte> line = buffer.AsSpan(position, length);
                position += length + 1;
                return line;
            }
            else if (!endOfStream) // some of the line remains in the stream
            {
                int sizeLeft = dataSize - position;

                if (sizeLeft > 0)  // move partial line to the start of the buffer
                {
                    Buffer.BlockCopy(buffer, position, buffer, 0, sizeLeft);
                }

                // refill buffer
                dataSize = sizeLeft;
                Span<byte> span = buffer.AsSpan();

                while (dataSize < buffer.Length)
                {
                    int bytesRead = stream.Read(span.Slice(dataSize));

                    if (bytesRead == 0)
                    {
                        endOfStream = true;
                        break;
                    }

                    dataSize += bytesRead;
                }

                position = sizeLeft;

                length = SeekNextLine();

                Debug.Assert(length >= 0); // its not the end of the stream, it shouldn't be

                position += length + 1;
                return span.Slice(0, sizeLeft + length);
            }

            return [];
        }

        private int SeekNextLine() => buffer.AsSpan(position, dataSize - position).IndexOf(cLine_seperator);
    }
}
