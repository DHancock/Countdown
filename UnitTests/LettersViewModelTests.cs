using Countdown.Models;
using Countdown.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Countdown.UnitTests
{


    [TestClass]
    public class LettersViewModelTests
    {
        private readonly LettersViewModel lvm = new ViewModel().LettersViewModel;



        [TestInitialize]
        public void Initialize()
        {
            lvm.ClipboadService = new UnitTestClipboard();
        }


        private void SetTiles(string[] data)
        {
            lvm.Letter_0 = data[0];
            lvm.Letter_1 = data[1];
            lvm.Letter_2 = data[2];
            lvm.Letter_3 = data[3];
            lvm.Letter_4 = data[4];
            lvm.Letter_5 = data[5];
            lvm.Letter_6 = data[6];
            lvm.Letter_7 = data[7];
            lvm.Letter_8 = data[8];
        }



        [TestMethod]
        public void Properties_Get_Set()
        {
            string[] data = { "A", "B", "C", "D", "E", "F", "G", "H", "I" };

            SetTiles(data);

            Assert.AreEqual(lvm.Letter_0, data[0]);
            Assert.AreEqual(lvm.Letter_1, data[1]);
            Assert.AreEqual(lvm.Letter_2, data[2]);
            Assert.AreEqual(lvm.Letter_3, data[3]);
            Assert.AreEqual(lvm.Letter_4, data[4]);
            Assert.AreEqual(lvm.Letter_5, data[5]);
            Assert.AreEqual(lvm.Letter_6, data[6]);
            Assert.AreEqual(lvm.Letter_7, data[7]);
            Assert.AreEqual(lvm.Letter_8, data[8]);
        }
        


        [TestMethod]
        public void Valid_Tiles()
        {
            const int min_vowels = Model.cLetterCount - LettersViewModel.max_consonants;

            List<string> letters = new List<string>(Model.cLetterCount);

            for (int vowelCount = min_vowels; vowelCount <= LettersViewModel.max_vowels; ++vowelCount)
            {
                letters.Clear();

                int vowels = vowelCount;
                int consonants = Model.cLetterCount - vowelCount;

                while (vowels-- > 0)
                    letters.Add(lvm.Model.GetVowel().ToString());

                while (consonants-- > 0)
                    letters.Add(lvm.Model.GetConsonant().ToString());

                Permutations<string> permutaions = new Permutations<string>(letters);

                foreach (List<string> permutaion in permutaions)
                {
                    SetTiles(permutaion.ToArray());
                    Assert.IsFalse(lvm.HasErrors);
                }
            }
        }



        private void LetterFrequencyTest(char[] letters)
        {
            const int cVowelCount = 5;
            const int cLoopCount = 200000; // per letter
            const double cAcceptableError = 0.5; // percent
            
            ConcurrentDictionary<char, long> frequencies = new ConcurrentDictionary<char, long>();

            // initialise the dictionary contents 
            foreach (char c in letters)
                frequencies.TryAdd(c, 0);

            Func<char> GetLetter;

            if (letters.Length == cVowelCount)
                GetLetter = () => lvm.Model.GetVowel();
            else
                GetLetter = () => lvm.Model.GetConsonant();

            // choose and record counts 
            Parallel.For(0, cLoopCount * letters.Length, (x) => frequencies[GetLetter()] += 1);

            // sum the settings frequencies to allow percentage comparisons
            double sum = 0;

            if (letters.Length == cVowelCount)
                sum = UserSettings.Vowels.Sum(lt => lt.Frequency);
            else
                sum = UserSettings.Consonants.Sum(lt => lt.Frequency);

            // compare settings and actual frequency percentages
            for (int index = 0; index < letters.Length; ++index)
            {
                double actualPercentage = (100.0 / (cLoopCount * letters.Length)) * frequencies[letters[index]];
                double settingsPercentage = (100.0 / sum);

                if (letters.Length == cVowelCount)
                    settingsPercentage *= UserSettings.Vowels[index].Frequency;
                else
                    settingsPercentage *= UserSettings.Consonants[index].Frequency;

                // assumes that enough random choices have been made for the 
                // frequency test to approach the required accuracy
                Assert.IsTrue(Math.Abs(actualPercentage - settingsPercentage) <= cAcceptableError);
                //System.Diagnostics.Debug.WriteLine($"{letters[index]}:{Math.Abs(actualPercentage - settingsPercentage)}");
            }
        }



        [TestMethod]
        public void Vowel_Frequency()
        {
            char[] set = { 'A', 'E', 'I', 'O', 'U' };
            LetterFrequencyTest(set);
        }



        [TestMethod]
        public void Consonant_Frequency()
        {
            char[] set = { 'B', 'C', 'D', 'F', 'G', 'H', 'J', 'K', 'L', 'M', 'N', 'P', 'Q', 'R', 'S', 'T', 'V', 'W', 'X', 'Y', 'Z' };
            LetterFrequencyTest(set);
        }



        [TestMethod]
        public void Pick_ConsonantCommand()
        {
            // max 6 consonants allowed
            string[] data = { "", "", "", "", "", "", "", "", "" };
            SetTiles(data);

            int count = 0;

            while (lvm.PickConsonantCommand.CanExecute(null) && (count < Model.cLetterCount))
            {
                lvm.PickConsonantCommand.Execute(null);
                ++count;
            }

            Assert.IsTrue(count == LettersViewModel.max_consonants);

            // check it becomes valid again
            lvm.Letter_0 = "";
            Assert.IsTrue(lvm.PickConsonantCommand.CanExecute(null));
        }



        [TestMethod]
        public void Pick_VowelCommand()
        {
            // max 5 vowels allowed
            string[] data = { "", "", "", "", "", "", "", "", "" };
            SetTiles(data);

            int count = 0;

            while (lvm.PickVowelCommand.CanExecute(null) && (count < Model.cLetterCount))
            {
                lvm.PickVowelCommand.Execute(null);
                ++count;
            }

            Assert.IsTrue(count == LettersViewModel.max_vowels);

            // check it becomes valid again
            lvm.Letter_0 = "";
            Assert.IsTrue(lvm.PickVowelCommand.CanExecute(null));
        }



        [TestMethod]
        public void ClearCommand()
        {
            string[] data = { "", "", "", "", "", "", "", "", "" };
            SetTiles(data);

            // check initial state, command disabled
            Assert.IsFalse(lvm.ClearCommand.CanExecute(null));

            for (int index = 0; index < Model.cLetterCount; ++index)
            {
                // add letter
                data[index] = "Z";
                // clear previous
                if (index > 0)
                    data[index - 1] = string.Empty;

                SetTiles(data);

                // check command enabled
                Assert.IsTrue(lvm.ClearCommand.CanExecute(null));

                // execute command and check results
                lvm.ClearCommand.Execute(null);
                Assert.IsFalse(lvm.ClearCommand.CanExecute(null));
                Assert.IsTrue(lvm.Model.Letters.All(s => s == string.Empty));
            }
        }



        [TestMethod]
        public async Task Solve_Command()
        {
            string[] data = { "K", "R", "A", "V", "S", "D", "R", "A", "A" };
            SetTiles(data);

            // executable if no validation errors
            Assert.IsTrue(lvm.SolveCommand.CanExecute(null));

            // trivial test but assumes word list contains...
            await ((RelayTaskCommand)lvm.SolveCommand).Execute(null);

            Assert.IsNotNull(lvm.WordList.Find((i) => i.Content.Equals("aardvarks")));
            Assert.IsNotNull(lvm.WordList.Find((i) => i.Content.Equals("dark")));

            // set invalid value and check command is disabled
            lvm.Letter_0 = string.Empty;
            Assert.IsFalse(lvm.SolveCommand.CanExecute(null));
        }


        [TestMethod]
        public void SelectAll_Command()
        {
            // can only execute if the list isn't null or empty
            Assert.IsFalse(lvm.ListSelectAllCommand.CanExecute(null));

            // add words
            lvm.WordList = new List<WordItem> { new WordItem("three"),
                                                new WordItem("two"),
                                                new WordItem("one") };

            // now it can execute do it
            Assert.IsTrue(lvm.ListSelectAllCommand.CanExecute(null));
            lvm.ListSelectAllCommand.Execute(null);

            // check all selected and command now disabled
            Assert.IsTrue(lvm.WordList.All(e => e.IsSelected));
            Assert.IsFalse(lvm.ListSelectAllCommand.CanExecute(null));

            // deselect one and check enabled
            lvm.WordList[0].IsSelected = false;
            Assert.IsTrue(lvm.ListSelectAllCommand.CanExecute(null));
        }



        [TestMethod]
        public void Copy_Command()
        {
            // can only execute if at least one item in the list is selected
            Assert.IsFalse(lvm.ListCopyCommand.CanExecute(null));

            // add words
            lvm.WordList = new List<WordItem> { new WordItem("three"),
                                                new WordItem("two"),
                                                new WordItem("one") };

            // list is full but nothing selected
            Assert.IsFalse(lvm.ListCopyCommand.CanExecute(null));

            // one item selected
            lvm.WordList[1].IsSelected = true;
            Assert.IsTrue(lvm.ListCopyCommand.CanExecute(null));

            // execute command
            lvm.ListCopyCommand.Execute(null);

            // check clipboard text
            Assert.IsTrue(lvm.ClipboadService.GetText().Equals(lvm.WordList[1].Content));
        }



        [TestMethod]
        public void Search_Command()
        {
            // can only execute if at least one item in the list
            Assert.IsFalse(lvm.ScrollToCommand.CanExecute(null));

            // add words
            lvm.WordList = new List<WordItem> { new WordItem("three"),
                                                new WordItem("two"),
                                                new WordItem("one") };

            // set valid search item
            lvm.ScrollToText = lvm.WordList[1].Content;

            lvm.ScrollToCommand.Execute(null);

            // its now expanded at least (selection happens in the views ListView which
            // doesn't exist, its then data bound back to the view models list item)
            Assert.IsTrue(lvm.WordList[1].IsExpanded);

            // invalid search item
            lvm.ScrollToText = "four";
            Assert.IsFalse(lvm.ScrollToCommand.CanExecute(null));
        }



        [TestMethod]
        public void StartTimer_Command()
        {
            Assert.IsFalse(lvm.StopwatchController.StopwatchRunning);

            // start the timer
            lvm.StopwatchController.StartStopTimerCommand.Execute(null);
            Assert.IsTrue(lvm.StopwatchController.StopwatchRunning);

            // stop the timer
            lvm.StopwatchController.StartStopTimerCommand.Execute(null);
            Assert.IsFalse(lvm.StopwatchController.StopwatchRunning);
        }
    }
}
