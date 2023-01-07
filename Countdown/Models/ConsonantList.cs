using Countdown.Utils;

namespace Countdown.Models;

internal sealed class ConsonantList : LetterList
{
    public ConsonantList()
    {
        AddRange('A', 2);
        AddRange('B', 2);
        AddRange('C', 3);
        AddRange('D', 6);
        AddRange('F', 2);
        AddRange('G', 3);
        AddRange('H', 2);
        AddRange('J', 1);
        AddRange('K', 1);
        AddRange('L', 5);
        AddRange('M', 4);
        AddRange('N', 8);
        AddRange('P', 4);
        AddRange('Q', 1);
        AddRange('R', 9);
        AddRange('S', 9);
        AddRange('T', 9);
        AddRange('V', 1);
        AddRange('W', 1);
        AddRange('X', 1);
        AddRange('Y', 1);
        AddRange('Z', 1);

        chars.Shuffle().ReduceDuplicateSequences();
    }
}
