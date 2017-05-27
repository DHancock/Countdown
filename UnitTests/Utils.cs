using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Countdown.UnitTests
{
    internal static class Utils
    {

        /// <summary>
        /// Calculates the combination size using the "n choose k" equation
        /// (the binomial coefficient). This calculation is only valid when the 
        /// source of the combination doesn't itself contain duplicates.
        /// </summary>
        /// <param name="n"></param>
        /// <param name="k"></param>
        /// <returns></returns>
        public static int CombinationSize(int n, int k)
        {
            return Factorial(n) / (Factorial(k) * Factorial(n - k));
        }



        /// <summary>
        /// Calculates the number of permutations for the source list
        /// when the list could also include duplicates. 
        /// Not an optimal implementation.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static long PermutaionSize<T>(IEnumerable<T> source)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            long divisor = 1;
            EqualityComparer<T> e = EqualityComparer<T>.Default;

            foreach (T distinctItem in source.Distinct())
            {
                divisor *= Factorial(source.Count(s => e.Equals(s, distinctItem)));
            }

            return Factorial(source.Count()) / divisor;
        }



        /// <summary>
        /// Factorials without recursion or multiplication
        /// Not pretty but quick.
        /// </summary>
        /// <param name="n"></param>
        /// <returns>n!</returns>
        private static int Factorial(int n)
        {
            switch (n)
            {
                case 0: return 1;
                case 1: return 1;
                case 2: return 2;
                case 3: return 6;
                case 4: return 24;
                case 5: return 120;
                case 6: return 720;
                case 7: return 5040;
                case 8: return 40320;
                case 9: return 362880;
                case 10: return 3628800;
                case 11: return 39916800;
                case 12: return 479001600;
                default:
                    // 13! overflows an Int32
                    throw new ArgumentOutOfRangeException();
            }
        }


        /// <summary>
        /// Not particularly efficient but ok for test code due to its simplicity
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        internal static void RemoveDuplicates<T>(List<List<T>> list, IComparer<T> comparer = null)
        {
            if (list != null)
            {
                int current = 0;
                int next = 1;

                while (next < list.Count)
                {
                    if (Compare(list[current], list[next], comparer) == 0)
                        list.RemoveAt(next);
                    else
                    {
                        ++current;
                        ++next;
                    }
                }
            }
        }





        internal static void TestComparers()
        {
            List<int> l0 = null;
            List<int> l1 = new List<int> { 0, 2, 3 };
            List<int> l2 = new List<int> { 0, 2, 3 };
            List<int> l3 = new List<int> { 1, 2, 4 };
            List<int> l4 = new List<int> { 1, 2 };

            Assert.IsTrue(Compare(l0, l0) == 0);    // nulls
            Assert.IsTrue(Compare(l1, l0) > 0);
            Assert.IsTrue(Compare(l0, l1) < 0);

            Assert.IsTrue(Compare(l1, l2) == 0);    // contents
            Assert.IsTrue(Compare(l1, l3) < 0);
            Assert.IsTrue(Compare(l3, l1) > 0);

            Assert.IsTrue(Compare(l1, l1) == 0);    // reference 

            Assert.IsTrue(Compare(l1, l4) > 0);     // lengths
            Assert.IsTrue(Compare(l4, l1) < 0);

            List<List<int>> ll0 = null;
            List<List<int>> lr0 = null;

            List<List<int>> ll1 = new List<List<int>> { l1, l1 };
            List<List<int>> ll2 = new List<List<int>> { l2, l2 };
            List<List<int>> ll3 = new List<List<int>> { l1, l3 };
            List<List<int>> ll4 = new List<List<int>> { l1 };

            Assert.IsTrue(Compare(lr0, ll0) == 0);  // nulls
            Assert.IsTrue(Compare(ll1, ll0) > 0);
            Assert.IsTrue(Compare(ll0, ll1) < 0);
            Assert.IsTrue(IsEqual(lr0, ll0));
            Assert.IsFalse(IsEqual(ll1, ll0));

            Assert.IsTrue(Compare(ll1, ll2) == 0);  // contents
            Assert.IsTrue(Compare(ll1, ll3) < 0);
            Assert.IsTrue(Compare(ll3, ll1) > 0);
            Assert.IsTrue(IsEqual(ll1, ll2));
            Assert.IsFalse(IsEqual(ll1, ll3));

            Assert.IsTrue(Compare(ll1, ll1) == 0);    // reference 
            Assert.IsTrue(IsEqual(ll1, ll1));

            Assert.IsTrue(Compare(ll1, ll4) > 0);     // lengths
            Assert.IsTrue(Compare(ll4, ll1) < 0);
            Assert.IsFalse(IsEqual(ll4, ll1));
            Assert.IsFalse(IsEqual(ll1, ll4));
        }




        /// <summary>
        /// Simplistic method to compare two ILists
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        internal static int Compare<T>(IList<T> left, IList<T> right, IComparer<T> comparer = null)
        {
            if (ReferenceEquals(left, right))
                return 0;

            if (left is null)
                return -1;

            if (right is null)
                return 1;

            if (left.Count != right.Count)
                return (left.Count < right.Count) ? -1 : 1;

            // null friendly comparer
            IComparer<T> c = comparer ?? Comparer<T>.Default;

            for (int index = 0; index < left.Count; index++)
            {
                int cr = c.Compare(left[index], right[index]);

                if (cr != 0)
                    return cr;
            }

            return 0;
        }





        internal static int Compare<T>(List<List<T>> left, List<List<T>> right, IComparer<T> comparer = null)
        {
            if (ReferenceEquals(left, right))
                return 0;

            if (left is null)
                return -1;

            if (right is null)
                return 1;

            if (left.Count != right.Count)
                return (left.Count < right.Count) ? -1 : 1;

            for (int index = 0; index < left.Count; index++)
            {
                int cr = Compare(left[index], right[index], comparer);

                if (cr != 0)
                    return cr;
            }

            return 0;
        }


        internal static bool IsEqual<T>(List<List<T>> left, List<List<T>> right, IComparer<T> comparer = null)
        {
            return Compare(left, right, comparer) == 0;
        }


        internal static bool IsEqual<T>(IList<T> left, IList<T> right, IComparer<T> comparer = null)
        {
            return Compare(left, right, comparer) == 0;
        }
    }



    internal class ListComparerClass<T> : IComparer<IList<T>> 
    {
        private readonly IComparer<T> comparer;

        public ListComparerClass(IComparer<T> c)
        {
            comparer = c;
        }

        public int Compare(IList<T> left, IList<T> right)
        {
            return Utils.Compare<T>(left, right, comparer);
        }
    }



    /// <summary>
    /// simple class that doesn't implement IComarable
    /// </summary>
    internal class InvalidClass
    {
        public int Order { get; }

        public InvalidClass(int order)
        {
            Order = order;
        }
    }


    /// <summary>
    /// simple structure that doesn't implement IComarable
    /// </summary>
    internal struct InvalidStruct
    {
        public int Order { get; }

        public InvalidStruct(int order)
        {
            Order = order;
        }
    }



    /// <summary>
    /// a comparer that allows combinations or permutations of a 
    /// class that doesn't or cannot implement IComparable.
    /// </summary>
    internal class InvalidClassComparer : IComparer<InvalidClass>
    {
        public int Compare(InvalidClass left, InvalidClass right)
        {
            if (ReferenceEquals(left, right))
                return 0;

            if (left is null)
                return -1;

            if (right is null)
                return 1;

            return left.Order.CompareTo(right.Order);
        }
    }

    /// <summary>
    /// a comparer that allows combinations or permutations of a 
    /// structure that doesn't or cannot implement IComparable.
    /// </summary>
    internal class InvalidStructComparer : IComparer<InvalidStruct>
    {
        public int Compare(InvalidStruct left, InvalidStruct right)
        {
            return left.Order.CompareTo(right.Order);
        }
    }




    /// <summary>
    /// A class that generates a list of permutations. No attempt has been made to 
    /// optimize this class, its just a different algorithm used to verify 
    /// the results obtained from the permutation enumerator. The chances of 
    /// both algorithms having the same error is small and allows better tests.
    /// </summary>
    internal static class AltPerm
    {

        public static List<List<T>> Get<T>(IEnumerable<T> source, IComparer<T> comparer = null) 
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            if (source.Count() == 0)
                throw new ArgumentOutOfRangeException(nameof(source));

            List<List<T>> result = new List<List<T>>();

            T[] input = source.ToArray();
            Array.Sort(input, comparer);

            Permute(input, 0, input.Length - 1, result);

            // convert to lexicographical and remove duplicates 
            result.Sort(new ListComparerClass<T>(comparer));

            if (HasDuplicates(input))
                Utils.RemoveDuplicates(result, comparer);

            return result;
        }

        private static bool HasDuplicates<T>(T[] input)
        {
            return input.Distinct().Count() != input.Length;
        }

        private static void Swap<T>(ref T a, ref T b)
        {
            T tmp = a;
            a = b;
            b = tmp;
        }

        private static void Permute<T>(T[] source, int recursionDepth, int maxDepth, List<List<T>> result)
        {
            if (recursionDepth == maxDepth)
            {
                result.Add(new List<T>(source));
                return;
            }

            for (int i = recursionDepth; i <= maxDepth; i++)
            {
                Swap(ref source[recursionDepth], ref source[i]);
                Permute(source, recursionDepth + 1, maxDepth, result);
                // backtrack
                Swap(ref source[recursionDepth], ref source[i]);
            }
        }
    }


    /// <summary>
    /// A class that generates a list of combinations. No attempt has been made to 
    /// optimize this class, its just a different algorithm used to verify 
    /// the results obtained from the combination enumerator. The chances of 
    /// both algorithms having the same error is small and allows better tests.
    /// </summary>
    internal static class AltComb
    {

        public static List<List<T>> Get<T>(IEnumerable<T> source, int k, IComparer<T> comparer = null) 
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            if (source.Count() == 0)
                throw new ArgumentOutOfRangeException(nameof(source));

            if ((k < 1) || (k > source.Count()))
                throw new ArgumentOutOfRangeException(nameof(k));

            T[] input = source.ToArray();
            Array.Sort(input, comparer);

            List<List<T>> result = GetCombinations(input, k);

            // convert to lexicographical and remove duplicates
            result.Sort(new ListComparerClass<T>(comparer));

            if (HasDuplicates(input))
                Utils.RemoveDuplicates(result, comparer);

            return result;
        }


        private static bool HasDuplicates<T>(T[] input)
        {
            return input.Distinct().Count() != input.Length; 
        }


        private static List<List<T>> GetCombinations<T>(T[] source, int k)
        {
            List<List<T>> listOfLists = new List<List<T>>();

            if (k == 0)
                return listOfLists;

            int nonEmptyCombinations = (int)Math.Pow(2, source.Length) - 1;

            for (int i = 1; i <= nonEmptyCombinations; i++)
            {
                List<T> thisCombination = new List<T>();

                for (int j = 0; j < source.Length; j++)
                {
                    if ((i >> j & 1) == 1)
                        thisCombination.Add(source[j]);
                }

                if (thisCombination.Count == k)
                    listOfLists.Add(thisCombination);
            }

            return listOfLists;
        }
    }
}
