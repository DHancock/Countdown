using Countdown.Utils;
using Countdown.ViewModels;

namespace Countdown.Models;

internal class Model
{
    public const int cMinTarget = 101;
    public const int cMaxTarget = 999;

    public const int cTileCount = 6;
    public const int cLetterCount = 9;

    // numbers game model data
    public int[] Tiles { get; } = new int[cTileCount];
    public int Target { get; set; } = cMinTarget;

    // letters game model data
    public string[] Letters { get; } = new string[cLetterCount];

    // conundrum model data
    public string[] Conundrum { get; } = new string[cLetterCount];

    private readonly WordDictionary wordDictionary = new WordDictionary();
    private readonly ConsonantList consonantList = new ConsonantList();
    private readonly VowelList vowelList = new VowelList();

    public Settings Settings { get; }


    public Model(Settings settings)
    {
        Settings = settings;
    }

    // solve the numbers game
    public static SolverResults Solve(int[] tiles, int target)
    {
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();

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

        stopWatch.Stop();
        results.Elapsed = stopWatch.Elapsed;

        return results;
    }


    // solve the letters game
    public List<WordItem> Solve(char[] letters) => wordDictionary.Solve(letters);

    // solve the conundrum game
    public string Solve() => wordDictionary.SolveConundrum(Conundrum);

    /// <summary>
    /// Automatically chooses six tiles and a target. 
    /// Tiles can have 0 to 4 large tiles from the set (25, 50, 75, 100) without repetition,
    /// the remaining tiles are picked from the set (1, 2, 3, 4, 5, 6, 7, 8, 9, 10) allowing
    /// a maximum of two of each value. 
    /// The target can be between 100 and 999.
    /// </summary>
    /// <param name="largeTileCount"></param>
    public void GenerateNumberData(int largeTileCount)
    {
        int[] largeTiles = { 25, 50, 75, 100 };
        int[] smallTiles = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        int smallTileCount = cTileCount - largeTileCount;
        int tileIndex = 0;

        if (largeTileCount > 0)
        {
            largeTiles.Shuffle();

            for (int index = 0; index < largeTileCount; index++)
                Tiles[tileIndex++] = largeTiles[index];
        }

        smallTiles.Shuffle();

        for (int index = 0; index < smallTileCount; index++)
            Tiles[tileIndex++] = smallTiles[index];

        Target = new Random().Next(cMinTarget, cMaxTarget + 1);
    }

    public void GenerateConundrum()
    {
        IList<char> conundrum = wordDictionary.GetConundrum().Shuffle();

        // convert to the models required format
        for (int index = 0; index < conundrum.Count; ++index)
            Conundrum[index] = conundrum[index].ToString();
    }

    public void GenerateLettersData(int vowelCount)
    {
        for (int index = 0; index < cLetterCount; index++)
        {
            if (index < vowelCount)
                Letters[index] = GetVowel().ToString();
            else
                Letters[index] = GetConsonant().ToString();
        }

        Letters.Shuffle().ReduceDuplicateSequences();
    }

    public bool HasConundrums => wordDictionary.HasConundrums;

    public char GetVowel() => vowelList.GetLetter();

    public char GetConsonant() => consonantList.GetLetter();
}
