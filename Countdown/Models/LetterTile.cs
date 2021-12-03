namespace Countdown.Models;

internal sealed class LetterTile
{
    public int Frequency { get; }
    public char Letter { get; }

    public LetterTile(char letter, int frequency)
    {
        if (!IsUpperLetter(letter))
            throw new ArgumentOutOfRangeException(nameof(letter));

        Frequency = frequency;
        Letter = letter;
    }

    public static bool IsUpperVowel(char c) => c is 'E' or 'A' or 'I' or 'O' or 'U';
    
    public static bool IsUpperConsonant(char c) => IsUpperLetter(c) && !IsUpperVowel(c);
    
    public static bool IsUpperLetter(char c) => c is >= 'A' and <= 'Z';   
}
