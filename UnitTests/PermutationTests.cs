using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Countdown.Models;
using System.Collections.Generic;

namespace Countdown.UnitTests
{
    [TestClass]
    public class PermutationTests
    {


        [ClassInitialize]
        public static void InitialiseClass(TestContext tc)
        {
            // test cases use the comparers so test them first
            Utils.TestComparers();
        }

        public void TestPermutations<T>(IEnumerable<T> source, IComparer<T> comparer = null) 
        {
            var list = new List<List<T>>(new Permutations<T>(source, comparer));

            List<List<T>> altList = AltPerm.Get(source, comparer);

            Assert.AreEqual(Utils.PermutaionSize(source), list.Count);
            Assert.IsTrue(Utils.IsEqual(list, altList, comparer));
        }


        [TestMethod]
        public void Permutations_Including_Duplicates()
        {
            int[] source = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 25, 50, 75, 100 };

            for (int k = 2; k < 7; ++k)
            {
                Combinations<int> combinations = new Combinations<int>(source, k);

                foreach (IList<int> input in combinations)
                    TestPermutations(input);
            }
        }



        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Enumerator_State_Start()
        {
            int[] source = { 1, 3, 2, 4 };
            Permutations<int> p = new Permutations<int>(source);

            IEnumerator<List<int>> e = p.GetEnumerator();

            // move next has not been called yet
            List<int> current = e.Current;
        }



        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Enumerator_State_Reset()
        {
            int[] source = { 1, 3, 2, 4 };
            Permutations<int> p = new Permutations<int>(source);

            IEnumerator<List<int>> e = p.GetEnumerator();

            e.MoveNext();
            e.MoveNext();

            e.Reset();

            // move next has not been called yet
            List<int> current = e.Current;
        }




        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Enumerator_State_End()
        {
            int[] source = { 1, 3, 2, 4 };
            Permutations<int> p = new Permutations<int>(source);

            IEnumerator<List<int>> e = p.GetEnumerator();

            while (e.MoveNext()) ;

            // end of the enumeration has been reached
            List<int> current = e.Current;
        }

       

        [TestMethod]
        public void Class_Not_IComparable_Using_Custom_Comparer()
        {
            InvalidClass[] source = { new InvalidClass(0), new InvalidClass(1), new InvalidClass(2), new InvalidClass(4), new InvalidClass(3) };
            TestPermutations(source, new InvalidClassComparer());
        }


        [TestMethod]
        public void Struct_Not_IComparable_Using_Custom_Comparer()
        {
            InvalidStruct[] source = { new InvalidStruct(0), new InvalidStruct(1), new InvalidStruct(2), new InvalidStruct(4), new InvalidStruct(3) };
            TestPermutations(source, new InvalidStructComparer());
        }


        [TestMethod]
        public void Generics()
        {
            string[] source = { "A", "C", "B", "D", "E"};
            TestPermutations(source);
        }


        [TestMethod]
        public void Generics_Null_Item()
        {
            string[] source = { "A", "C", "B", null, "E" };
            TestPermutations(source);
        }


        [TestMethod]
        public void Nullables()
        {
            int?[] source = { 1, 2, 3, 4, 5 };
            TestPermutations(source);
        }


        [TestMethod]
        public void Nullables_Null_Item()
        {
            int?[] source = { 1, 2, 3, null, 5 };
            TestPermutations(source);
        }



        [TestMethod]
        public void Enumerated_Reset()
        {
            int[] source = { 1, 3, 2, 4 };
            Permutations<int> p = new Permutations<int>(source);

            IEnumerator<List<int>> e = p.GetEnumerator();

            e.MoveNext();
            List<int> c1 = e.Current;
            e.MoveNext();

            e.Reset();
            e.MoveNext();

            Assert.IsTrue(Utils.IsEqual(c1, e.Current));
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Source_Empty()
        {
            // empty source
            int[] source = new int[0];
            Permutations<int> p1 = new Permutations<int>(source);
        }



        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Source_Null()
        {
            Permutations<int> p2 = new Permutations<int>(null);
        }



        [TestMethod]
        public void Source_Single()
        {
            int[] source = { 367 };
            var list = new List<List<int>>(new Permutations<int>(source));

            if ((list.Count != 1) || (list[0].Count != 1) || (list[0][0] != source[0]))
                Assert.Fail();
        }




        [TestMethod]
        public void Enumerated_Twice()
        {
            int[] source = { 1, 3, 2 };
            Permutations<int> p1 = new Permutations<int>(source);

            List<List<int>> p1list = new List<List<int>>(p1);
            List<List<int>> p2list = new List<List<int>>(p1);

            Assert.IsTrue(Utils.IsEqual(p1list, p2list));
        }
    }
}
