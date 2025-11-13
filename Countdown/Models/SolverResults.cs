namespace Countdown.Models;

internal sealed class SolverResults
{
    private readonly Lock solvedLock = new();
    private readonly Lock closestLock = new();
    private readonly Lock differenceLock = new();

    // collects a reference of each solver's solution list
    private readonly List<List<string>> solverLists = new(32);
    private readonly List<List<(string, int)>> closestLists = new(32);

    public bool HasSolutions => solverLists.Count > 0;

    private int difference = SolvingEngine.cNonMatchThreshold;

    // How far from the target that the closest non matching equation is.
    // Shared by all soving engine instances so that they each know when to ignore closest matches.
    public int LowestDifference
    {
        get => difference;

        set
        {
            lock (differenceLock)
            {
                if (difference > value)
                {
                    difference = value;
                }
            }
        }
    }

    // aggregates the solving engine results from each parallel partition
    public void AggregateData(SolvingEngine solvingEngine)
    {
        if (solvingEngine.Solutions.Count > 0)
        {
            lock (solvedLock)
            {
                solverLists.Add(solvingEngine.Solutions);
            }
        }
        else if ((solvingEngine.Closests.Count > 0) && (solverLists.Count == 0))
        {
            lock (closestLock)
            {
                closestLists.Add(solvingEngine.Closests);
            }
        }
    }

    public List<string> GetResults()
    {
        int size = 0;
        List<string> results;

        if (solverLists.Count > 0)
        {
            foreach (List<string> solverResults in solverLists)
            {
                size += solverResults.Count;
            }

            results = new(size);

            foreach (List<string> solverResults in solverLists)
            {
                results.AddRange(solverResults);
            }

            return results;
        }

        size += 2; // allow space for any preamble

        foreach (List<(string, int difference)> solverResults in closestLists)
        {
            foreach ((string equation, int difference) in solverResults)
            {
                if (difference == LowestDifference)
                {
                    ++size;
                }
            }
        }

        results = new(size);

        foreach (List<(string equation, int difference)> solverResults in closestLists)
        {
            foreach ((string equation, int difference) in solverResults)
            {
                if (difference == LowestDifference)
                {
                    results.Add(equation);
                }
            }
        }

        return results;
    }
}
