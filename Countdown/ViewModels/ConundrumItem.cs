namespace Countdown.ViewModels
{
    internal sealed class ConundrumItem : ItemBase
    {
        public string Solution { get; }

        public ConundrumItem(string conundrum, string solution) : base(conundrum)
        {
            Solution = solution ?? string.Empty;  
        }

        public string Conundrum => Content;

        public override string ToString() => $"{Conundrum}\t{Solution}";
    }
}
