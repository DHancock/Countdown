namespace Countdown.ViewModels
{
    /// <summary>
    /// Used to display solver results in the ui and tracks
    /// the selection of the items
    /// </summary>
    internal sealed class EquationItem : ItemBase
    {
        public EquationItem() : this(string.Empty)
        {
        }

        public EquationItem(string equation) : base(equation)
        {
        }
    }
}
