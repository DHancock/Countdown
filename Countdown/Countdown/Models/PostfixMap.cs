namespace Countdown.Models;

/// <summary>
/// The PostfixMap class creates a map that is used to build postfix equations. There are
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
internal sealed class PostfixMap
{
    /// <summary>
    /// thread safe singleton initialization
    /// </summary>
    public static readonly PostfixMap Instance = new PostfixMap();

    private readonly List<List<int[]>> map;


    private PostfixMap()
    {
        map = new(5);

        map.Add(new(1));    // capacity for 2 tiles
        map.Add(new(2));    // capacity for 3 tiles
        map.Add(new(5));    // capacity for 4 tiles
        map.Add(new(14));   // capacity for 5 tiles
        map.Add(new(42));   // capacity for 6 tiles

        // map entries for 2 tiles
        map[0].Add(new[] { 2, 0 });

        // map entries for 3 tiles
        map[1].Add(new[] { 3, 0, 0 });
        map[1].Add(new[] { 2, 0, 1, 0 });

        // map entries for 4 tiles
        map[2].Add(new[] { 4, 0, 0, 0 });
        map[2].Add(new[] { 3, 0, 1, 0, 0 });
        map[2].Add(new[] { 3, 0, 0, 1, 0 });
        map[2].Add(new[] { 2, 0, 2, 0, 0 });
        map[2].Add(new[] { 2, 0, 1, 0, 1, 0 });

        // map entries for 5 tiles
        map[3].Add(new[] { 5, 0, 0, 0, 0 });
        map[3].Add(new[] { 4, 0, 1, 0, 0, 0 });
        map[3].Add(new[] { 4, 0, 0, 1, 0, 0 });
        map[3].Add(new[] { 4, 0, 0, 0, 1, 0 });
        map[3].Add(new[] { 3, 0, 2, 0, 0, 0 });
        map[3].Add(new[] { 3, 0, 1, 0, 1, 0, 0 });
        map[3].Add(new[] { 3, 0, 1, 0, 0, 1, 0 });
        map[3].Add(new[] { 3, 0, 0, 2, 0, 0 });
        map[3].Add(new[] { 3, 0, 0, 1, 0, 1, 0 });
        map[3].Add(new[] { 2, 0, 3, 0, 0, 0 });
        map[3].Add(new[] { 2, 0, 2, 0, 1, 0, 0 });
        map[3].Add(new[] { 2, 0, 2, 0, 0, 1, 0 });
        map[3].Add(new[] { 2, 0, 1, 0, 2, 0, 0 });
        map[3].Add(new[] { 2, 0, 1, 0, 1, 0, 1, 0 });

        // map entries for 6 tiles
        map[4].Add(new[] { 6, 0, 0, 0, 0, 0 });
        map[4].Add(new[] { 5, 0, 1, 0, 0, 0, 0 });
        map[4].Add(new[] { 5, 0, 0, 1, 0, 0, 0 });
        map[4].Add(new[] { 5, 0, 0, 0, 1, 0, 0 });
        map[4].Add(new[] { 5, 0, 0, 0, 0, 1, 0 });
        map[4].Add(new[] { 4, 0, 2, 0, 0, 0, 0 });
        map[4].Add(new[] { 4, 0, 1, 0, 1, 0, 0, 0 });
        map[4].Add(new[] { 4, 0, 1, 0, 0, 1, 0, 0 });
        map[4].Add(new[] { 4, 0, 1, 0, 0, 0, 1, 0 });
        map[4].Add(new[] { 4, 0, 0, 2, 0, 0, 0 });
        map[4].Add(new[] { 4, 0, 0, 1, 0, 1, 0, 0 });
        map[4].Add(new[] { 4, 0, 0, 1, 0, 0, 1, 0 });
        map[4].Add(new[] { 4, 0, 0, 0, 2, 0, 0 });
        map[4].Add(new[] { 4, 0, 0, 0, 1, 0, 1, 0 });
        map[4].Add(new[] { 3, 0, 3, 0, 0, 0, 0 });
        map[4].Add(new[] { 3, 0, 2, 0, 1, 0, 0, 0 });
        map[4].Add(new[] { 3, 0, 2, 0, 0, 1, 0, 0 });
        map[4].Add(new[] { 3, 0, 2, 0, 0, 0, 1, 0 });
        map[4].Add(new[] { 3, 0, 1, 0, 2, 0, 0, 0 });
        map[4].Add(new[] { 3, 0, 1, 0, 1, 0, 1, 0, 0 });
        map[4].Add(new[] { 3, 0, 1, 0, 1, 0, 0, 1, 0 });
        map[4].Add(new[] { 3, 0, 1, 0, 0, 2, 0, 0 });
        map[4].Add(new[] { 3, 0, 1, 0, 0, 1, 0, 1, 0 });
        map[4].Add(new[] { 3, 0, 0, 3, 0, 0, 0 });
        map[4].Add(new[] { 3, 0, 0, 2, 0, 1, 0, 0 });
        map[4].Add(new[] { 3, 0, 0, 2, 0, 0, 1, 0 });
        map[4].Add(new[] { 3, 0, 0, 1, 0, 2, 0, 0 });
        map[4].Add(new[] { 3, 0, 0, 1, 0, 1, 0, 1, 0 });
        map[4].Add(new[] { 2, 0, 4, 0, 0, 0, 0 });
        map[4].Add(new[] { 2, 0, 3, 0, 1, 0, 0, 0 });
        map[4].Add(new[] { 2, 0, 3, 0, 0, 1, 0, 0 });
        map[4].Add(new[] { 2, 0, 3, 0, 0, 0, 1, 0 });
        map[4].Add(new[] { 2, 0, 2, 0, 2, 0, 0, 0 });
        map[4].Add(new[] { 2, 0, 2, 0, 1, 0, 1, 0, 0 });
        map[4].Add(new[] { 2, 0, 2, 0, 1, 0, 0, 1, 0 });
        map[4].Add(new[] { 2, 0, 2, 0, 0, 2, 0, 0 });
        map[4].Add(new[] { 2, 0, 2, 0, 0, 1, 0, 1, 0 });
        map[4].Add(new[] { 2, 0, 1, 0, 3, 0, 0, 0 });
        map[4].Add(new[] { 2, 0, 1, 0, 2, 0, 1, 0, 0 });
        map[4].Add(new[] { 2, 0, 1, 0, 2, 0, 0, 1, 0 });
        map[4].Add(new[] { 2, 0, 1, 0, 1, 0, 2, 0, 0 });
        map[4].Add(new[] { 2, 0, 1, 0, 1, 0, 1, 0, 1, 0 });

#if false
        GenerateMap();
#endif
    }


    private static void GenerateMap()
    {
        SetClipBoardText(ConvertToText(CreateMap()));
        Debugger.Break();
    }



    private static void SetClipBoardText(string text)
    {
        static void AddText(string text)
        {
            DataPackage dp = new();
            dp.SetText(text);
            Clipboard.SetContent(dp);
        }

        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            AddText(text);
        else
        {
            Thread t = new(() => AddText(text));

            if (t.TrySetApartmentState(ApartmentState.STA))
            {
                t.Start();
                t.Join();
            }
        }
    }



    private static string ConvertToText(List<List<List<int>>> localMap)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"map = new({localMap.Count});");
        sb.AppendLine();

        for (int tileIndex = 0; tileIndex < localMap.Count; tileIndex++)
        {
            sb.AppendLine($"map.Add(new ({localMap[tileIndex].Count}));\t// capacity for {tileIndex + 2} tiles");
        }

        sb.AppendLine();

        for (int tileIndex = 0; tileIndex < localMap.Count; tileIndex++)
        {
            List<List<int>> tileEntries = localMap[tileIndex];

            sb.AppendLine($"// map entries for {tileIndex + 2} tiles");

            for (int entryIndex = 0; entryIndex < tileEntries.Count; entryIndex++)
            {
                List<int> entry = tileEntries[entryIndex];

                sb.Append($"map[{tileIndex}].Add(new[] {{ ");

                for (int valueIndex = 0; valueIndex < entry.Count; valueIndex++)
                {
                    sb.Append(entry[valueIndex]);

                    if (valueIndex < (entry.Count - 1))
                        sb.Append(", ");
                }

                sb.AppendLine($" }});");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }



    /// <summary>
    /// Counts variations of operators in equations constructed with the number of tiles
    /// The are always 1 less operators than tiles in an equation and the 
    /// equation always ends with an operator.
    /// 
    ///     12 . 3 .. 4 ... 5 .... 6 .....
    ///        ^   ^    ^     ^      ^
    ///        |   |    |     |      |
    ///       op0  |   op2    |     op4
    ///           op1        op3
    ///    
    /// </summary>
    private static List<List<List<int>>> CreateMap()
    {
        List<List<List<int>>> localMap = new(5)
        {
            new List<List<int>>(1),     // 2 tiles have 1 equation pattern (12o)
            new List<List<int>>(2),     // 3 tiles have 2 equation patterns (123oo, 12o3o)
            new List<List<int>>(5),     // 4 tiles have 5 
            new List<List<int>>(14),    // 5 tiles have 14 
            new List<List<int>>(42)     // 6 tiles have 42   
        };

        // op0 can be between 0..1
        for (int op0 = 0; op0 < 2; op0++)
        {
            if (op0 == 1)
                AddMapRow(localMap[0], op0); // for 2 tiles

            // op1 can be between 0..2
            for (int op1 = 0; op1 < 3; op1++)
            {
                if (op0 + op1 == 2)
                    AddMapRow(localMap[1], op0, op1); // for 3 tiles

                int op0and1 = op0 + op1;

                // op2 can be between 0..3 and the total operator count has to be 5 or less
                for (int op2 = 0; (op2 < 4) && ((op0and1 + op2) < 6); op2++)
                {
                    if (op0and1 + op2 == 3)
                        AddMapRow(localMap[2], op0, op1, op2); // for 4 tiles

                    int op01and2 = op0 + op1 + op2;

                    // op3 can be between 0..4 and the total operator count has to be 5 or less
                    for (int op3 = 0; (op3 < 5) && ((op01and2 + op3) < 6); op3++)
                    {
                        if (op01and2 + op3 == 4)
                            AddMapRow(localMap[3], op0, op1, op2, op3); // for 5 tiles

                        // op4 can be between 1..5 operators and the total operator count in the 
                        // equation must be 5 or less. If the total operators so far 
                        // is less that the maximum we can just calculate the value because there 
                        // are no more operators reliant on this value further down the chain
                        if (op01and2 + op3 < 5)
                        {
                            int op4 = 5 - (op01and2 + op3);
                            AddMapRow(localMap[4], op0, op1, op2, op3, op4); // for 6 tiles
                        }
                    }
                }
            }
        }

        return localMap;
    }


    /// <summary>
    /// Converts operator variation counts into map rows
    /// </summary>
    /// <param name="map"></param>
    /// <param name="operators"></param>
    private static void AddMapRow(List<List<int>> map, params int[] operators)
    {
        int digitCount = 2; // start by pushing at least two digits on to the stack
        int digitTotal = 0;
        int operatorTotal = 0;
        List<int> row = new(operators.Length * 2); ;

        foreach (int operatorCount in operators)
        {
            if (operatorCount == 0) // push another digit on to the stack
                digitCount++;
            else
            {
                digitTotal += digitCount;
                operatorTotal += operatorCount;

                if (operatorTotal < digitTotal)
                {
                    row.Add(digitCount);    // push digits

                    int count = operatorCount;

                    while (count-- > 0)
                        row.Add(0); // add zeros which force the evaluation of an operator

                    digitCount = 1; // reset digit count
                }
                else
                    return; // invalid, more operators than digits so far, ignore this entry
            }
        }

        map.Add(row);
    }

    /// <summary>
    /// index is the number of tiles in the equation that this map entry is used for
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public List<int[]> this[int tileCount] => map[tileCount - 2];
}