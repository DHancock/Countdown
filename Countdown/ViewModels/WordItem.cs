using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Countdown.ViewModels
{
    /// <summary>
    /// A list item used in the letters result ui list
    /// </summary>
    internal sealed class WordItem : ItemBase, INotifyPropertyChanged, IComparable<WordItem>
    {
        private bool isExpanded;
        public event PropertyChangedEventHandler PropertyChanged;

        public WordItem(string item) : base(item)
        {
        }

        public bool IsExpanded
        {
            get { return isExpanded; }
            set
            {
                if (isExpanded != value)
                {
                    isExpanded = value;
                    RaisePropertyChanged();
                }
            }
        }

        public int CompareTo(WordItem other)
        {
            int lengthCompare = other.Content.Length - Content.Length;
            return lengthCompare == 0 ? string.Compare(Content, other.Content, StringComparison.Ordinal) : lengthCompare;
        }

        private void RaisePropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
