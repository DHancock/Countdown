﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Countdown.SourceGenerator;

[Generator]
public class PostfixMapGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //System.Diagnostics.Debugger.Launch();

        context.RegisterPostInitializationOutput(static postInitializationContext => 
        {
            postInitializationContext.AddSource("PostfixMap.g.cs", SourceText.From(
                $$"""
                // <auto-generated/>

                namespace Countdown.Models;

                internal sealed partial class PostfixMap
                {
                    private PostfixMap()
                    {
                        {{GenerateMap()}}
                    }
                }
                """, Encoding.UTF8));
        });
    }

    private static string GenerateMap() => ConvertToText(CreateMap());

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
    [SuppressMessage("Style", "IDE0028:Simplify collection initialization")]
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
            {
                AddMapRow(localMap[0], op0); // for 2 tiles
            }

            // op1 can be between 0..2
            for (int op1 = 0; op1 < 3; op1++)
            {
                if (op0 + op1 == 2)
                {
                    AddMapRow(localMap[1], op0, op1); // for 3 tiles
                }

                int op0and1 = op0 + op1;

                // op2 can be between 0..3 and the total operator count has to be 5 or less
                for (int op2 = 0; (op2 < 4) && ((op0and1 + op2) < 6); op2++)
                {
                    if (op0and1 + op2 == 3)
                    {
                        AddMapRow(localMap[2], op0, op1, op2); // for 4 tiles
                    }

                    int op01and2 = op0 + op1 + op2;

                    // op3 can be between 0..4 and the total operator count has to be 5 or less
                    for (int op3 = 0; (op3 < 5) && ((op01and2 + op3) < 6); op3++)
                    {
                        if (op01and2 + op3 == 4)
                        {
                            AddMapRow(localMap[3], op0, op1, op2, op3); // for 5 tiles
                        }

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
            {
                digitCount++;
            }
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
                {
                    return; // invalid, more operators than digits so far, ignore this entry
                }
            }
        }

        map.Add(row);
    }

    private static string ConvertToText(List<List<List<int>>> localMap)
    {
        StringBuilder sb = new($"map = new({localMap.Count});");
        sb.AppendLine();

        for (int tileIndex = 0; tileIndex < localMap.Count; tileIndex++)
        {
            sb.AppendLine($"\t\tmap.Add(new({localMap[tileIndex].Count}));\t// capacity for {tileIndex + 2} tiles");
        }

        for (int tileIndex = 0; tileIndex < localMap.Count; tileIndex++)
        {
            List<List<int>> tileEntries = localMap[tileIndex];

            sb.AppendLine();
            sb.AppendLine($"\t\t{(tileIndex == 0 ? "List<int[]> " : "")}tiles = map[{tileIndex}];\t// {tileIndex + 2} tiles");

            for (int entryIndex = 0; entryIndex < tileEntries.Count; entryIndex++)
            {
                List<int> entry = tileEntries[entryIndex];

                sb.Append($"\t\ttiles.Add([");

                for (int valueIndex = 0; valueIndex < entry.Count; valueIndex++)
                {
                    sb.Append(entry[valueIndex]);

                    if (valueIndex < (entry.Count - 1))
                    {
                        sb.Append(", ");
                    }
                }

                sb.AppendLine($"]);");
            }
        }

        return sb.ToString();
    }
}

