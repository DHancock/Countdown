namespace Countdown.Models;

internal sealed class ConsonantList : LetterList
{
    public ConsonantList() : base(new List<LetterTile>(21)
    {
        new LetterTile('B', 2),
        new LetterTile('C', 3),
        new LetterTile('D', 6),
        new LetterTile('F', 2),
        new LetterTile('G', 3),
        new LetterTile('H', 2),
        new LetterTile('J', 1),
        new LetterTile('K', 1),
        new LetterTile('L', 5),
        new LetterTile('M', 4),
        new LetterTile('N', 8),
        new LetterTile('P', 4),
        new LetterTile('Q', 1),
        new LetterTile('R', 9),
        new LetterTile('S', 9),
        new LetterTile('T', 9),
        new LetterTile('V', 1),
        new LetterTile('W', 1),
        new LetterTile('X', 1),
        new LetterTile('Y', 1),
        new LetterTile('Z', 1)
    })
    {
    }
}