using Countdown.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Text;


namespace Countdown.Models
{
    [TypeConverter(typeof(LetterListConverter))]
    [SettingsSerializeAs(SettingsSerializeAs.String)]
    internal sealed class LetterList : ReadOnlyCollection<LetterTile>, IDeepCloneable<LetterList>
    {
        public const int vowel_count = 5;
        public const int consonant_count = 21;

        /// <summary>
        /// Decent error checking, the settings file can be edited by the user
        /// </summary>
        /// <param name="list"></param>
        public LetterList(List<LetterTile> list) : base(list)
        {
            if (list is null)
                throw new ArgumentNullException(nameof(list));

            if ((list.Count < vowel_count) || (list[0] is null))
                throw new ArgumentOutOfRangeException(nameof(list));

            bool containsVowels = LetterTile.IsUpperVowel(list[0].Letter);

            if (containsVowels)
            {
                if (list.Count != vowel_count)
                    throw new ArgumentOutOfRangeException(nameof(list));
            }
            else if (list.Count != consonant_count)
                throw new ArgumentOutOfRangeException(nameof(list));

            // sort alphabetically on letter
            list.Sort();
            char previousLetter = list[0].Letter;

            for (int index = 1; index < list.Count; ++index)
            {
                if (list[index] is null)
                    throw new ArgumentOutOfRangeException(nameof(list));

                if (previousLetter == list[index].Letter) // duplicate check
                    throw new ArgumentOutOfRangeException(nameof(list));

                if (containsVowels != LetterTile.IsUpperVowel(list[index].Letter))
                    throw new ArgumentOutOfRangeException(nameof(list));

                previousLetter = list[index].Letter;
            }
        }


        public LetterList(LetterList other) : base(new List<LetterTile>())
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            foreach (LetterTile tile in other.Items)
                Items.Add(tile.DeepClone());
        }



        public char GetLetter(Random random)
        {
            if (random is null)
                throw new ArgumentNullException(nameof(random));

            // find tile with frequency match
            int probability = random.Next(this.Sum(lt => lt.Frequency));
            int sum = 0;

            foreach (LetterTile tile in this)
            {
                sum += tile.Frequency;

                if (probability < sum)
                    return tile.Letter;
            }
            
            return '\0';
        }

       
        public LetterList DeepClone() => new LetterList(this);


        public void ResetTo(LetterList toThis)
        {
            if (toThis is null)
                throw new ArgumentNullException(nameof(toThis));

            if (Count != toThis.Count)
                throw new ArgumentOutOfRangeException(nameof(toThis));

            for (int index = 0; index < Count; index++)
                this[index].Frequency = toThis[index].Frequency;
        }


        public override int GetHashCode() => base.GetHashCode();
        
        
        public override bool Equals(object obj)
        {
            if (obj != null)
            {
                if (ReferenceEquals(this, obj))
                    return true;

                if (obj is LetterList other)
                {
                    if (Count != other.Count)
                        return false;

                    for (int index = 0; index < Count; index++)
                    {
                        if (this[index] != other[index])
                            return false;
                    }

                    return true;
                }
            }

            return false;
        }

 
        public static bool operator ==(LetterList a, LetterList b)
        {
            return (a is null) ? (b is null) : a.Equals(b);
        }


        public static bool operator !=(LetterList a, LetterList b)
        {
            return !(a == b);
        }
    }



      
    internal sealed class LetterTile : PropertyChangedBase, IComparable<LetterTile>, IDeepCloneable<LetterTile>
    {
        public const int cMaxFrequency = 99;
        public const int cMinFrequency = 1;

        private int frequency;
        public char Letter { get; }

        public LetterTile(char letter, int frequency)
        {
            if (!IsUpperLetter(letter))
                throw new ArgumentOutOfRangeException(nameof(letter));

            if ((frequency < cMinFrequency) || (frequency > cMaxFrequency))
                throw new ArgumentOutOfRangeException(nameof(frequency));

            this.frequency = frequency;
            Letter = letter;
        }


        public LetterTile(LetterTile other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            Frequency = other.Frequency;
            Letter = other.Letter;
        }


        public int Frequency
        {
            get { return frequency; }
            set
            {
                if ((value < cMinFrequency) || (value > cMaxFrequency))
                    throw new ArgumentOutOfRangeException(nameof(value));

                if (frequency != value)
                {
                    frequency = value;
                    RaisePropertyChanged(nameof(Frequency));
                }
            }
        }

     
        public LetterTile DeepClone() => new LetterTile(this);


        public static bool IsUpperVowel(char c)
        {
            // ordered by frequency
            return (c is 'E') || (c is 'A') || (c is 'I') || (c is 'O') || (c is 'U');
        }


        public static bool IsUpperConsonant(char c)
        {
            return IsUpperLetter(c) && !IsUpperVowel(c);
        }


        // no diacritics, basic Latin only
        public static bool IsUpperLetter(char c)
        {
            return (c >= 'A') && (c <= 'Z');
        }


        public override int GetHashCode() => base.GetHashCode();


        public override bool Equals(object obj)
        {
            if (obj != null)
            {
                if (ReferenceEquals(this, obj))
                    return true;

                if (obj is LetterTile other)
                    return (Letter == other.Letter) && (Frequency == other.Frequency);
            }

            return false;
        }


        public static bool operator ==(LetterTile a, LetterTile b)
        {
            return (a is null) ? (b is null) : a.Equals(b);
        }


        public static bool operator !=(LetterTile a, LetterTile b)
        {
            return !(a == b);
        }


        /// <summary>
        /// Used for sorting, compares letter only
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(LetterTile other)
        {
            return Letter.CompareTo(other.Letter);
        }


        public override string ToString() => $"{Letter}: {Frequency}";
    }


    internal interface IDeepCloneable<T>
    {
        T DeepClone();
    }


    internal sealed class LetterListConverter : TypeConverter
    {
        private const char entry_seperator = ',';
        private const char field_seperator = ' ';


        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }


        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (value is string data)
            {
                List<LetterTile> list = new List<LetterTile>();

                foreach (string entry in data.Split(new char[] { entry_seperator }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] parts = entry.Split(new char[] { field_seperator }, StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length == 2)
                    {
                        if (int.TryParse(parts[1], out int frequency))
                        {
                            try
                            {
                                list.Add(new LetterTile(parts[0][0], frequency));

                                if (list.Count == LetterList.consonant_count)
                                    break;
                            }
                            catch (ArgumentException)
                            {
                                return null;
                            }
                        }
                        else
                            return null;
                    }
                    else
                        return null;
                }

                try
                {
                    return new LetterList(list);
                }
                catch (ArgumentException)
                {
                }
            }

            return null;
        }
       



        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                if (value is LetterList data)
                {
                    StringBuilder sb = new StringBuilder();

                    foreach (LetterTile letterTile in data)
                        sb.AppendFormat($"{letterTile.Letter}{field_seperator}{letterTile.Frequency}{entry_seperator}");

                    return sb.ToString();
                }
            }

            return string.Empty;
        }
    }
}
