using System.Diagnostics;

namespace Countdown.Models
{
    internal sealed class VowelList : LetterList
    {
        public const int cVowelCount = 5;

        public VowelList(IList<LetterTile> source) : base(source)
        {
            Debug.Assert(Count == cVowelCount);
            Debug.Assert(this.All(c => LetterTile.IsUpperVowel(c.Letter)));
        }

        public void ResetTo(VowelList toThis) => base.ResetTo(toThis);
    }
}
