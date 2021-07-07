namespace Countdown.ViewModels
{
    internal abstract class ItemBase
    {
        public string Content { get; }
        public bool IsSelected { get; set; }

        public ItemBase(string item)
        {
            Content = item ?? string.Empty;
        }

        public override string ToString() => Content;
    }
}
