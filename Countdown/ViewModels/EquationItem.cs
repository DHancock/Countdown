using System;

namespace Countdown.ViewModels
{
    /// <summary>
    /// Used to display solver results in the ui and tracks
    /// the selection of the items
    /// </summary>
    internal sealed class EquationItem : ItemBase, IComparable
    {
        public EquationItem() : this(string.Empty)
        {
        }

        public EquationItem(string equation) : base(equation)
        {
        }

        public int CompareTo(object obj)
        {
            string other = ((EquationItem)obj).Content;

            int lengthCompare = Content.Length - other.Length;
            return lengthCompare == 0 ? string.Compare(Content, other) : lengthCompare;
        }
    }
}
