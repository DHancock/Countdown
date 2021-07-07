using System;

namespace Countdown.ViewModels
{
    /// <summary>
    /// A list item used in the numbers result ui list
    /// </summary>
    internal sealed class EquationItem : ItemBase, IComparable<EquationItem>
    {
        public EquationItem() : base(string.Empty)
        {
        }

        public EquationItem(string item) : base(item)
        {
        }

        public int CompareTo(EquationItem other)
        {
            int lengthCompare = Content.Length - other.Content.Length;
            return lengthCompare == 0 ? string.Compare(other.Content, Content, StringComparison.Ordinal) : lengthCompare;
        }
    }
}
