namespace Countdown.ViewModels
{
    abstract internal class ItemBase : PropertyChangedBase
    {
        public string Content { get; protected set; }
        private bool isSelected = false;


        public ItemBase() : this(null)
        {
        }


        public ItemBase(string item)
        {
            Content = item ?? string.Empty;
        }


        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                if (isSelected != value)
                {
                    isSelected = value;
                    RaisePropertyChanged(nameof(IsSelected));
                }
            }
        }


        public override string ToString() => Content;
    }
}
