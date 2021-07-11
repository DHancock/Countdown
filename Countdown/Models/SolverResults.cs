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

        /// <summary>
        /// the equation solutions
        /// </summary>
        public List<EquationItem> Solutions { get; } = new List<EquationItem>(500);


        /// <summary>
        /// If no solutions found this is the closest equation
        /// </summary>
        public string ClosestMatch { get; private set; } = string.Empty;


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
                    Solutions.AddRange(solvingEngine.Solutions);
                }
            }
            else if ((Solutions.Count == 0) && solvingEngine.HasClosestMatch) // no existing or new matches
            {
                // there is a race hazard reading Solutions.Count but the down side is trivial,
                // even the c# concurrent collections don't guarantee counts
                lock (updateLock)
                {
                    if (Math.Abs(solvingEngine.Difference) < Math.Abs(Difference)) // record the closest
                    {
                        Difference = solvingEngine.Difference;
                        ClosestMatch = solvingEngine.ClosestMatch;
                    }
                }
            }
        }
    }
}
