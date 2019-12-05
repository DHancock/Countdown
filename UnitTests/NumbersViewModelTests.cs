using Microsoft.VisualStudio.TestTools.UnitTesting;
using Countdown.ViewModels;
using Countdown.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Countdown.UnitTests
{

    [TestClass]
    public class NumbersViewModelTests
    {
        private readonly static string[] propertyNames = { nameof(NumbersViewModel.Tile_A),
                                                            nameof(NumbersViewModel.Tile_B),
                                                            nameof(NumbersViewModel.Tile_C),
                                                            nameof(NumbersViewModel.Tile_D),
                                                            nameof(NumbersViewModel.Tile_E),
                                                            nameof(NumbersViewModel.Tile_F) };


        private readonly NumbersViewModel nvm = new ViewModel().NumbersViewModel;


        [TestInitialize]
        public void Initialize()
        {
            nvm.ClipboadService = new UnitTestClipboard();
        }


        private void SetTiles(int a, int b, int c, int d, int e, int f)
        {
            nvm.Tile_A = a.ToString();
            nvm.Tile_B = b.ToString();
            nvm.Tile_C = c.ToString();
            nvm.Tile_D = d.ToString();
            nvm.Tile_E = e.ToString();
            nvm.Tile_F = f.ToString();
        }


        private void SetTiles(IList<int> t)
        {
            if ((t is null) || (t.Count != 6))
                Assert.Fail("test data invalid");

            SetTiles(t[0], t[1], t[2], t[3], t[4], t[5]);
        }


        private void SetTarget(int target)
        {
            nvm.Target = target.ToString();
        }


      

        [TestMethod]
        public void Properties_Get_Set()
        {
            nvm.Tile_A = "1";
            nvm.Tile_B = "2";
            nvm.Tile_C = "3";
            nvm.Tile_D = "4";
            nvm.Tile_E = "5";
            nvm.Tile_F = "6";

            Assert.AreEqual(nvm.Tile_A, "1");
            Assert.AreEqual(nvm.Tile_B, "2");
            Assert.AreEqual(nvm.Tile_C, "3");
            Assert.AreEqual(nvm.Tile_D, "4");
            Assert.AreEqual(nvm.Tile_E, "5");
            Assert.AreEqual(nvm.Tile_F, "6");

            nvm.Target = "567";
            Assert.AreEqual(nvm.Target, "567");
        }


        [TestMethod]
        public void Properties_Empty_String()
        {
            // test the convert methods that convert string to int to load into the model
            // and visa versa. The built in binding converters throw format exceptions when 
            // presented with empty strings.

            nvm.Tile_A = nvm.Tile_B = nvm.Tile_C = nvm.Tile_D = nvm.Tile_E = nvm.Tile_F = "1";
            nvm.Tile_A = nvm.Tile_B = nvm.Tile_C = nvm.Tile_D = nvm.Tile_E = nvm.Tile_F = string.Empty;

            Assert.AreEqual(nvm.Tile_A, string.Empty);
            Assert.AreEqual(nvm.Tile_B, string.Empty);
            Assert.AreEqual(nvm.Tile_C, string.Empty);
            Assert.AreEqual(nvm.Tile_D, string.Empty);
            Assert.AreEqual(nvm.Tile_E, string.Empty);
            Assert.AreEqual(nvm.Tile_F, string.Empty);

            nvm.Target = "765";
            nvm.Target = string.Empty;
            Assert.AreEqual(nvm.Target, string.Empty);
        }



        [TestMethod]
        public void Valid_Tiles()
        {
            // all permutations of any 6 tiles from this array will be valid
            int[] validTiles = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 25, 50, 75, 100 };

            Combinations<int> combinations = new Combinations<int>(validTiles, 6);

            foreach (IList<int> combination in combinations)
            {
                Permutations<int> permutaions = new Permutations<int>(combination);

                foreach (IList<int> permutaion in permutaions)
                {
                    SetTiles(permutaion);
                    Assert.IsFalse(nvm.HasErrors);
                }
            }
        }





        private void TestInvalidTiles(int[] tiles, int invalidTileValue)
        {
            if ((tiles is null) || (tiles.Length != 6))
                Assert.Fail("test data invalid");

            Permutations<int> permutations = new Permutations<int>(tiles);

            foreach (IList<int> permutation in permutations)
            {
                SetTiles(permutation);
                Assert.IsTrue(nvm.HasErrors);

                for (int index = 0; index < permutation.Count; index++)
                {
                    if (permutation[index] == invalidTileValue)
                        Assert.IsNotNull(nvm.GetErrors(propertyNames[index]));
                    else
                        Assert.IsNull(nvm.GetErrors(propertyNames[index]));
                }
            }
        }


        [TestMethod]
        public void Invalid_Large_Tiles()
        {
            // more than one large tile value
            int[] tiles = { 25, 25, 1, 2, 3, 4 };
            TestInvalidTiles(tiles, tiles[0]);

            tiles[0] = tiles[1] = 50;
            TestInvalidTiles(tiles, tiles[0]);

            tiles[0] = tiles[1] = 75;
            TestInvalidTiles(tiles, tiles[0]);

            tiles[0] = tiles[1] = 100;
            TestInvalidTiles(tiles, tiles[0]);
        }


        [TestMethod]
        public void Invalid_Small_Tiles()
        {
            // more than two small tile values
            int[] tiles = { 0, 0, 0, 25, 75, 100 };

            for (int index = 1; index <= 10; ++index)
            {
                tiles[0] = tiles[1] = tiles[2] = index;
                TestInvalidTiles(tiles, tiles[0]);
            }
        }


        [TestMethod]
        public void Valid_Target()
        {
            Assert.IsFalse(nvm.HasErrors);

            for (int target = 0; target <= 1000; ++target)
            {
                nvm.Target = target.ToString();

                if ((target < Model.cMinTarget) || (target > Model.cMaxTarget))
                {
                    Assert.IsTrue(nvm.HasErrors);
                    Assert.IsNotNull(nvm.GetErrors(nameof(nvm.Target)));
                }
                else
                    Assert.IsFalse(nvm.HasErrors);
            }
        }

        
        [TestMethod]
        public void Choose_Command()
        {
            for (int tileOption = 0; tileOption < NumbersViewModel.TileOptionsList.Count; ++tileOption)
            {
                nvm.TileOptionIndex = tileOption;

                // generates random data so repeat...
                for (int index = 0; index < 10000; ++index)
                {
                    nvm.ChooseNumbersCommand.Execute(null);

                    // check valid tiles picked
                    Assert.IsFalse(nvm.HasErrors);

                    int largeTileCount = 0;

                    if (int.Parse(nvm.Tile_A) > 10)
                        ++largeTileCount;

                    if (int.Parse(nvm.Tile_B) > 10)
                        ++largeTileCount;

                    if (int.Parse(nvm.Tile_C) > 10)
                        ++largeTileCount;

                    if (int.Parse(nvm.Tile_D) > 10)
                        ++largeTileCount;

                    if (int.Parse(nvm.Tile_E) > 10)
                        ++largeTileCount;

                    if (int.Parse(nvm.Tile_F) > 10)
                        ++largeTileCount;

                    // check large and therefore small tile counts
                    Assert.AreEqual(largeTileCount, tileOption);
                }
            }
        }


        [TestMethod]
        public async Task Solve_Command()
        {
            SetTarget(101);
            SetTiles(100, 2, 3, 4, 5, 5);

            // executable if no validation errors
            Assert.IsTrue(nvm.SolveCommand.CanExecute(null));

            // trivial test
            await ((RelayTaskCommand)nvm.SolveCommand).Execute(null);
            Assert.IsNotNull(nvm.EquationList.Find((i) => i.Content.Equals("100 + (5 ÷ 5)")));

            // set invalid target value and check command is disabled
            SetTarget(3);
            Assert.IsFalse(nvm.SolveCommand.CanExecute(null));
        }


        [TestMethod]
        public void SelectAll_Command()
        {
            // can only execute if the list isn't null or empty
            Assert.IsFalse(nvm.ListSelectAllCommand.CanExecute(null));

            // fill the equation list
            nvm.EquationList = new List<EquationItem> { new EquationItem("1+2+3+4"),
                                                        new EquationItem("2+3+4+5"),
                                                        new EquationItem("3+4+5+6") };

            // now it can execute do it
            Assert.IsTrue(nvm.ListSelectAllCommand.CanExecute(null));
            nvm.ListSelectAllCommand.Execute(null);

            // check all selected and command now disabled
            Assert.IsTrue(nvm.EquationList.All(e => e.IsSelected));
            Assert.IsFalse(nvm.ListSelectAllCommand.CanExecute(null));
             
            // deselect one and check enabled
            nvm.EquationList[0].IsSelected = false;
            Assert.IsTrue(nvm.ListSelectAllCommand.CanExecute(null));
        }
        


        [TestMethod]
        public void Copy_Command()
        {
            // can only execute if at least one item in the list is selected
            Assert.IsFalse(nvm.ListCopyCommand.CanExecute(null));

            // fill the equation list
            nvm.EquationList = new List<EquationItem> { new EquationItem("1+2+3+4"),
                                                        new EquationItem("2+3+4+5"),
                                                        new EquationItem("3+4+5+6") };

            // list is full but nothing selected
            Assert.IsFalse(nvm.ListCopyCommand.CanExecute(null));

            // one item selected
            nvm.EquationList[1].IsSelected = true;
            Assert.IsTrue(nvm.ListCopyCommand.CanExecute(null));

            // execute command
            nvm.ListCopyCommand.Execute(null);

            // check clipboard text
            Assert.IsTrue(nvm.ClipboadService.GetText().Equals(nvm.EquationList[1].Content));
        }



        [TestMethod]
        public void StartTimer_Command()
        {
            Assert.IsFalse(nvm.StopwatchController.StopwatchRunning);

            // start the timer
            nvm.StopwatchController.StartStopTimerCommand.Execute(null);
            Assert.IsTrue(nvm.StopwatchController.StopwatchRunning);

            // stop the timer
            nvm.StopwatchController.StartStopTimerCommand.Execute(null);
            Assert.IsFalse(nvm.StopwatchController.StopwatchRunning);
        }
    }
}