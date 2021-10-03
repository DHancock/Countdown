using System;

using Countdown.ViewModels;

namespace Countdown.Models
{
    internal sealed class LetterTile : PropertyChangedBase, IComparable<LetterTile>
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


        public static bool IsUpperVowel(char c)
        {
            // ordered by frequency
            return c is 'E' or 'A' or 'I' or 'O' or 'U';
        }


        public static bool IsUpperConsonant(char c)
        {
            return IsUpperLetter(c) && !IsUpperVowel(c);
        }


        // no diacritics, basic Latin only
        public static bool IsUpperLetter(char c)
        {
            return c is >= 'A' and <= 'Z';
        }


        public override int GetHashCode() => base.GetHashCode();


        public override bool Equals(object? obj)
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
        public int CompareTo(LetterTile? other)
        {
            return other is null ? -1 : Letter.CompareTo(other.Letter);
        }


        public override string ToString() => $"{Letter}: {Frequency}";
    }
}
