using System;
using System.Configuration;
using System.Reflection;

namespace Countdown.Models
{
    internal static class UserSettings
    {
        private static LetterList defaultConsonants;
        private static LetterList defaultVowels;



        public static void UpgradeIfRequired()
        {
            if (Settings.Default.UpdateRequired)
            {
                Settings.Default.Upgrade();
                Settings.Default.UpdateRequired = false;
            }
        }




        public static LetterList Consonants
        {
            get
            {
                if (Settings.Default.ConsonantFrequency is null) 
                {
                    if (DefaultConsonants is null)
                        throw new InvalidOperationException(nameof(Consonants));

                    Settings.Default.ConsonantFrequency = DefaultConsonants;
                }

                return Settings.Default.ConsonantFrequency;
            }
        }

        

        public static LetterList DefaultConsonants
        {
            get
            {
                if (defaultConsonants is null)
                    defaultConsonants = GetLetterListFromAttribute(nameof(Settings.Default.ConsonantFrequency));

                return defaultConsonants;
            }
        }

   

        public static LetterList DefaultVowels
        {
            get
            {
                if (defaultVowels is null)
                    defaultVowels = GetLetterListFromAttribute(nameof(Settings.Default.VowelFrequency));

                return defaultVowels;
            }
        }

        

        public static LetterList Vowels
        {
            get
            {
                if (Settings.Default.VowelFrequency is null)
                {
                    if (DefaultVowels is null)
                        throw new InvalidOperationException(nameof(Vowels));

                    Settings.Default.VowelFrequency = DefaultVowels;
                }

                return Settings.Default.VowelFrequency;
            }
        }


  

        private static LetterList GetLetterListFromAttribute(string propertyName)
        {
            string defaultValue = GetAttributeValue(propertyName);

            if (!string.IsNullOrEmpty(defaultValue))
                return new LetterListConverter().ConvertFrom(defaultValue) as LetterList;

            return null;
        }



        private static string GetAttributeValue(string propertyName)
        {
            MemberInfo[] propertyInfo = Settings.Default.GetType().GetProperties();

            foreach (MemberInfo mi in propertyInfo)
            {
                if (mi.Name == propertyName)
                {
                    if (Attribute.GetCustomAttribute(mi, typeof(DefaultSettingValueAttribute)) is DefaultSettingValueAttribute dsva)
                        return dsva.Value;

                    return null;
                }
            }

            return null;
        }
    }
}
