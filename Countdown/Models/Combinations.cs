using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Countdown.Models
{
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
    internal sealed class Combinations<T> : IEnumerable<T[]> 
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

        /// <summary>
        /// the source has duplicate entries
        /// </summary>
        private readonly bool noDuplicates;

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
        public Combinations(IEnumerable<T> source, int k, IComparer<T> comp = null)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            // expensive if source isn't ICollection
            int n = source.Count();  

            if (n < 1)
                throw new ArgumentOutOfRangeException(nameof(source));

            if ((k < 0) || (k > n))
                throw new ArgumentOutOfRangeException(nameof(k));

            // setup the comparer
            comparer = comp ?? Comparer<T>.Default;
            
            // record the "n choose k" variables 
            this.k = k;
            this.n = n;

            // copy source, shallow if T is a reference type
            input = source.ToArray();

            // sort the input to ensure lexicographical ordering
            Array.Sort(input, comparer);

            // now check the source for duplicates
            noDuplicates = true;

            for (int index = 1; (index < n) && noDuplicates; index++)
                noDuplicates = comparer.Compare(input[index - 1], input[index]) != 0;
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




        /// <summary>
        /// The enumerator for the combinations. Each time MoveNext()
        /// is called a new combination generated on demand.
        /// </summary>
        private struct CombinationEnumerator : IEnumerator<T[]>
        {
            /// <summary>
            /// the source collection
            /// </summary>
            private readonly T[] input;

            /// <summary>
            /// the current combination
            /// </summary>
            private T[] current;

            /// <summary>
            /// The previous combination
            /// Used to filter out duplicate combinations
            /// </summary>
            private T[] previous;

            /// <summary>
            /// An integer look up table
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
            private readonly bool noDuplicates;

            /// <summary>
            /// null friendly comparer 
            /// </summary>
            private readonly IComparer<T> comparer;

            /// <summary>
            /// enumerator state 
            /// </summary>
            private bool setUpFirstItem ;

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
                noDuplicates = c.noDuplicates;

                // allocate storage
                map = new int[k];
                current = new T[k];

                comparer = c.comparer;
                previous = (noDuplicates) ? null : new T[k];
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
                    return Initialise();
                }

                if (GetNext())
                    return true;

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
            public T[] Current
            {
                get
                {
                    if (setUpFirstItem)
                        throw new InvalidOperationException("Enumerator state is invalid");

                    T[] copy = new T[k];
                    current.AsSpan().CopyTo(copy);

                    return copy;
                }
            }


            /// <summary>
            /// explicit IEnumerator.Current interface implementation.
            /// </summary>
            object IEnumerator.Current
            {
                get { return this.Current; }
            }


            /// <summary>
            /// Nothing to dispose, no unmanaged resources used
            /// </summary>
            void IDisposable.Dispose()
            {
            }


            /// <summary>
            /// Set up state for the first enumerator call
            /// </summary>
            private bool Initialise()
            {
                // first check if there is anything to do
                if (k < 1)
                    return false;

                // initialise the map with 0, 1, 2... the first combination
                for (int index = 0; index < k; index++)
                    map[index] = index;

                // copy sorted input to current 
                for (int index = 0; index < k; index++)
                    current[index] = input[index];

                return true;
            }


            /// <summary>
            /// Generates the next combination of the input collection.
            /// It first gets the next map entry and uses it to
            /// build a list of the input objects of type T.
            /// </summary>
            private bool GetNext()
            {
                if (noDuplicates) // the input has no duplicate entries
                {
                    if (GetNextMapEntry())
                    {
                        // build current from the map
                        for (int index = 0; index < k; index++)
                            current[index] = input[map[index]];

                        return true;
                    }

                    return false;
                }
                else
                {
                    // swap current to previous
                    T[] temp = previous;
                    previous = current;
                    current = temp;
                    
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
                                    break;
                            }
                        }

                        if (cr > 0)  // greater than, its not a duplicate
                            return true;
                    }  
                  
                    return false;
                }
            }
           

            /// <summary>
            /// Generate combinations of integers in lexicographic order
            /// The resultant combinations are used as a look up table to build
            /// combinations of objects of type T 
            /// Algorithm by Donald Knuth
            /// </summary>
            private bool GetNextMapEntry()
            {
                // start at last item
                int i = k - 1;

                while (map[i] == (n - k + i))  // find next item to increment 
                {
                    if (--i < 0)
                        return false; // all done
                }

                ++map[i]; // increment

                // do next 
                for (int j = i + 1; j < k; j++)
                    map[j] = map[i] + j - i;

                return true;
            }
        }
    }
}


