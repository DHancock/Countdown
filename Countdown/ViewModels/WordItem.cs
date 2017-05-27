using System;

namespace Countdown.ViewModels
{
    internal sealed class WordItem : ItemBase, IComparable<WordItem>
    {     
        private bool isExpanded = false;

        
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
                    RaisePropertyChanged(nameof(IsExpanded));
                }
            }
        }

        
        public int CompareTo(WordItem other)
        {
            if (other is null)
                return -1;

            if (ReferenceEquals(this, other))
                return 0;
            
            if (Content is null)
                return (other.Content is null) ? 0 : 1;

            if (other.Content is null)
                return -1;

            // sort on word length, longer first 
            if (Content.Length != other.Content.Length)
                return (Content.Length < other.Content.Length) ? 1 : -1;

            // standard alphabetical sort
            return Content.CompareTo(other.Content);
        }
    }
}
