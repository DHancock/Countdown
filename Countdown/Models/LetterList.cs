namespace Countdown.Models;

internal abstract class LetterList
{
    protected readonly List<char> chars = new List<char>();
    private int index = 0;

    protected void AddRange(char c, int count)
    {
        for (int i = 0; i < count; i++)
        {
            chars.Add(c);
        }
    }

    public char GetLetter() => chars[index++ % chars.Count];
}
