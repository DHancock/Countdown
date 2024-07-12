namespace Countdown.Models;

/// <summary>
/// The PostfixMap class is a map that is used to build postfix equations. There are
/// six map entries one for each combination of tiles i.e 1 of 6, 2 of 6, 3 of 6 etc. 
/// Post fix equations are built up of a repeating sequence of pushing digits 
/// on to a stack followed by executing zero or more operators. The diagram below
/// shows the possible post fix equations for 6 tiles:
///
///            12.3..4...5....6......
///
/// The digits represent tiles and the dots represent possible operator positions. All 
/// equations will start by pushing 2 digits followed by executing 0 or 1 operator, 
/// then another digit followed by 0 to 2 operators etc. There will always be one less 
/// operator than digits and the final map entry will always be an operator. 
/// 
/// Consider the case for 4 digits, there are 5 map sub entries one for each possible 
/// postfix equation:
/// 
///         Equation        Operator Counts     Map Entries
///                        
///         12.3..4ooo      =>  0 0 3           =>  4 0 0 0        => push 4 digits, execute 3 operators 
///         12.3.o4oo.	    =>  0 1 2           =>  3 0 1 0 0
///         12o3..4oo.	    =>  1 0 2           =>  2 0 2 0 0 
///         12.3oo4o..	    =>  0 2 1           =>  3 0 0 1 0
///         12o3.o4o..	    =>  1 1 1           =>  2 0 1 0 1 0 
///         
///  Here the "o" represent actual operator positions with in the post fix equation. The code 
///  that generates the map first counts all the variations of operators in the possible operator 
///  locations. It then converts these counts into a map entries where numbers greater than zero 
///  means push that number of digits onto the stack and a zero indicates that operators should 
///  be executed. Each map entry always starts by pushing at least two digits onto the stack.
/// </summary>
internal sealed partial class PostfixMap
{
    public static readonly PostfixMap Instance = new PostfixMap();

    private readonly List<List<int[]>> map;    // the map is auto generated

    /// <summary>
    /// the map entry at index 0 is for 2 tiles, the minimum number of tiles
    /// </summary>
    /// <param name="tileCount">the number of tiles in the equation</param>
    /// <returns></returns>
    public List<int[]> this[int tileCount] => map[tileCount - 2];
}
