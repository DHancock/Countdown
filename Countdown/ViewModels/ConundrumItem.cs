namespace Countdown.ViewModels;

internal sealed record ConundrumItem(string Conundrum, string Solution)
{
    public override string ToString()
    {
        return $"{Solution}\t{Conundrum}";
    }
}