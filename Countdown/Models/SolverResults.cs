using System;
using System.Collections.Generic;
using Countdown.ViewModels;

namespace Countdown.Models
{
    internal sealed class SolverResults
    {
        /// <summary>
        /// thread synchronization objects
        /// </summary>
        private readonly object addLock = new object();
        private readonly object updateLock = new object();


        // collects a reference of each solver's solution list
        private List<List<EquationItem>> SolverLists { get; } = new List<List<EquationItem>>(100);

        // used to aggregate all the solver list contents into a single list
        public List<EquationItem> Solutions { get; private set; }

        // If no solutions found this is the closest equation
        public string ClosestEquation { get; private set; }

        // If no solutions found this is the closest result
        public int ClosestResult { get; private set; }

        public bool HasClosestResult => ClosestResult > 0;

        /// <summary>
        /// If no solutions found this is how far from the target 
        /// that the closest non match equation is
        /// </summary>
        public int Difference { get; private set; } = int.MaxValue;
       

        /// <summary>
        /// time taken for the solvers to complete
        /// </summary>
        public TimeSpan Elapsed { get; set; }



        /// <summary>
        /// aggregates the data from solving engines 
        /// that were run in parallel partitions
        /// </summary>
        /// <param name="solvingEngine"></param>
        public void AggregateData(SolvingEngine solvingEngine)
        {
            if ((solvingEngine is null) || (solvingEngine.Solutions is null))
                throw new ArgumentNullException(nameof(solvingEngine));
            
            if (solvingEngine.Solutions.Count > 0)
            {
                lock (addLock)
                {
                    SolverLists.Add(solvingEngine.Solutions);
                }
            }
            else if ((SolverLists.Count == 0) && solvingEngine.HasClosestResult) // no existing or new matches
            {
                // there is a race hazard reading SolverLists.Count but the down side is trivial,
                // even the c# concurrent collections don't guarantee counts
                lock (updateLock)
                {
                    if (solvingEngine.Difference < Difference) // record the closest
                    {
                        Difference = solvingEngine.Difference;
                        ClosestResult = solvingEngine.ClosestResult;
                        ClosestEquation = solvingEngine.ClosestEquation;
                    }
                }
            }
        }

        public void AggregateResults()
        {
            int size = 0;

            foreach (List<EquationItem> solverResults in SolverLists)
                size += solverResults.Count;

            Solutions = new(size);

            foreach (List<EquationItem> solverResults in SolverLists)
                Solutions.AddRange(solverResults);
        }
    }
}