using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using Countdown.Models;

namespace Countdown.ViewModels
{
    internal sealed class LettersViewModel : DataErrorInfoBase
    {
        public const int max_vowels = 5;
        public const int max_consonants = 6;

        // which item in the letter option list is selected
        private int letterOptionIndex = 0;

        // allow the clipboard to be impersonated in unit tests
        public IClipboardService ClipboadService { get; set; } = new RealClipboard();

        // property names for change events when generating data and error notifications
        private static readonly string[] propertyNames = { nameof(Letter_0),
                                                            nameof(Letter_1),
                                                            nameof(Letter_2),
                                                            nameof(Letter_3),
                                                            nameof(Letter_4),
                                                            nameof(Letter_5),
                                                            nameof(Letter_6),
                                                            nameof(Letter_7),
                                                            nameof(Letter_8)};
        
        // list of letter choose options displayed in the ui
        public static List<string> LetterOptionsList { get; } = new List<string>
            {
                 "_3 vowels and 6 consonants",
                 "_4 vowels and 5 consonants",
                 "_5 vowels and 4 consonants"
            };

        // property backing stores
        private string scrollToText;
        private WordItem scrollToItem;
        private List<WordItem> wordList;
        private bool containsScrollToWord;
        private bool scrollToTextLengthValid;

        public ICommand ClearCommand { get; }
        public ICommand PickVowelCommand { get; }
        public ICommand PickConsonantCommand { get; }
        public ICommand ScrollToCommand { get; }
        public ICommand SolveCommand { get; }
        public ICommand ChooseLettersCommand { get; }
        public ICommand ListCopyCommand { get; }
        public ICommand ListSelectAllCommand { get; }
        public ICommand GoToDefinitionCommand { get; }
        public Model Model { get; }
        public StopwatchController StopwatchController { get; }


        public LettersViewModel(Model model, StopwatchController sc)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            StopwatchController = sc ?? throw new ArgumentNullException(nameof(sc));

            ChooseLettersCommand = new RelayCommand(ExecuteChooseLetters);
            SolveCommand = new RelayTaskCommand(ExecuteSolveAsync, CanSolve);
            ScrollToCommand = new RelayCommand(ExecuteScrollTo, CanScrollTo);

            ClearCommand = new RelayCommand(ExecuteClear, CanClear);
            PickVowelCommand = new RelayCommand(ExecutePickVowel, CanPickVowel);
            PickConsonantCommand = new RelayCommand(ExecutePickConsonant, CanPickConsonant);

            ListSelectAllCommand = new RelayCommand(ExecuteSelectAll, CanSelectAll);
            ListCopyCommand = new RelayCommand(ExecuteCopy, CanCopy);
            GoToDefinitionCommand = new RelayCommand(GoToDefinition, CanGoToDefinition);

            // initialise letter menu selected item
            LetterOptionIndex = Settings.Default.PickLetterOption;
        }



        /// <summary>
        /// Letter properties. 
        /// The char type cannot an have empty value so use strings.
        /// </summary>
        public string Letter_0
        {
            get { return Model.Letters[0]; }
            set { SetLetter(value, ref Model.Letters[0], nameof(Letter_0)); }
        }

        public string Letter_1
        {
            get { return Model.Letters[1]; }
            set { SetLetter(value, ref Model.Letters[1], nameof(Letter_1)); }
        }

        public string Letter_2
        {
            get { return Model.Letters[2]; }
            set { SetLetter(value, ref Model.Letters[2], nameof(Letter_2)); }
        }

        public string Letter_3
        {
            get { return Model.Letters[3]; }
            set { SetLetter(value, ref Model.Letters[3], nameof(Letter_3)); }
        }

        public string Letter_4
        {
            get { return Model.Letters[4]; }
            set { SetLetter(value, ref Model.Letters[4], nameof(Letter_4)); }
        }

        public string Letter_5
        {
            get { return Model.Letters[5]; }
            set { SetLetter(value, ref Model.Letters[5], nameof(Letter_5)); }
        }

        public string Letter_6
        {
            get { return Model.Letters[6]; }
            set { SetLetter(value, ref Model.Letters[6], nameof(Letter_6)); }
        }

        public string Letter_7
        {
            get { return Model.Letters[7]; }
            set { SetLetter(value, ref Model.Letters[7], nameof(Letter_7)); }
        }

        public string Letter_8
        {
            get { return Model.Letters[8]; }
            set { SetLetter(value, ref Model.Letters[8], nameof(Letter_8)); }
        }


        private void SetLetter(string newValue, ref string existing, string propertyName)
        {
            if (newValue != existing)
            {
                existing = newValue;
                ValidateLetters();
                RaisePropertyChanged(propertyName);
            }
        }



        private void ValidateLetters()
        {
            bool[] errors = new bool[Model.cLetterCount];

            int vowelCount = Model.Letters.Count(c => IsUpperVowel(c));

            if (vowelCount > max_vowels)
            {
                for (int index = 0; index < Model.Letters.Length; index++)
                {
                    if (IsUpperVowel(Model.Letters[index]))
                    {
                        SetValidationError(propertyNames[index], $"A maximum of {max_vowels} vowels are allowed");
                        errors[index] = true;
                    }
                }
            }
            else
            {
                int consonantCount = Model.Letters.Where(c => IsUpperConsonant(c)).Count();

                if (consonantCount > max_consonants)
                {
                    for (int index = 0; index < Model.Letters.Length; index++)
                    {
                        if (IsUpperConsonant(Model.Letters[index]))
                        {
                            SetValidationError(propertyNames[index], $"A maximum of {max_consonants} consonants are allowed");
                            errors[index] = true;
                        }
                    }
                }
            }

            // remove any preexisting error states on valid properties
            for (int index = 0; index < errors.Length; index++)
            {
                if (!errors[index])
                    ClearValidationError(propertyNames[index]);
            }
        }


        private static bool IsUpperVowel(string letter)
        {
            if (string.IsNullOrEmpty(letter))
                return false;

            return LetterTile.IsUpperVowel(letter[0]);
        }


        private static bool IsUpperConsonant(string letter)
        {
            if (string.IsNullOrEmpty(letter))
                return false;

            return LetterTile.IsUpperLetter(letter[0]) && !LetterTile.IsUpperVowel(letter[0]);
        }


        /// <summary>
        /// The contents of the scroll to text box
        /// </summary>
        public string ScrollToText
        {
            get { return scrollToText; }
            set
            {
                scrollToText = value;
                ScrollToTextLengthValid = (scrollToText != null) && (scrollToText.Length >= WordDictionary.cMinLetters);

                if (ScrollToTextLengthValid)
                    ContainsScrollToWord = WordListContains();
                else
                    ContainsScrollToWord = false;
            }
        }



        /// <summary>
        /// A property that the list view binds with. When this changes it 
        /// scrolls that item into view if necessary and selects it
        /// </summary>
        public WordItem ScrollToItem
        {
            get { return scrollToItem; }
            set
            {
                scrollToItem = value;
                RaisePropertyChanged(nameof(ScrollToItem));
            }
        } 
       


        /// <summary>
        /// Expose the list so it can be bound to by the ui 
        /// </summary>
        public List<WordItem> WordList
        {
            get { return wordList; }
            set
            {
                wordList = value;
                RaisePropertyChanged(nameof(WordList));
            }
        }

        

        public bool ContainsScrollToWord 
        {
            get { return containsScrollToWord; }
            set
            {
                containsScrollToWord = value;
                RaisePropertyChanged(nameof(ContainsScrollToWord));
            }
        }

        

        public bool ScrollToTextLengthValid
        {
            get { return scrollToTextLengthValid; }
            set
            {
                scrollToTextLengthValid = value;
                RaisePropertyChanged(nameof(ScrollToTextLengthValid));
            }
        }



        private bool WordListContains()
        {
            if ((WordList != null) && (ScrollToText != null) && (ScrollToText.Length >= WordDictionary.cMinLetters))
                return WordList.BinarySearch(new WordItem(ScrollToText)) >= 0;
            
            return false;
        }

        

        private void ExecuteClear(object p)
        {
            for (int index = 0; index < Model.cLetterCount; ++index)
            {
                if (!string.IsNullOrEmpty(Model.Letters[index]))
                {
                    Model.Letters[index] = string.Empty;
                    RaisePropertyChanged(propertyNames[index]);
                    ClearValidationError(propertyNames[index]);
                }
            }
        }



        private bool CanClear(object p)
        {
            return Model.Letters.Any(c => !string.IsNullOrEmpty(c));
        }

        

        private void ExecutePickVowel(object p)
        {
            SetFirstEmptyLetter(Model.GetVowel());
        }



        private void SetFirstEmptyLetter(char newValue)
        {
            for (int index = 0; index < Model.cLetterCount; ++index)
            {
                if (string.IsNullOrEmpty(Model.Letters[index]))
                {
                    Model.Letters[index] = newValue.ToString();
                    RaisePropertyChanged(propertyNames[index]);
                    break;
                }
            }
        }


        private bool CanPickVowel(object p)
        {
            int vowels = Model.Letters.Where(c => IsUpperVowel(c)).Count();
            return vowels < max_vowels && Model.Letters.Any(c => string.IsNullOrEmpty(c));
        }

        

        private void ExecutePickConsonant(object p)
        {
            SetFirstEmptyLetter(Model.GetConsonant());
        }


        private bool CanPickConsonant(object p)
        {
            int consonants = Model.Letters.Where(c => IsUpperConsonant(c)).Count();
            return consonants < max_consonants && Model.Letters.Any(c => string.IsNullOrEmpty(c));
        }

        

        private void ExecuteScrollTo(object p)
        {
            if (WordList != null)
            {
                int index = WordList.BinarySearch(new WordItem(ScrollToText));

                if (index >= 0)
                {
                    if (WordList[index] == ScrollToItem)
                        ScrollToItem = null; // force a property changed notification

                    // expand the group that the search word belongs too
                    int firstItem = WordList.BinarySearch(new WordItem(new string ('a', ScrollToText.Length)));

                    if (firstItem < 0)
                        WordList[~firstItem].IsExpanded = true;

                    // select and scroll into view
                    ScrollToItem = WordList[index];  
                }
            }
        }

        

        private bool CanScrollTo(object p)
        {
            return (WordList != null) && ContainsScrollToWord;
        }
        


        private async Task ExecuteSolveAsync(object p)
        {
            // copy the model data now converting to lower case, 
            // it could be changed before the task is run
            char[] letters = new char[Model.cLetterCount];

            for (int index = 0; index < Model.cLetterCount; ++index)
                letters[index] = (char)(Model.Letters[index][0] | 0x20); // to lower

            List<WordItem> viewData = await Task.Run(() => Model.Solve(letters));

            if (viewData.Count > 0)
            {
                // sort, longer words first then alphabetically
                viewData.Sort();
                // expand the first group
                viewData[0].IsExpanded = true;
            }

            // update the ui
            WordList = viewData;

            // reset the search text and thus its dependencies
            ScrollToText = ScrollToText;
        }


        private bool CanSolve(object p, bool isExecuting)
        {
            return !(HasErrors || isExecuting || Model.Letters.Any(c => string.IsNullOrEmpty(c)));
        }

             

        // which item in the letter option list is selected
        public int LetterOptionIndex
        {
            get { return letterOptionIndex; }
            set
            {
                if ((value < 0) && (value > LetterOptionsList.Count - 1))
                    value = 0;

                letterOptionIndex = value;
                RaisePropertyChanged(nameof(LetterOptionIndex));
                ChooseLettersCommand.Execute(null);
                Settings.Default.PickLetterOption = value;
            }
        }

        

        private void ExecuteChooseLetters(object p)
        {
            if ((LetterOptionIndex >= 0) && (LetterOptionIndex < LetterOptionsList.Count))
            {
                for (int index = 0; index < Model.cLetterCount; index++)
                {
                    if (index < (LetterOptionIndex + LetterOptionsList.Count))
                        Model.Letters[index] = Model.GetVowel().ToString();
                    else
                        Model.Letters[index] = Model.GetConsonant().ToString();

                    RaisePropertyChanged(propertyNames[index]);
                    ClearValidationError(propertyNames[index]);
                }
            }
        }



        private void ExecuteCopy(object p)
        {
            if (WordList != null)
            {
                StringBuilder sb = new StringBuilder();

                foreach (WordItem e in WordList)
                {
                    if (e.IsSelected)
                    {
                        if (sb.Length > 0)
                            sb.Append(Environment.NewLine);
                        
                        sb.Append(e.ToString());
                    }
                }

                if (sb.Length > 0)
                    ClipboadService.SetText(sb.ToString());
            }
        }


        private bool CanCopy(object p)
        {
            return (WordList != null) && WordList.Any(e => e.IsSelected);
        }




        private void ExecuteSelectAll(object p)
        {
            if (WordList != null)
            {
                foreach (WordItem e in WordList)
                {
                    // Always expand any groups. This guarantees the wpf ui items with in the group
                    // are created. If they don't exist it causes problems when items are deselected
                    // by clicking in the list, the bound model list item will not be deselected.
                    if (!e.IsExpanded)
                        e.IsExpanded = true;

                    if (!e.IsSelected)
                        e.IsSelected = true;
                }
            }
        }


        private bool CanSelectAll(object p)
        {
            return (WordList != null) && WordList.Any(e => !e.IsSelected);
        }

        private void GoToDefinition(object p)
        {
            try
            {
                foreach (WordItem e in WordList)
                {
                    if (e.IsSelected)
                    {
                        ProcessStartInfo psi = new()
                        {
                            UseShellExecute = true,
                            FileName = string.Format(CultureInfo.InvariantCulture, p.ToString(), e.Content),
                        };

                        _ = Process.Start(psi);
                    }
                }
            }
            catch
            {
                // fail silently...
            }
        }

        private bool CanGoToDefinition(object p) => WordList.Count(e => e.IsSelected) is > 0 and < 11;
    }
}
