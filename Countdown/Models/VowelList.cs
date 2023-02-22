using Countdown.Utils;

namespace Countdown.Models;

internal sealed class VowelList : LetterList
{
    public VowelList()
    {
        AddRange('A', 2);
        AddRange('E', 3);
        AddRange('I', 6);
        AddRange('O', 2);
        AddRange('U', 3);

        chars.Shuffle().ReduceDuplicateSequences();
    }
}
