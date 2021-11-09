using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Windows.Storage;

namespace Countdown.Models
{
    internal class Settings
    {



        public static int ChooseNumbersIndex
        {
            get
            {
                if (ApplicationData.Current.LocalSettings.Values.TryGetValue(nameof(ChooseNumbersIndex), out object? value))
                    return (int)(value ?? 0);

                return 0;
            }
            set
            {
                ApplicationData.Current.LocalSettings.Values[nameof(ChooseNumbersIndex)] = value;
            }
        }

        public static int ChooseLettersIndex
        {
            get
            {
                if (ApplicationData.Current.LocalSettings.Values.TryGetValue(nameof(ChooseLettersIndex), out object? value))
                    return (int)(value ?? 0);

                return 0;
            }
            set
            {
                ApplicationData.Current.LocalSettings.Values[nameof(ChooseLettersIndex)] = value;
            }
        }



        public static ConsonantList DefaultConsonants
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



        public static VowelList DefaultVowels
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





        public static VowelList Vowels
        {
            get
            {
                if (ApplicationData.Current.LocalSettings.Values.TryGetValue(nameof(Vowels), out object? value) && (value is VowelList list))
                    return list;

                return DefaultVowels;
            }

            set
            {
                ApplicationData.Current.LocalSettings.Values[nameof(Vowels)] = value;
            }
        }

        public static ConsonantList Consonants
        {
            get
            {
                if (ApplicationData.Current.LocalSettings.Values.TryGetValue(nameof(Consonants), out object? value) && (value is ConsonantList list))
                    return list;

                return DefaultConsonants;
            }

            set
            {
                ApplicationData.Current.LocalSettings.Values[nameof(Consonants)] = value;
            }
        }


        public static ElementTheme CurrentTheme
        {
            get
            {
                if (ApplicationData.Current.LocalSettings.Values.TryGetValue(nameof(CurrentTheme), out object? value) && (value is string theme))
                    return Enum.Parse<ElementTheme>(theme);

                if (App.MainWindow?.Content is FrameworkElement fe)
                    return fe.RequestedTheme;

                return Application.Current.RequestedTheme == ApplicationTheme.Light ? ElementTheme.Light : ElementTheme.Dark;
            }

            set
            {
                Debug.Assert(Enum.IsDefined(typeof(ElementTheme), value));
                ApplicationData.Current.LocalSettings.Values[nameof(CurrentTheme)] = value.ToString();
            }
        }
    }
}
