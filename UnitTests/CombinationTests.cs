using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Countdown.Models;
using System.Linq;

namespace Countdown.UnitTests
{
    [TestClass]
    public class CombinationTests
    {

        [ClassInitialize]
        public static void InitialiseClass(TestContext tc)
        {
            // test cases use the comparers so test them first
            Utils.TestComparers();
        }



        
        public void TestCombinations<T>(IEnumerable<T> source, IComparer<T> comparer = null) 
        {
            bool uniqueSource = source.Distinct().Count() == source.Count();

            for (int k = 1; k <= source.Count(); ++k)
            {
                var list = new List<T[]>(new Combinations<T>(source, k, comparer));
                List<T[]> altList = AltComb.Get(source, k, comparer);

                if (uniqueSource)
                    Assert.AreEqual(Utils.CombinationSize(source.Count(), k), list.Count) ;

                Assert.IsTrue(Utils.IsEqual(list, altList, comparer));
            }
        }



        [TestMethod] 
        public void Source_Unique()
        {
            int[] source = {1, 3, 2, 4, 6, 5, 7};
            TestCombinations(source);
        }




        [TestMethod]
        public void Source_One_Duplicates()
        {
            int[] source = { 1, 1, 4, 3, 5, 6, 7 };
            TestCombinations(source);
        }



        [TestMethod]
        public void Source_Two_Duplicates()
        {
            int[] source = { 1, 1, 3, 3, 6, 5, 7 };
            TestCombinations(source);
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Source_Empty()
        {
            // empty source
            int[] source = new int[0];
            Combinations<int> c1 = new Combinations<int>(source, 0);
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Source_Null()
        {
            Combinations<int> c2 = new Combinations<int>(null, 0);
        }



        [TestMethod]
        public void Source_Single()
        {
            int[] source = { 864 };

            var list = new List<int[]>(new Combinations<int>(source, 1));

            if ((list.Count != 1) || (list[0].Length != 1) || (list[0][0] != source[0]))
                Assert.Fail();
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void K_Greater_Than_N()
        {
            int[] source = { 1, 3, 2, 4 };
            Combinations<int> c = new Combinations<int>(source, source.Length + 1);
        }


        [TestMethod]
        public void K_Is_Zero()
        {
            int[] source = { 1, 3, 2, 4 };
            Combinations<int> c = new Combinations<int>(source, 0);

            // k == 0 is a valid input condition. The enumerator should 
            // return nothing
            foreach (int[] o in c)
            {
                Assert.Fail();
            }
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void K_Less_Than_Zero()
        {
            int[] source = { 1, 3, 2, 4 };
            Combinations<int> c = new Combinations<int>(source, -1);
        }



        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Enumerator_State_Start()
        {
            int[] source = { 1, 3, 2, 4 };
            Combinations<int> c = new Combinations<int>(source, 2);

            IEnumerator<int[]> e = c.GetEnumerator();

            // move next has not been called yet
            _ = e.Current;
        }
        


        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Enumerator_State_Reset()
        {
            int[] source = { 1, 3, 2, 4 };
            Combinations<int> c = new Combinations<int>(source, 2);

            IEnumerator<int[]> e = c.GetEnumerator();

            e.MoveNext();
            e.MoveNext();

            e.Reset();
          
            // move next has not been called yet
            _ = e.Current;
        }




        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Enumerator_State_End()
        {
            int[] source = { 1, 3, 2, 4 };
            Combinations<int> c = new Combinations<int>(source, 2);

            IEnumerator<int[]> e = c.GetEnumerator();

            while (e.MoveNext()) ;

            // end of the enumeration has been reached
            _ = e.Current;
        }

        
        [TestMethod]
        public void Class_Not_IComparable_Using_Custom_Comparer()
        {
            InvalidClass[] source = { new InvalidClass(0), new InvalidClass(1), new InvalidClass(2), new InvalidClass(4), new InvalidClass(3) };
            TestCombinations(source, new InvalidClassComparer());
        }


        [TestMethod]
        public void Struct_Not_IComparable_Using_Custom_Comparer()
        {
            InvalidStruct[] source = { new InvalidStruct(0), new InvalidStruct(1), new InvalidStruct(2), new InvalidStruct(4), new InvalidStruct(3) };
            TestCombinations(source, new InvalidStructComparer());
        }


        [TestMethod]
        public void Generics()
        {
            string[] source = { "A", "C", "B", "D", "E" };
            TestCombinations(source);
        }


        [TestMethod]
        public void Generics_Null_Item()
        {
            string[] source = { "A", "C", "B", null, "E" };
            TestCombinations(source);
        }


        [TestMethod]
        public void Nullables()
        {
            int?[] source = { 1, 2, 3, 4, 5 };
            TestCombinations(source);
        }


        [TestMethod]
        public void Nullables_Null_Item()
        {
            int?[] source = { 1, 2, 3, null, 5 };
            TestCombinations(source);
        }


        [TestMethod]
        public void Enumerated_Reset()
        {
            int[] source = { 1, 3, 2, 4 };
            Combinations<int> c = new Combinations<int>(source, 2);

            IEnumerator<int[]> e = c.GetEnumerator();

            e.MoveNext();
            int[] c1 = e.Current;
            e.MoveNext();

            e.Reset();
            e.MoveNext();

            Assert.IsTrue(Utils.IsEqual(c1, e.Current));
        }


        [TestMethod]
        public void Enumerated_Twice()
        {
            int[] source = { 1, 3, 2, 4 };

            Combinations<int> c1 = new Combinations<int>(source, 2);
            List<int[]> c1list = new List<int[]>(c1);
            List<int[]> c2list = new List<int[]>(c1);

            Assert.IsTrue(Utils.IsEqual(c1list, c2list));
        }
    }
}
