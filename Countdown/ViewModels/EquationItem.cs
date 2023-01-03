namespace Countdown.ViewModels;

/// <summary>
/// A list item used in the numbers result ui list
/// </summary>
internal sealed class EquationItem : ItemBase, IComparable<EquationItem>
{
    public EquationItem(string item) : base(item)
    {
    }

    public int CompareTo(EquationItem? other)
    {
        Debug.Assert(other is not null);

        // shorter strings first, then reverse alphabetical (numbers before parenthesis)
        int lengthCompare = Content.Length - other.Content.Length;
        return lengthCompare == 0 ? string.CompareOrdinal(Content, other.Content) : lengthCompare;
    }
}
