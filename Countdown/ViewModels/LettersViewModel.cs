using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

using Countdown.Models;

namespace Countdown.ViewModels
{
    internal sealed class LettersViewModel : DataErrorInfoBase
    {
        public const int max_vowels = 5;
        public const int max_consonants = 6;

        // which item in the letter option list is selected
        private int chooseLettersOption = 0;

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
        private WordItem scrollToItem;
        private List<WordItem> wordList;

        public ICommand ClearCommand { get; }
        public ICommand PickVowelCommand { get; }
        public ICommand PickConsonantCommand { get; }
        public ICommand ScrollToCommand { get; }
        public ICommand SolveCommand { get; }
        public ICommand ChooseLettersCommand { get; }
        public ICommand ListCopyCommand { get; }
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

            ListCopyCommand = new RelayCommand(ExecuteCopy, CanCopy);
            GoToDefinitionCommand = new RelayCommand(ExecuteGoToDefinition, CanGoToDefinition);

            // initialise letter menu selected item
            ChooseLettersOption = Settings.Default.PickLetterOption;
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
                int consonantCount = Model.Letters.Count(c => IsUpperConsonant(c));

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
        /// Bound to the contents of the search text box
        /// </summary>
        public string SearchText { get; set; }


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
                RaisePropertyChanged();
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
                RaisePropertyChanged();
            }
        }

        private bool WordListContains(string word)
        {
            if (word?.Length >= WordDictionary.cMinLetters)
                return WordList?.BinarySearch(new WordItem(word)) >= 0;

            return false;
        }

        private void ExecuteClear(object _)
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



        private bool CanClear(object _)
        {
            return Model.Letters.Any(c => !string.IsNullOrEmpty(c));
        }

        

        private void ExecutePickVowel(object _)
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


        private bool CanPickVowel(object _)
        {
            int vowels = Model.Letters.Count(c => IsUpperVowel(c));
            return vowels < max_vowels && Model.Letters.Any(c => string.IsNullOrEmpty(c));
        }

        

        private void ExecutePickConsonant(object _)
        {
            SetFirstEmptyLetter(Model.GetConsonant());
        }


        private bool CanPickConsonant(object _)
        {
            int consonants = Model.Letters.Count(c => IsUpperConsonant(c));
            return consonants < max_consonants && Model.Letters.Any(c => string.IsNullOrEmpty(c));
        }

        

        private void ExecuteScrollTo(object _)
        {
            if (WordList != null)
            {
                int index = WordList.BinarySearch(new WordItem(SearchText));

                if (index >= 0)
                {
                    // expand the group that the search word belongs too
                    int firstItem = WordList.BinarySearch(new WordItem(new string('a', SearchText.Length)));

                    if (firstItem < 0)
                    {
                        WordList[~firstItem].IsExpanded = true;

                        // select and scroll into view
                        ScrollToItem = WordList[index];
                    }
                }
            }
        }

        private bool CanScrollTo(object _) => WordListContains(SearchText);     


        private async Task ExecuteSolveAsync(object _)
        {
            // copy the model data now, converting to lower case 
            // it could be changed before the task is run
            char[] letters = new char[Model.cLetterCount];

            for (int index = 0; index < Model.cLetterCount; ++index)
                letters[index] = (char)(Model.Letters[index][0] | 0x20); // to lower

            List<WordItem> results = await Task.Run(() => Model.Solve(letters));

            if (results.Count > 0)
            {
                // sort, longer words first then alphabetically
                results.Sort();
                // expand the first group
                results[0].IsExpanded = true;
            }

            // update the ui
            WordList = results;
        }


        private bool CanSolve(object _, bool isExecuting)
        {
            return !(HasErrors || isExecuting || Model.Letters.Any(c => string.IsNullOrEmpty(c)));
        }

        // which item in the choose letter menu is selected
        public int ChooseLettersOption
        {
            get { return chooseLettersOption; }
            set
            {
                chooseLettersOption = value;
                RaisePropertyChanged();
                ChooseLettersCommand.Execute(null);
                Settings.Default.PickLetterOption = value;
            }
        }

        private void ExecuteChooseLetters(object _)
        {
            for (int index = 0; index < Model.cLetterCount; index++)
            {
                if (index < (ChooseLettersOption + LetterOptionsList.Count))
                    Model.Letters[index] = Model.GetVowel().ToString();
                else
                    Model.Letters[index] = Model.GetConsonant().ToString();

                RaisePropertyChanged(propertyNames[index]);
                ClearValidationError(propertyNames[index]);
            }
        }

        private void ExecuteCopy(object _)
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
                    Clipboard.SetText(sb.ToString());
            }
        }


        private bool CanCopy(object _)
        {
            return (WordList != null) && WordList.Any(e => e.IsSelected);
        }

        private void ExecuteGoToDefinition(object p)
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

        private bool CanGoToDefinition(object _) => WordList?.Count(e => e.IsSelected) is > 0 and < 11;
    }
}
