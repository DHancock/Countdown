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
            return Content.Length == other.Length ? string.Compare(Content, other) : Content.Length - other.Length;
        }
    }
}
