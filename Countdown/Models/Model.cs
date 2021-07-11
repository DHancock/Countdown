using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Countdown.ViewModels;


namespace Countdown.Models
{
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

        // ok for non-cryptographic applications
        private readonly Random random = new Random();
        // handles word dictionaries
        private readonly WordDictionary wordDictionary = new WordDictionary();


        public Model()
        {
            UserSettings.UpgradeIfRequired();
        }


        /// <summary>
        /// Automatically chooses six tiles and a target. 
        /// Tiles can have 0 to 4 large tiles from the set (25, 50, 75, 100) without repetition,
        /// the remaining tiles are picked from the set (1, 2, 3, 4, 5, 6, 7, 8, 9, 10) allowing
        /// a maximum of two of each value. 
        /// The target can be between 100 and 999.
        /// </summary>
        /// <param name="tileOption"></param>
        public void GenerateNumberData(int tileOption)
        {
            int[] largeTiles = { 25, 50, 75, 100 };
            int[] smallTiles = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            if ((tileOption < 0) || (tileOption > largeTiles.Length))
                throw new ArgumentOutOfRangeException(nameof(tileOption));

            int largeTileCount = tileOption;
            int smallTileCount = Tiles.Length - largeTileCount;

            // pick large tiles
            PickCards(largeTileCount, largeTiles, 0);

            // pick small tiles
            PickCards(smallTileCount, smallTiles, largeTileCount);

            // pick target
            Target = random.Next(cMinTarget, cMaxTarget + 1);
        }


        /// <summary>
        /// Picks tiles randomly from the supplied array and loads the correspond ui 
        /// text boxes
        /// </summary>
        /// <param name="cardCount"></param>
        /// <param name="possibleCards"></param>
        /// <param name="tileIndex"></param>
        private void PickCards(int cardCount, int[] possibleTiles, int tileIndex)
        {
            while (cardCount > 0)
            {
                int rnd = random.Next(possibleTiles.Length);

                if (possibleTiles[rnd] != 0) // check the tile hasn't already been used
                {
                    Tiles[tileIndex++] = possibleTiles[rnd];

                    possibleTiles[rnd] = 0;  // mark this tile as used
                    --cardCount;
                }
            }
        }



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

            stopWatch.Stop();
            results.Elapsed = stopWatch.Elapsed;

            return results;
        }



        // solve the letters game
        public List<WordItem> Solve(char[] letters) => wordDictionary.Solve(letters);
        

        public void GenerateConundrum()
        {
            char[] conundrum = wordDictionary.GetConundrum(random);

            if (conundrum != null)
            {
                // shuffle the letters
                for (int index = conundrum.Length - 1; index > 0; --index)
                {
                    int next = random.Next(index + 1);

                    char temp = conundrum[next];
                    conundrum[next] = conundrum[index];
                    conundrum[index] = temp;
                }

                // convert to the models required format
                for (int index = 0; index < conundrum.Length; ++index)
                    Conundrum[index] = conundrum[index].ToString();
            }
        }

       

        public bool HasConundrums => wordDictionary.HasConundrums;
      
        public string Solve() => wordDictionary.SolveConundrum(Conundrum);
        
        public char GetVowel() => UserSettings.Vowels.GetLetter(random);
        
        public char GetConsonant() => UserSettings.Consonants.GetLetter(random);
    }
}

