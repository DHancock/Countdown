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
                List<LetterTile> defaultConsonants = new List<LetterTile>(ConsonantList.cConsonantCount);

                defaultConsonants.Add(new LetterTile('B', 2));
                defaultConsonants.Add(new LetterTile('C', 3));
                defaultConsonants.Add(new LetterTile('D', 6));
                defaultConsonants.Add(new LetterTile('F', 2));
                defaultConsonants.Add(new LetterTile('G', 3));
                defaultConsonants.Add(new LetterTile('H', 2));
                defaultConsonants.Add(new LetterTile('J', 1));
                defaultConsonants.Add(new LetterTile('K', 1));
                defaultConsonants.Add(new LetterTile('L', 5));
                defaultConsonants.Add(new LetterTile('M', 4));
                defaultConsonants.Add(new LetterTile('N', 8));
                defaultConsonants.Add(new LetterTile('P', 4));
                defaultConsonants.Add(new LetterTile('Q', 1));
                defaultConsonants.Add(new LetterTile('R', 9));
                defaultConsonants.Add(new LetterTile('S', 9));
                defaultConsonants.Add(new LetterTile('T', 9));
                defaultConsonants.Add(new LetterTile('V', 1));
                defaultConsonants.Add(new LetterTile('W', 1));
                defaultConsonants.Add(new LetterTile('X', 1));
                defaultConsonants.Add(new LetterTile('Y', 1));
                defaultConsonants.Add(new LetterTile('Z', 1));

                return new ConsonantList(defaultConsonants);
            }
        }



        public static VowelList DefaultVowels
        {
            get
            {
                List<LetterTile> defaultVowels = new List<LetterTile>(VowelList.cVowelCount);

                defaultVowels.Add(new LetterTile('A', 2));
                defaultVowels.Add(new LetterTile('E', 3));
                defaultVowels.Add(new LetterTile('I', 6));
                defaultVowels.Add(new LetterTile('O', 2));
                defaultVowels.Add(new LetterTile('U', 3));

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
