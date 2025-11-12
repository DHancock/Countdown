namespace Countdown.Models;

internal sealed class SolvingEngine
{
    // record closest matches <= this threshold
    public const int cNonMatchThreshold = 10;

    private readonly int target;

    // Operator identifiers
    private const int cMultiply = 0;
    private const int cAdd = 1;
    private const int cSubtract = 2;
    private const int cDivide = 3;

    // The postfix equation evaluation stacks. Each level of recursion has its own stack
    private readonly StackHelper<int> stacks;

    // used to convert postfix equations into infix strings
    private readonly StackHelper<char> charStack;

    // Keeps a record of the operators used when evaluating the current postfix equation.
    // Used to construct a string representation of the equation
    private readonly int[] operators;

    // store results locally, avoids locks
    public List<string> Solutions { get; } = new(25);
    public List<(string, int)> Closests { get; } = new(25);

    private readonly SolverResults solverResults;
 
    public SolvingEngine(int value, SolverResults results)
    {
        const int n = 6;   // the "n choose k" maximum permutation length

        target = value;
        solverResults = results;

        // initialize the stacks. Each recursive call gets a copy
        // of the current stack so that when the recursion unwinds
        // the caller can simply proceed with the next operator
        stacks = new StackHelper<int>(n, n);

        // store for the operators used to build a string representation of the current equation
        operators = new int[n - 1];

        // minimum size is 41 chars:
        // [offset] + [size] + [space for may be 4 parentheses] + "100 + ((((75 + 50) + 25) + 10) + 1)" 
        charStack = new StackHelper<char>(44, n);
    }

    /// <summary>
    /// The postfix equation solving method. It implements a sequence of pushing zero or more tiles 
    /// onto the stack before executing an operator. It then recurses executing another sequence. Each
    /// recursive call gets its own copy of the current stack so that when the recursion unwinds it 
    /// can simply continue with the next operator. The recursion ends when the map is completed.
    /// Although it is a linear function conceptually it can be thought of as a tree. Each sequence
    /// splits into 4 branches (one for each operator), and for each recursion every branch further 
    /// splits into 4 more branches. This repeats until the last sequence in the map when the final 
    /// result is obtained for the 4 operators. After the first operator is executed subsequent
    /// equations are calculated by executing only one operator rather than the complete equation.
    /// The same principal applies for each node in the tree as the recursion unwinds.
    /// </summary>
    /// <param name="stackHead">the stack head index</param>
    /// <param name="mapEntry">the current postfix map entry</param>
    /// <param name="mapIndex">position within the map</param>
    /// <param name="permutation">the current tile permutation</param>
    /// <param name="permutationIndex">position in the permutation</param>
    /// <param name="depth">recursion depth</param>
    private void SolveRecursive(int stackHead, ReadOnlySpan<int> mapEntry, int mapIndex, ReadOnlySpan<int> permutation, int permutationIndex, int depth)
    {
        const int cInvalidResult = 0;

        // identify which stack to use for this depth of recursion
        Span<int> stack = stacks[depth];

        // read how many numbers that are required to be pushed on to the stack
        int pushCount = mapEntry[mapIndex++];

        bool copyToNextStack = true;   // avoid multiple copies

        // if at least two tiles are pushed onto the stack, the operator will act on
        // tiles directly rather than a derived value. This allows simple duplicate 
        // equations to be filtered out e.g. 4+6 == 6+4 
        bool derived = true;

        if (pushCount > 0)
        {
            derived = pushCount < 2;

            while (pushCount-- > 0)
                stack[++stackHead] = permutation[permutationIndex++]; // push

            ++mapIndex; // for the first operator
        }

        int right = stack[stackHead--]; // pop
        int left = stack[stackHead];    // peek

        for (int op = cMultiply; op <= cDivide; op++)
        {
            int result = cInvalidResult;

            if (op == cMultiply)
            {
                if ((left > 1) && (right > 1) && (derived || (left <= right)))
                {
                    result = left * right;
                }
            }
            else if (op == cAdd)
            {
                if (derived || (left <= right))
                {
                    result = left + right;
                }
            }
            else if (op == cSubtract)
            {
                result = left - right;
            }
            else // cDivide
            {
                if ((right > 1) && (left >= right) && ((left % right) == 0))
                {
                    result = left / right;
                }
            }

            if (result > cInvalidResult)  // valid result
            {
                operators[depth] = op;

                if (mapIndex < mapEntry.Length)   // some left
                {
                    Span<int> nextStack = stacks[depth + 1];

                    if (copyToNextStack)
                    {
                        // copy the current stack for the next depth of recursion
                        copyToNextStack = false;

                        for (int index = 0; index < stackHead; index++)
                        {
                            nextStack[index] = stack[index];
                        }
                    }

                    // record the current result
                    nextStack[stackHead] = result; // poke

                    // evaluate the next sequence
                    SolveRecursive(stackHead, mapEntry, mapIndex, permutation, permutationIndex, depth + 1);
                }
                else
                {
                    if (result == target)   // got one...
                    {
                        Solutions.Add(ConvertToString(mapEntry, permutation));
                        break;
                    }

                    if (Solutions.Count == 0) // no solutions so record if its the closest result
                    {
                        int difference = Math.Abs(target - result);

                        if (difference <= cNonMatchThreshold)
                        {
                            if (difference < solverResults.LowestDifference)
                            {
                                solverResults.LowestDifference = difference;
                            }

                            if (difference == solverResults.LowestDifference)
                            {
                                Closests.Add(($"{ConvertToString(mapEntry, permutation)} = {result}", difference));
                            }
                        }
                    }

                    if ((op == cMultiply) && (result < target))
                    {
                        // multiplied but result is still less than target
                        // no point evaluating the other three operators
                        // (multiply by one is an invalid operation)
                        break;
                    }
                }
            }
        }
    }

    // Convert the current postfix equation to an infix formatted string
    // This works in the same way as evaluating the postfix equation
    // but instead of calculating simply concatenates strings.
    private string ConvertToString(ReadOnlySpan<int> mapEntry, ReadOnlySpan<int> tiles)
    {
        const int cOffsetIndex = 0;      // holds the offset to the start of the data
        const int cSizeIndex = 1;        // holds the size of the data

        static void WriteTileValue(int value, Span<char> line)
        {
            const int cOffsetValue = 6; // allow space for prepending 4 opening parentheses

            line[cOffsetIndex] = (char)cOffsetValue;

            if (value < 10)
            {
                line[cSizeIndex] = (char)1;
                line[cOffsetValue] = (char)('0' + value);
            }
            else if (value < 100)
            {
                line[cSizeIndex] = (char)2;
                line[cOffsetValue] = (char)('0' + value / 10);
                line[cOffsetValue + 1] = (char)('0' + value % 10);
            }
            else
            {
                line[cSizeIndex] = (char)3;
                line[cOffsetValue] = '1';
                line[cOffsetValue + 1] = '0';
                line[cOffsetValue + 2] = '0';
            }
        }

        static char Operator(int op)
        {
            switch (op)
            {
                case cMultiply: return '×';
                case cAdd: return '+';
                case cSubtract: return '-';
                default: return '÷';
            }
        }

        // left is also the destination
        void ConcatWithParentheses(Span<char> left, int op, ReadOnlySpan<char> right)
        {
            // prepend opening parentheses
            --left[cOffsetIndex];
            left[left[cOffsetIndex]] = '(';
            ++left[cSizeIndex];

            Concat(left, op, right);

            // append closing parentheses
            left[left[cOffsetIndex] + left[cSizeIndex]] = ')';
            ++left[cSizeIndex];
        }

        // left is also the destination
        void Concat(Span<char> left, int op, ReadOnlySpan<char> right)
        {
            int writeIndex = left[cOffsetIndex] + left[cSizeIndex];

            // append operator
            left[writeIndex++] = ' ';
            left[writeIndex++] = Operator(op);
            left[writeIndex++] = ' ';

            // copy right into left
            right.Slice(right[cOffsetIndex], right[cSizeIndex]).CopyTo(left.Slice(writeIndex));

            // update size
            left[cSizeIndex] += (char)(right[cSizeIndex] + 3);
        }


        int mapIndex = 0;
        int operatorIndex = 0;
        int tileIndex = 0;
        int stackHead = -1;

        while (true)
        {
            int tileCount = mapEntry[mapIndex++];

            if (tileCount > 0) // push tiles, mapEntry[0] will always push at least two tiles
            {
                while (tileCount-- > 0)
                {
                    WriteTileValue(tiles[tileIndex++], charStack[++stackHead]);
                }

                ++mapIndex; // always followed by at least one operator
            }

            // execute operator
            ReadOnlySpan<char> right = charStack[stackHead--]; // pop
            Span<char> left = charStack[stackHead];    // peek
            int operand = operators[operatorIndex++];

            if (mapIndex < mapEntry.Length)
            {
                ConcatWithParentheses(left, operand, right); // left is also the destination
            }
            else
            {
                Concat(left, operand, right);
                return left.Slice(left[cOffsetIndex], left[cSizeIndex]).ToString();
            }
        }
    }

    /// <summary>
    /// starts the solving engine for the given permutation 
    /// </summary>
    public void Solve(ReadOnlySpan<int> permutation)
    {
        foreach (int[] mapEntry in PostfixMap.Instance[permutation.Length])
        {
            SolveRecursive(-1, mapEntry, 0, permutation, 0, 0);
        }
    }

    private readonly struct StackHelper<T> where T : struct
    {
        private readonly T[][] list;

        public StackHelper(int stackSize, int stackCount)
        {
            list = new T[stackCount][];

            for (int index = 0; index < stackCount; index++)
            {
                list[index] = new T[stackSize];
            }
        }

        public Span<T> this[int i] => list[i];
    }
}
