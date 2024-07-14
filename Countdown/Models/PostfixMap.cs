namespace Countdown.Models;

 /// <summary>
 /// The map defines how postfix equations are constructed for the output of the permutations of tiles.
 /// There are separate entries for each permutation count, that's 2 from 6, 3 from 6, 4 from 6 etc.
 /// Each entry defines every possible equation for that number of tiles. Positive integers > 0 indicate 
 /// tiles should be pushed on to the stack. Zeros indicate that an operator should be executed. 
 /// </summary>

internal sealed partial class PostfixMap
{
    public static readonly PostfixMap Instance = new PostfixMap();

    private readonly List<List<int[]>> map;    // the map is auto generated

    /// <summary>
    /// Indexer access. The minimum number of tiles is 2, at index 0
    /// </summary>
    /// <param name="tileCount">the number of tiles in the equation</param>
    /// <returns></returns>
    public List<int[]> this[int tileCount] => map[tileCount - 2];
}
