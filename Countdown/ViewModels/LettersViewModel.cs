using Countdown.Models;

namespace Countdown.ViewModels;

internal sealed class LettersViewModel : DataErrorInfoBase
{
    public const int max_vowels = 5;
    public const int max_consonants = 6;

    private double clearButtonPathOpacity = 1.0;

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

    // property backing stores
    private IEnumerable<string> wordList = new List<string>();

    public RelayCommand ClearCommand { get; }
    public RelayCommand PickVowelCommand { get; }
    public RelayCommand PickConsonantCommand { get; }
    public RelayTaskCommand SolveCommand { get; }
    public ICommand ChooseLettersCommand { get; }
    public ICommand ChooseOptionCommand { get; }
    public Model Model { get; }
    public StopwatchController StopwatchController { get; }
    public string? SuggestionText { get; set; }

    public LettersViewModel(Model model, StopwatchController sc) : base(propertyNames.Length)
    {
        Model = model;
        StopwatchController = sc;

        ChooseLettersCommand = new RelayCommand(ExecuteChooseLetters);
        SolveCommand = new RelayTaskCommand(ExecuteSolveAsync, CanSolve);
        ClearCommand = new RelayCommand(ExecuteClear, CanClear);
        PickVowelCommand = new RelayCommand(ExecutePickVowel, CanPickVowel);
        PickConsonantCommand = new RelayCommand(ExecutePickConsonant, CanPickConsonant);
        ChooseOptionCommand = new RelayCommand(ExecuteChooseOption);

        // initialise letters
        ChooseLettersCommand.Execute(null);
    }

    /// <summary>
    /// Letter properties. 
    /// The char type cannot an have empty value so use strings.
    /// </summary>
    public string Letter_0
    {
        get => Model.Letters[0];
        set => SetLetter(value, ref Model.Letters[0]);
    }

    public string Letter_1
    {
        get => Model.Letters[1];
        set => SetLetter(value, ref Model.Letters[1]);
    }

    public string Letter_2
    {
        get => Model.Letters[2];
        set => SetLetter(value, ref Model.Letters[2]);
    }

    public string Letter_3
    {
        get => Model.Letters[3];
        set => SetLetter(value, ref Model.Letters[3]);
    }

    public string Letter_4
    {
        get => Model.Letters[4];
        set => SetLetter(value, ref Model.Letters[4]);
    }

    public string Letter_5
    {
        get => Model.Letters[5];
        set => SetLetter(value, ref Model.Letters[5]);
    }

    public string Letter_6
    {
        get => Model.Letters[6];
        set => SetLetter(value, ref Model.Letters[6]);
    }

    public string Letter_7
    {
        get => Model.Letters[7];
        set => SetLetter(value, ref Model.Letters[7]);
    }

    public string Letter_8
    {
        get => Model.Letters[8];
        set => SetLetter(value, ref Model.Letters[8]);
    }


    private void SetLetter(string newValue, ref string existing, [CallerMemberName] string propertyName = "")
    {
        if (newValue != existing)
        {
            existing = newValue;
            ValidateLetters();
            RaisePropertyChanged(propertyName);
            UpdateCommandsExecuteStatus();
        }
    }

    private void ValidateLetters()
    {
        ClearAllErrors();

        int vowelCount = Model.Letters.Count(c => IsUpperVowel(c));

        if (vowelCount > max_vowels)
        {
            for (int index = 0; index < Model.Letters.Length; index++)
            {
                if (IsUpperVowel(Model.Letters[index]))
                    SetValidationError(index, $"A maximum of {max_vowels} vowels are allowed");
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
                        SetValidationError(index, $"A maximum of {max_consonants} consonants are allowed");
                }
            }
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
    /// Expose the list so it can be bound to by the ui 
    /// </summary>
    public IEnumerable<string> WordList
    {
        get => wordList;
        private set => HandlePropertyChanged(ref wordList, value);
    }

    private void ExecuteClear(object? _)
    {
        for (int index = 0; index < Model.cLetterCount; ++index)
        {
            if (!string.IsNullOrEmpty(Model.Letters[index]))
            {
                Model.Letters[index] = string.Empty;
                RaisePropertyChanged(propertyNames[index]);
                ClearValidationError(index);
            }
        }

        UpdateCommandsExecuteStatus();
    }

    private bool CanClear(object? _)
    {
        bool canClear = Model.Letters.Any(c => !string.IsNullOrEmpty(c));
        ClearButtonPathOpacity = canClear ? 1.0 : 0.3;
        return canClear;
    }

    public double ClearButtonPathOpacity
    {
        get => clearButtonPathOpacity;
        set => HandlePropertyChanged(ref clearButtonPathOpacity, value);
    }

    private void ExecutePickVowel(object? _)
    {
        SetFirstEmptyLetter(Model.GetVowel());
        UpdateCommandsExecuteStatus();
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

    private void UpdateCommandsExecuteStatus()
    {
        // updating all in one go avoids any logic errors
        ClearCommand.RaiseCanExecuteChanged();
        PickConsonantCommand.RaiseCanExecuteChanged();
        PickVowelCommand.RaiseCanExecuteChanged();
        SolveCommand.RaiseCanExecuteChanged();
    }

    private bool CanPickVowel(object? _)
    {
        int vowels = Model.Letters.Count(c => IsUpperVowel(c));
        return vowels < max_vowels && Model.Letters.Any(c => string.IsNullOrEmpty(c));
    }

    private void ExecutePickConsonant(object? _)
    {
        SetFirstEmptyLetter(Model.GetConsonant());
        UpdateCommandsExecuteStatus();
    }

    private bool CanPickConsonant(object? _)
    {
        int consonants = Model.Letters.Count(c => IsUpperConsonant(c));
        return consonants < max_consonants && Model.Letters.Any(c => string.IsNullOrEmpty(c));
    }

    private async Task ExecuteSolveAsync(object? _)
    {
        // copy the model data now, converting to lower case 
        // it could be changed before the task is run
        char[] letters = new char[Model.cLetterCount];

        for (int index = 0; index < Model.cLetterCount; ++index)
            letters[index] = (char)(Model.Letters[index][0] | 0x20); // to lower

        List<string> results = await Task.Run(() => Model.Solve(letters));
        WordList = results;
    }

    private bool CanSolve(object? _, bool isExecuting)
    {
        return !(HasErrors || isExecuting || Model.Letters.Any(c => string.IsNullOrEmpty(c)));
    }

    // which item in the choose letter menu is selected
    public int TileOptionIndex
    {
        get => Settings.Data.ChooseLettersIndex;
        set
        {
            Settings.Data.ChooseLettersIndex = value;
            ChooseLettersCommand.Execute(null);
        }
    }

    private void ExecuteChooseLetters(object? _)
    {
        int vowelCount = TileOptionIndex + 3;   // option zero is "3 vowels and 6 consonants"

        Model.GenerateLettersData(vowelCount);

        for (int index = 0; index < Model.cLetterCount; index++)
        {
            RaisePropertyChanged(propertyNames[index]);
            ClearValidationError(index);
        }

        UpdateCommandsExecuteStatus();
    }

    private void ExecuteChooseOption(object? p)
    {
        if (int.TryParse(p as string, out int value))
            TileOptionIndex = value;
        else
            TileOptionIndex = 0;
    }

    public bool IsTileOptionChecked(int option)
    {
        return TileOptionIndex == option;
    }
}