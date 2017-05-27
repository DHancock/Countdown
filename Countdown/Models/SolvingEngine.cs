using Countdown.ViewModels;
using System.Collections.Generic;


namespace Countdown.Models
{
    internal sealed class SolvingEngine
    {
        /// <summary>
        /// if there are no solutions yet then results less than this 
        /// threshold will be recorded if the difference is less any previous 
        /// recorder non solution results 
        /// </summary>
        private const int cNonMatchThreshold = 11;

        /// <summary>
        /// the target value
        /// </summary>
        private readonly int target;

        /// <summary>
        /// the map for this solver instance
        /// </summary>
        private readonly List<List<int>> map;

        /// <summary>
        /// records the closest non matching equation difference
        /// </summary>
        private int absDifference = cNonMatchThreshold;

        /// <summary>
        /// if there is a non matching result less than or 10 from the target result 
        /// </summary>
        public bool HasClosestMatch { get; private set; } = false;


        /// <summary>
        /// Operator identifiers
        /// </summary>
        private const int cMultiply = 0;
        private const int cAdd = 1;
        private const int cSubtract = 2;
        private const int cDivide = 3;

        /// <summary>
        /// String equivalents of the operators
        /// </summary>
        private readonly string[] opStr = { " × ", " + ", " - ", " ÷ " };

        /// <summary>
        /// The postfix equation evaluation stacks. 
        /// Each level of recursion has its own copy of the stack
        /// </summary>
        private readonly int[][] stacks;

        /// <summary>
        /// Keeps a record of the operators used when evaluating the
        /// current postfix equation. Used to construct a string
        /// representation of the equation
        /// </summary>
        private readonly int[] operators;

        /// <summary>
        /// a list of matching equations
        /// </summary>
        public List<EquationItem> Solutions { get; } = new List<EquationItem>();

        /// <summary>
        /// If no solutions found this is the closest equation
        /// </summary>
        public string ClosestMatch { get; private set; } = string.Empty;

        /// <summary>
        /// If no solutions found this is how far from the target 
        /// that the closest non match equation is, could be negative
        /// </summary>
        public int Difference { get; private set; } = 0;






        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="tiles"></param>
        /// <param name="target"></param>
        /// <param name="k">the length of a permutation</param>
        public SolvingEngine(int target, int k)
        {
            // initialize the stacks. Each recursive call gets a copy
            // of the current stack so that when the recursion unwinds
            // the caller can simply proceed with the next operator
            stacks = new int[k - 1][];

            for (int index = 0; index < (k - 1); index++)
                stacks[index] = new int[k];

            // store for the operators used to build a string representation of the current equation
            operators = new int[k - 1];

            // record params
            this.target = target;
            map = PostfixMap.Instance[k];
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
        /// <param name="stackHead">the stack head pointer</param>
        /// <param name="mapEntry">the current postfix map entry</param>
        /// <param name="mapIndex">position within the map</param>
        /// <param name="permutation">the current tile permutation</param>
        /// <param name="permutationIndex">position in the permutation</param>
        /// <param name="depth">recursion depth</param>
        private void SolveRecursive(int stackHead, List<int> mapEntry, int mapIndex, List<int> permutation, int permutationIndex, int depth)
        {
            const int cInvalidResult = 0;

            // identify which stack to use for this depth of recursion
            int[] stack = stacks[depth];

            // read how many numbers that are required to be pushed on to the stack
            int pushCount = mapEntry[mapIndex++];

            // if at least two tiles are pushed onto the stack, the operator will act on
            // tiles directly rather than a derived value. This allows simple duplicate 
            // equations to be filtered out e.g. 4+6 == 6+4 
            bool derived = true;

            if (pushCount > 0)
            {
                derived = (pushCount < 2);

                while (pushCount-- > 0)
                    stack[++stackHead] = (permutation[permutationIndex++]); // push

                ++mapIndex; // for the first operator
            }

            int right = stack[stackHead--]; // pop
            int left = stack[stackHead];    // peek

            for (int op = cMultiply; op <= cDivide; op++)
            {
                operators[depth] = op; // record the current operator

                int result = cInvalidResult;

                if (op == cMultiply)
                {
                    if ((left > 1) && (right > 1) && (derived || (left <= right)))
                        result = left * right;
                }
                else if (op == cAdd)
                {
                    if (derived || (left <= right))
                        result = left + right;
                }
                else if (op == cSubtract)
                {
                    result = left - right;
                }
                else // cDivide
                {
                    if ((right > 1) && (left >= right) && ((left % right) == 0))
                        result = left / right;
                }

                if (result > cInvalidResult)  // valid result
                {
                    if (mapIndex < mapEntry.Count)   // some left
                    {
                        // copy the current stack for the next depth of recursion
                        int[] localStack = stacks[depth + 1];

                        for (int index = 0; index < stackHead; index++)
                            localStack[index] = stack[index];

                        // record the current result
                        localStack[stackHead] = result; // poke

                        // evaluate the next sequence
                        SolveRecursive(stackHead, mapEntry, mapIndex, permutation, permutationIndex, depth + 1);
                    }
                    else
                    {
                        if (result == target)   // got one...
                        {
                            Solutions.Add(new EquationItem(ConvertToString(mapEntry, permutation)));
                            break;
                        }

                        if (Solutions.Count == 0) // no solutions so record if its the closest result
                        {
                            int difference;

                            if (result < target)
                                difference = target - result;
                            else
                                difference = result - target;

                            if (difference < absDifference)
                            {
                                absDifference = difference;
                                HasClosestMatch = true;
                                Difference = target - result;
                                ClosestMatch = ConvertToString(mapEntry, permutation);
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



        /// <summary>
        /// Convert the current postfix equation to an infix formatted string
        /// This works in the same way as evaluating the postfix equation
        /// but instead of calculating simply concatenates strings.
        /// </summary>
        /// <param name="mapEntry"></param>
        /// <param name="permutation"></param>
        /// <returns></returns>
        private string ConvertToString(List<int> mapEntry, List<int> permutation)
        {
            int mapIndex = 0;
            int operatorCount = 0;
            int permutationCount = 0;

            string[] stack = new string[permutation.Count];
            int stackHead = -1;

            do
            {
                int digitCount = mapEntry[mapIndex++];

                if (digitCount > 0) // push digits, mapEntry[0] will always push at least two digits
                {
                    while (digitCount-- > 0)
                        stack[++stackHead] = (permutation[permutationCount++]).ToString();
                }
                else // execute operator
                {
                    string right = stack[stackHead--]; // pop
                    string left = stack[stackHead];    // peek
                    string operand = opStr[operators[operatorCount++]];

                    if (mapIndex < mapEntry.Count)
                        stack[stackHead] = "(" + left + operand + right + ")"; // poke
                    else
                        stack[stackHead] = left + operand + right;
                }
            }
            while (mapIndex < mapEntry.Count);

            return stack[stackHead];
        }


        /// <summary>
        /// starts the solving engine for the given permutation 
        /// </summary>
        public void Solve(List<int> permutation)
        {
            foreach (List<int> mapEntry in map)
                SolveRecursive(-1, mapEntry, 0, permutation, 0, 0);
        }
    }
}
