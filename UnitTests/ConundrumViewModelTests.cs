using Countdown.Models;
using Countdown.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Countdown.UnitTests
{


    [TestClass]
    public class ConundrumViewModelTests
    {
        private readonly ConundrumViewModel cvm = new ViewModel().ConundrumViewModel;


        [TestInitialize]
        public void Initialize()
        {
            // select so that any commands which access the 
            // word dictionary are valid
            cvm.IsSelected = true;
            cvm.ClipboadService = new UnitTestClipboard();
        }

        

        private char[] GetConundrum()
        {
            char[] conundrum = new char[Model.cLetterCount];

            conundrum[0] = cvm.Conundrum_0[0];
            conundrum[1] = cvm.Conundrum_1[0];
            conundrum[2] = cvm.Conundrum_2[0];
            conundrum[3] = cvm.Conundrum_3[0];
            conundrum[4] = cvm.Conundrum_4[0];
            conundrum[5] = cvm.Conundrum_5[0];
            conundrum[6] = cvm.Conundrum_6[0];
            conundrum[7] = cvm.Conundrum_7[0];
            conundrum[8] = cvm.Conundrum_8[0];

            return conundrum;
        }



        [TestMethod]
        public void Choose_ConundrumCommand()
        {
            Assert.IsTrue(cvm.ChooseCommand.CanExecute(null));

            cvm.ChooseCommand.Execute(null);

            Assert.IsTrue(GetConundrum().All(c => LetterTile.IsUpperLetter(c)));
        }

        




        [TestMethod]
        public void Solve_Command()
        {
            // pick conundrum
            Assert.IsTrue(cvm.ChooseCommand.CanExecute(null));
            cvm.ChooseCommand.Execute(null);

            // solve it
            Assert.IsTrue(cvm.SolveCommand.CanExecute(null));
            cvm.SolveCommand.Execute(null);

            // check result, as best as can be done
            char[] sol = cvm.Solution.ToCharArray();
            char[] con = GetConundrum();

            Array.Sort(sol);
            Array.Sort(con);

            Assert.IsTrue(Utils.IsEqual(sol, con));
        }


        [TestMethod]
        public void SelectAll_Command()
        {
            // can only execute if the list isn't null or empty
            Assert.IsFalse(cvm.ListSelectAllCommand.CanExecute(null));

            // add words
            for (int index = 0; index < 3; ++index)
            {
                cvm.ChooseCommand.Execute(null);
                cvm.SolveCommand.Execute(null);
            }

            // now it can execute do it
            Assert.IsTrue(cvm.ListSelectAllCommand.CanExecute(null));
            cvm.ListSelectAllCommand.Execute(null);

            // check all selected and command now disabled
            Assert.IsTrue(cvm.SolutionList.All(e => e.IsSelected));
            Assert.IsFalse(cvm.ListSelectAllCommand.CanExecute(null));

            // deselect one and check enabled
            cvm.SolutionList[0].IsSelected = false;
            Assert.IsTrue(cvm.ListSelectAllCommand.CanExecute(null));
        }



        [TestMethod]
        public void Copy_Command()
        {
            // can only execute if at least one item in the list is selected
            Assert.IsFalse(cvm.ListCopyCommand.CanExecute(null));

            // add words
            for (int index = 0; index < 3; ++index)
            {
                cvm.ChooseCommand.Execute(null);
                cvm.SolveCommand.Execute(null);
            }

            // list is full but nothing selected
            Assert.IsFalse(cvm.ListCopyCommand.CanExecute(null));

            // one item selected
            cvm.SolutionList[1].IsSelected = true;
            Assert.IsTrue(cvm.ListCopyCommand.CanExecute(null));

            // execute command
            cvm.ListCopyCommand.Execute(null);

            // check clipboard text
            Assert.IsTrue(cvm.ClipboadService.GetText().Equals(cvm.SolutionList[1].ToString()));
        }

        

        [TestMethod]
        public void StartTimer_Command()
        {
            Assert.IsFalse(cvm.StopwatchController.StopwatchRunning);

            // start the timer
            cvm.StopwatchController.StartStopTimerCommand.Execute(null);
            Assert.IsTrue(cvm.StopwatchController.StopwatchRunning);

            // stop the timer
            cvm.StopwatchController.StartStopTimerCommand.Execute(null);
            Assert.IsFalse(cvm.StopwatchController.StopwatchRunning);
        }
    }
}
