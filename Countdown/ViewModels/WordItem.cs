namespace Countdown.ViewModels;

/// <summary>
/// A list item used in the letters result ui list
/// </summary>
internal sealed class WordItem : ItemBase, IComparable<WordItem>
{
    public WordItem(string item) : base(item)
    {
    }

    public int CompareTo(WordItem? other)
    {
        Debug.Assert(other is not null);

        // shorter words first, then alphabetical
        int lengthCompare = Content.Length - other.Content.Length;
        return lengthCompare == 0 ? string.CompareOrdinal(Content, other.Content) : lengthCompare;
    }
}
