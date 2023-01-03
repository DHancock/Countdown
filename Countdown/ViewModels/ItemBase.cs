namespace Countdown.ViewModels;

internal abstract class ItemBase
{
    public string Content { get; }

    public ItemBase(string item)
    {
        Content = item;
    }

    public override string ToString() => Content;
}
