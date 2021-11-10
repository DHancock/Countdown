namespace Countdown.Models
{
    internal sealed class ConsonantList : LetterList
    {
        public const int cConsonantCount = 21;

        public ConsonantList(IList<LetterTile> source) : base(source)
        {
            Debug.Assert(Count == cConsonantCount);
            Debug.Assert(this.All(c => LetterTile.IsUpperConsonant(c.Letter)));
        }

        public void ResetTo(ConsonantList toThis) => base.ResetTo(toThis);
    }
}
