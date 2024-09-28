namespace Countdown.Models;

/// <summary>
/// Class that generates combinations of k items from the supplied collection 
/// without duplicates. The combinations will be in lexicographical order.
/// 
/// If the supplied collection itself contains duplicate entries this implementation
/// will not produce duplicate combinations e.g.
/// 
///     For a source of {3, 1, 1, 1} and k = 2 the code generates 
///     two combinations:
///  
///     {1, 1} 
///     {1, 3}
///        
/// </summary>
/// <typeparam name="T"></typeparam>
internal sealed partial class Combinations<T> : IEnumerable<T[]>
{
    /// <summary>
    /// copy of the source 
    /// </summary>
    private readonly T[] input;

    /// <summary>
    /// the "n choose k" variables
    /// </summary>
    private readonly int n;
    private readonly int k;

    private readonly bool sourceHasDuplicates;

    /// <summary>
    /// null friendly comparer 
    /// </summary>
    private readonly IComparer<T> comparer;

    /// <summary>
    /// Initialise the enumerator used to generate combinations of the source collection.
    /// </summary>
    /// <param name="source">source collection</param>
    /// <param name="k">the n choose k variable</param>
    /// <param name="comp">optional comparer, use if type T can't implement an IComparable interface</param>
    /// <exception cref="T:System.ArgumentNullException"> <paramref name="source" /> is null </exception>
    /// <exception cref="T:System.ArgumentOutOfRangeException"> <paramref name="source.Count" /> is less than 1 </exception>
    /// <exception cref="T:System.ArgumentOutOfRangeException"> <paramref name="k" /> is less than 0 or greater than source count </exception>
    public Combinations(IEnumerable<T> source, int k, IComparer<T> comp)
    {
        // expensive if source isn't ICollection
        int n = source.Count();

        if (n < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(source));
        }

        if ((k < 0) || (k > n))
        {
            throw new ArgumentOutOfRangeException(nameof(k));
        }

        // setup the comparer
        comparer = comp;

        // record the "n choose k" variables 
        this.k = k;
        this.n = n;

        // copy source, shallow if T is a reference type
        input = source.ToArray();

        // sort the input to ensure lexicographical ordering
        Array.Sort(input, comparer);

        // now check the source for duplicates
        sourceHasDuplicates = false;

        for (int index = 1; index < n; index++)
        {
            if (comparer.Compare(input[index - 1], input[index]) == 0)
            {
                sourceHasDuplicates = true;
                break;
            }
        }
    }


    public Combinations(IEnumerable<T> source, int k) : this(source, k, Comparer<T>.Default)
    {
    }


    /// <summary>
    /// Gets the non generic enumerator for the combinations.
    /// </summary>
    /// <returns></returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }


    /// <summary>
    /// Gets the generic enumerator for the combinations.
    /// </summary>
    /// <returns></returns>
    public IEnumerator<T[]> GetEnumerator()
    {
        return new CombinationEnumerator(this);
    }



    private struct CombinationEnumerator : IEnumerator<T[]>
    {
        /// <summary>
        /// the source collection
        /// </summary>
        private readonly T[] input;

        /// <summary>
        /// used to generate the next combination when the source
        /// collection contains duplicate entries
        /// </summary>
        private T[]? current;
        private T[]? previous;

        /// <summary>
        /// An integer look up table. The next combination is generated in the table. 
        /// The enumerator output is built from the table indexing in to the source.
        /// </summary>
        private readonly int[] map;

        /// <summary>
        /// the "n choose k" variables
        /// </summary>
        private readonly int n;
        private readonly int k;

        /// <summary>
        /// Stores if the source contains duplicates
        /// </summary>
        private readonly bool sourceHasDuplicates;

        /// <summary>
        /// null friendly comparer 
        /// </summary>
        private readonly IComparer<T> comparer;

        /// <summary>
        /// enumerator state 
        /// </summary>
        private bool setUpFirstItem;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p"></param>
        public CombinationEnumerator(Combinations<T> c)
        {
            setUpFirstItem = true;
            input = c.input;
            n = c.n;
            k = c.k;
            sourceHasDuplicates = c.sourceHasDuplicates;

            map = new int[k];

            comparer = c.comparer;

            if (sourceHasDuplicates)
            {
                previous = new T[k];
                current = new T[k];
            }
        }


        /// <summary>
        /// The IEnumerator.MoveNext interface implementation
        /// Moves to the next item. 
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (setUpFirstItem)
            {
                if (k < 1) // nothing to do
                {
                    return false;
                }

                setUpFirstItem = false;
                Initialise();
                return true;
            }

            if (GetNext())
            {
                return true;
            }

            // prepare for the next iteration sequence
            setUpFirstItem = true;
            return false;
        }


        /// <summary>
        /// The IEnumerator.Reset interface implementation
        /// Provided for COM interoperability, but doesn't need to be implemented
        /// </summary>
        public void Reset()
        {
            setUpFirstItem = true;
        }


        /// <summary>
        /// IEnumerator<T>.Current interface implementation. 
        /// </summary>
        public readonly T[] Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (setUpFirstItem)
                {
                    throw new InvalidOperationException("Enumerator state is invalid");
                }

                T[] copy = new T[k];

                if (sourceHasDuplicates)
                {
                    current.AsSpan().CopyTo(copy);
                }
                else
                {
                    // building from the map here avoids an extra copy into current.
                    for (int index = 0; index < k; index++)
                    {
                        copy[index] = input[map[index]];
                    }
                }

                return copy;
            }
        }


        /// <summary>
        /// explicit IEnumerator.Current interface implementation.
        /// </summary>
        readonly object IEnumerator.Current
        {
            get { return Current; }
        }


        /// <summary>
        /// Nothing to dispose, no unmanaged resources used
        /// </summary>
        readonly void IDisposable.Dispose()
        {
        }


        /// <summary>
        /// Set up state for the first enumerator call
        /// </summary>
        private readonly void Initialise()
        {
            // initialise the map with the first combination
            for (int index = 0; index < k; index++)
            {
                map[index] = index;
            }

            if (sourceHasDuplicates)
            {
                // store current, it's used to generate the next combination
                input.AsSpan(0, k).CopyTo(current);
            }
        }


        /// <summary>
        /// Generates the next combination of the input collection.
        /// It first gets the next map entry and uses it to
        /// build a list of the input objects of type T.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool GetNext()
        {
            if (!sourceHasDuplicates)
            {
                return GetNextMapEntry();
            }
            
            Debug.Assert(previous is not null);
            Debug.Assert(current is not null);

            (previous, current) = (current, previous);

            while (GetNextMapEntry())
            {
                int cr = 0;

                // build current from the map, checking for duplicates
                // by comparing this combination with the previous combination
                for (int index = 0; index < k; index++)
                {
                    current[index] = input[map[index]];

                    if (cr == 0)  // equal so far
                    {
                        cr = comparer.Compare(current[index], previous[index]);

                        if (cr < 0) // less than, its a duplicate
                        {
                            break;
                        }
                    }
                }

                if (cr > 0)  // greater than, its not a duplicate
                {
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Generate combinations of integers in lexicographic order
        /// The resultant combinations are used as a look up table to build
        /// combinations of the input objects of type T 
        /// Algorithm by Donald Knuth
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly bool GetNextMapEntry()
        {
            Span<int> span = map;

            // start at last item
            int i = k - 1;

            while (span[i] == n - k + i)  // find next item to increment 
            {
                if (--i < 0)
                {
                    return false; // all done
                }
            }

            ++span[i]; // increment

            // do next 
            for (int j = i + 1; j < k; j++)
            {
                span[j] = span[i] + j - i;
            }

            return true;
        }
    }
}
