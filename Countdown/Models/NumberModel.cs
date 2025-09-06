using Countdown.Utilities;

namespace Countdown.Models;

internal class NumberModel
{
    public const int cMinTarget = 101;
    public const int cMaxTarget = 999;
    public const int cNumberTileCount = 6;

    private readonly Random random = new Random();
    private readonly int[] largeTiles = [25, 50, 75, 100];
    private readonly int[] smallTiles = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];


    public int[] GenerateNumberData(int largeTileCount)
    {
        int[] output = new int[cNumberTileCount];

        int smallTileCount = cNumberTileCount - largeTileCount;
        int tileIndex = 0;

        if (largeTileCount > 0)
        {
            largeTiles.Shuffle();

            for (int index = 0; index < largeTileCount; index++)
            {
                output[tileIndex++] = largeTiles[index];
            }
        }

        smallTiles.Shuffle();

        for (int index = 0; index < smallTileCount; index++)
        {
            output[tileIndex++] = smallTiles[index];
        }

        return output;
    }

    public int GenerateTarget() => random.Next(cMinTarget, cMaxTarget + 1);

    public static SolverResults Solve(int[] tiles, int target)
    {
        SolverResults results = new SolverResults();

        for (int k = 2; k <= tiles.Length; k++)
        {
            Combinations<int> combinations = new Combinations<int>(tiles, k);

            foreach (int[] combination in combinations)
            {
                Permutations<int> permutations = new Permutations<int>(combination);

                Parallel.ForEach(permutations,
                                // the partitions local solver
                                () => new SolvingEngine(target),
                                // the parallel action
                                (permutation, loopState, solvingEngine) =>
                                {
                                    solvingEngine.Solve(permutation);
                                    return solvingEngine;
                                },
                                // local finally 
                                (solvingEngine) => results.AggregateData(solvingEngine));
            }
        }

        results.AggregateResults();

        return results;
    }
}
