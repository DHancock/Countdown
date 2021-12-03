using Countdown.Utils;

namespace Countdown.Models;

internal abstract class LetterList
{
    private List<char> letters;
    private int index = 0;
    private const int cMinimumStackSize = 100;

    public LetterList(IList<LetterTile> source)
    {
        int frequencyCount = source.Sum(lt => lt.Frequency);
        int copies = (cMinimumStackSize / frequencyCount) + 1;
        letters = new List<char>(copies * frequencyCount);

        foreach (LetterTile letterTile in source)
        {
            for (int i = 0; i < (letterTile.Frequency * copies); i++)
                letters.Add(letterTile.Letter);
        }

        letters.Shuffle();
        letters.ReduceDuplicateSequences();
    }

    public char GetLetter() => letters[index++ % letters.Count];
}
