namespace Countdown.Models;

/// <summary>
/// Generates all permutations for a collection of objects. 
/// </summary>
internal sealed partial class Permutations<T> : IEnumerable<T[]>
{
    /// <summary>
    /// store for the source 
    /// </summary>
    private readonly T[] input;

    /// <summary>
    /// null friendly comparer 
    /// </summary>
    private readonly IComparer<T> comparer;


    /// <summary>
    /// Initialise the enumerator used to generate permutations of the source collection.
    /// </summary>
    /// <param name="source">source collection</param>
    /// <param name="comp">comparer, use if type T can't implement an IComparable interface</param>
    /// <exception cref="T:System.ArgumentNullException"> <paramref name="source" /> is null. </exception>
    /// <exception cref="T:System.ArgumentOutOfRangeException"> <paramref name="source.Count" /> is less than 1 </exception>
    public Permutations(IEnumerable<T> source, IComparer<T> comp)
    {
        if (!source.Any())
        {
            throw new ArgumentOutOfRangeException(nameof(source));
        }

        // setup the comparer
        comparer = comp;

        // copy source, shallow if T is a reference type
        input = source.ToArray();
    }


    public Permutations(IEnumerable<T> source) : this(source, Comparer<T>.Default)
    {
    }

    /// <summary>
    /// Gets the non generic enumerator for the permutations.
    /// </summary>
    /// <returns></returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }


    /// <summary>
    /// Gets the generic enumerator for the permutations.
    /// </summary>
    /// <returns></returns>
    public IEnumerator<T[]> GetEnumerator()
    {
        return new PermutationEnumerator(this);
    }




    /// <summary>
    /// The enumerator for the permutations. Each time MoveNext()
    /// is called a new permutation generated.
    /// </summary>
    private struct PermutationEnumerator : IEnumerator<T[]>
    {
        /// <summary>
        /// the current permutation
        /// </summary>
        private readonly T[] current;

        /// <summary>
        /// enumerator state 
        /// </summary>
        private bool setUpFirstItem;

        /// <summary>
        /// null friendly comparison function
        /// </summary>
        private readonly IComparer<T> comparer;


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p"></param>
        public PermutationEnumerator(Permutations<T> p)
        {
            setUpFirstItem = true;
            current = p.input;
            comparer = p.comparer;
        }


        /// <summary>
        /// The IEnumerator.MoveNext interface implementation
        /// Moves to the next item. 
        /// </summary>
        /// <returns></returns>
        public bool MoveNext()
        {
            if (setUpFirstItem)
            {
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
            get
            {
                if (setUpFirstItem)
                {
                    throw new InvalidOperationException("Enumerator state is invalid");
                }

                T[] copy = new T[current.Length];
                current.AsSpan().CopyTo(copy);

                return copy;
            }
        }


        /// <summary>
        /// explicit IEnumerator.Current interface implementation.
        /// </summary>
        readonly object IEnumerator.Current
        {
            get { return this.Current; }
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
            // sort the input - its the first permutation
            Array.Sort(current, comparer);
        }


        /// <summary>
        /// Gets the next non duplicate permutation in lexicographic order.
        /// Algorithm L by Donald Knuth. This algorithm is described in 
        /// The Art of Computer Programming, Volume 4, Fascicle 2: Generating All Tuples and Permutations.
        /// </summary>
        /// <returns></returns>
        private readonly bool GetNext()
        {
            int end = current.Length - 1;
            int i = end;

            while (i > 0)
            {
                int j = i;
                i--;

                // find the end of the head and start of the tail
                if (comparer.Compare(current[i], current[j]) < 0)
                {
                    int k = end;

                    // find smallest tail element that is less than or equal to the last head element
                    while (comparer.Compare(current[k], current[i]) < 1)
                        k--;

                    // swap the head and tail elements
                    Swap(i, k);

                    // reverse the tail
                    while (j < end)
                        Swap(j++, end--);

                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Utility swap routine
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        private readonly void Swap(int a, int b)
        {
            T temp = current[a];
            current[a] = current[b];
            current[b] = temp;
        }
    }
}
