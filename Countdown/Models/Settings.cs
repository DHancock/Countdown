namespace Countdown.Models;

internal static class Settings
{
    private static bool sImplemented = true;

    // fallback store for values if ApplicationData isn't implemented
    private static int sChooseNumbersIndex = 1;
    private static int sChooseLettersIndex = 1;
    private static ElementTheme sCurrentTheme = ElementTheme.Default;


    public static int ChooseNumbersIndex
    {
        get => GetValue(sChooseNumbersIndex);
        set => SetValue(value, ref sChooseNumbersIndex);    
    }

    public static int ChooseLettersIndex
    {
        get => GetValue(sChooseLettersIndex);
        set => SetValue(value, ref sChooseLettersIndex);
    }

    public static ElementTheme CurrentTheme
    {
        get => GetValue(sCurrentTheme);
        set => SetValue(value, ref sCurrentTheme);
    }



    private static T GetValue<T>(T fallbackValue, [CallerMemberName] string key = "")
    {
        if (sImplemented)
        {
            try
            {
                if (ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out object? value))
                    return (T)value;
            }
            catch (InvalidOperationException) // it hasn't been implemented in WindowsAppSDK 1.0.0
            {
                sImplemented = false;
            }
        }

        return fallbackValue;
    }


    private static void SetValue<T>(T value, ref T fallbackStore, [CallerMemberName] string key = "")
    {
        if (sImplemented)
        {
            try
            {
                ApplicationData.Current.LocalSettings.Values[key] = value;
            }
            catch (InvalidOperationException) // it hasn't been implemented in WindowsAppSDK 1.0.0
            {
                sImplemented = false;
            }
        }

        if (!sImplemented)
            fallbackStore = value;
    }




    public static ConsonantList Consonants
    {
        get
        {
            List<LetterTile> defaultConsonants = new List<LetterTile>(ConsonantList.cConsonantCount)
            {
                new LetterTile('B', 2),
                new LetterTile('C', 3),
                new LetterTile('D', 6),
                new LetterTile('F', 2),
                new LetterTile('G', 3),
                new LetterTile('H', 2),
                new LetterTile('J', 1),
                new LetterTile('K', 1),
                new LetterTile('L', 5),
                new LetterTile('M', 4),
                new LetterTile('N', 8),
                new LetterTile('P', 4),
                new LetterTile('Q', 1),
                new LetterTile('R', 9),
                new LetterTile('S', 9),
                new LetterTile('T', 9),
                new LetterTile('V', 1),
                new LetterTile('W', 1),
                new LetterTile('X', 1),
                new LetterTile('Y', 1),
                new LetterTile('Z', 1)
            };

            return new ConsonantList(defaultConsonants);
        }
    }



    public static VowelList Vowels
    {
        get
        {
            List<LetterTile> defaultVowels = new List<LetterTile>(VowelList.cVowelCount)
            {
                new LetterTile('A', 2),
                new LetterTile('E', 3),
                new LetterTile('I', 6),
                new LetterTile('O', 2),
                new LetterTile('U', 3)
            };

            return new VowelList(defaultVowels);
        }
    }
}
