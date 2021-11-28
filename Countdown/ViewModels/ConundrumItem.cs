namespace Countdown.ViewModels;

/// <summary>
/// A list item used in the conundrum result ui list
/// Unlike the other lists it has two columns so separate property
/// getters are required for the solution and conundrum words
/// </summary>
internal sealed class ConundrumItem : ItemBase
{
    public string Solution { get; }

    public ConundrumItem(string conundrum, string solution) : base(conundrum)
    {
        Solution = solution;
    }

    public string Conundrum => Content;

    public override string ToString() => $"{Conundrum}\t{Solution}";
}
