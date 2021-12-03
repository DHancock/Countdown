namespace Countdown.Models;

internal sealed class VowelList : LetterList
{
    public VowelList() : base(new List<LetterTile>(5)
    {
        new LetterTile('A', 2),
        new LetterTile('E', 3),
        new LetterTile('I', 6),
        new LetterTile('O', 2),
        new LetterTile('U', 3)    
    })
    {
    }
}