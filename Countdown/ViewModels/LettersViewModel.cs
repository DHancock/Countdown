using Countdown.Models;

namespace Countdown.ViewModels;

internal sealed class LettersViewModel : DataErrorInfoBase
{
    private const int cMaxVowels = 5;
    private const int cMaxConsonants = 6;

    private double clearButtonPathOpacity = 1.0;

    // for raising property change events when generating data
    private static readonly string[] propertyNames = { nameof(Letter_0),
                                                        nameof(Letter_1),
                                                        nameof(Letter_2),
                                                        nameof(Letter_3),
                                                        nameof(Letter_4),
                                                        nameof(Letter_5),
                                                        nameof(Letter_6),
                                                        nameof(Letter_7),
                                                        nameof(Letter_8)};

    private readonly string[] letters = new string[WordModel.cLetterCount];

    private readonly WordModel wordModel;

    private IEnumerable<string> wordList = new List<string>();
    public RelayCommand ClearCommand { get; }
    public RelayCommand PickVowelCommand { get; }
    public RelayCommand PickConsonantCommand { get; }
    public RelayTaskCommand SolveCommand { get; }
    public ICommand ChooseLettersCommand { get; }
    public ICommand ChooseOptionCommand { get; }
    public StopwatchController StopwatchController { get; }


    public LettersViewModel(WordModel model, StopwatchController sc) : base(WordModel.cLetterCount)
    {
        wordModel = model;
        StopwatchController = sc;

        ChooseLettersCommand = new RelayCommand(ExecuteChooseLetters);
        SolveCommand = new RelayTaskCommand(ExecuteSolveAsync, CanSolve);
        ClearCommand = new RelayCommand(ExecuteClear, CanClear);
        PickVowelCommand = new RelayCommand(ExecutePickVowel, CanPickVowel);
        PickConsonantCommand = new RelayCommand(ExecutePickConsonant, CanPickConsonant);
        ChooseOptionCommand = new RelayCommand(ExecuteChooseLettersMenuOption);

        // initialise letters
        ChooseLettersCommand.Execute(null);
    }

    /// <summary>
    /// Letter properties. 
    /// The char type cannot an have empty value so use strings.
    /// </summary>
    public string Letter_0
    {
        get => letters[0];
        set
        {
            letters[0] = value;
            UpdateProperties();
        }
    }

    public string Letter_1
    {
        get => letters[1];
        set
        {
            letters[1] = value;
            UpdateProperties();
        }
    }

    public string Letter_2
    {
        get => letters[2];
        set
        {
            letters[2] = value;
            UpdateProperties();
        }
    }

    public string Letter_3
    {
        get => letters[3];
        set
        {
            letters[3] = value;
            UpdateProperties();
        }
    }

    public string Letter_4
    {
        get => letters[4];
        set
        {
            letters[4] = value;
            UpdateProperties();
        }
    }

    public string Letter_5
    {
        get => letters[5];
        set
        {
            letters[5] = value;
            UpdateProperties();
        }
    }

    public string Letter_6
    {
        get => letters[6];
        set
        {
            letters[6] = value;
            UpdateProperties();
        }
    }

    public string Letter_7
    {
        get => letters[7];
        set
        {
            letters[7] = value;
            UpdateProperties();
        }
    }

    public string Letter_8
    {
        get => letters[8];
        set
        {
            letters[8] = value;
            UpdateProperties();
        }
    }

    private void UpdateProperties([CallerMemberName] string? propertyName = default)
    {
        ValidateLetters();

        if (WordList.Any())
            WordList = new List<string>();

        RaisePropertyChanged(propertyName);
        UpdateCommandsExecuteStatus();
    }

    private void ValidateLetters()
    {
        ClearAllErrors();

        int vowelCount = letters.Count(c => IsUpperVowel(c));

        if (vowelCount > cMaxVowels)
        {
            for (int index = 0; index < letters.Length; index++)
            {
                if (IsUpperVowel(letters[index]))
                    SetValidationError(index, $"A maximum of {cMaxVowels} vowels are allowed");
            }
        }
        else
        {
            int consonantCount = letters.Count(c => IsUpperConsonant(c));

            if (consonantCount > cMaxConsonants)
            {
                for (int index = 0; index < letters.Length; index++)
                {
                    if (IsUpperConsonant(letters[index]))
                        SetValidationError(index, $"A maximum of {cMaxConsonants} consonants are allowed");
                }
            }
        }
    }

    private static bool IsUpperVowel(string letter)
    {
        if (string.IsNullOrEmpty(letter))
            return false;

        return IsUpperVowel(letter[0]);
    }

    private static bool IsUpperConsonant(string letter)
    {
        if (string.IsNullOrEmpty(letter))
            return false;

        return IsUpperLetter(letter[0]) && !IsUpperVowel(letter[0]);
    }

    private static bool IsUpperVowel(char c) => c is 'E' or 'A' or 'I' or 'O' or 'U';
        
    private static bool IsUpperLetter(char c) => c is >= 'A' and <= 'Z';  
    
    // Bound to by the ui 
    public IEnumerable<string> WordList
    {
        get => wordList;
        private set
        {
            wordList = value;
            RaisePropertyChanged();
        }
    }

    private void ExecuteClear(object? _)
    {
        // set the backing store directly, the letters don't need validating
        for (int index = 0; index < WordModel.cLetterCount; ++index)
        {
            letters[index] = string.Empty;
            RaisePropertyChanged(propertyNames[index]);
            ClearValidationError(index);
        }

        if (WordList.Any())
            WordList = new List<string>();

        UpdateCommandsExecuteStatus();
    }

    private bool CanClear(object? _)
    {
        bool canClear = letters.Any(c => !string.IsNullOrEmpty(c));
        ClearButtonPathOpacity = canClear ? 1.0 : 0.3;
        return canClear;
    }

    public double ClearButtonPathOpacity
    {
        get => clearButtonPathOpacity;
        private set
        {
            clearButtonPathOpacity = value;
            RaisePropertyChanged();
        }
    }

    private void ExecutePickVowel(object? _)
    {
        SetFirstEmptyLetter(wordModel.GetVowel());
        UpdateCommandsExecuteStatus();
    }

    private void SetFirstEmptyLetter(char newValue)
    {
        for (int index = 0; index < WordModel.cLetterCount; ++index)
        {
            if (string.IsNullOrEmpty(letters[index]))
            {
                letters[index] = newValue.ToString();
                RaisePropertyChanged(propertyNames[index]);
                break;
            }
        }
    }

    private void UpdateCommandsExecuteStatus()
    {
        ClearCommand.RaiseCanExecuteChanged();
        PickConsonantCommand.RaiseCanExecuteChanged();
        PickVowelCommand.RaiseCanExecuteChanged();
        SolveCommand.RaiseCanExecuteChanged();
    }

    private bool CanPickVowel(object? _)
    {
        int vowels = letters.Count(c => IsUpperVowel(c));
        return vowels < cMaxVowels && letters.Any(c => string.IsNullOrEmpty(c));
    }

    private void ExecutePickConsonant(object? _)
    {
        SetFirstEmptyLetter(wordModel.GetConsonant());
        UpdateCommandsExecuteStatus();
    }

    private bool CanPickConsonant(object? _)
    {
        int consonants = letters.Count(c => IsUpperConsonant(c));
        return consonants < cMaxConsonants && letters.Any(c => string.IsNullOrEmpty(c));
    }

    private async Task ExecuteSolveAsync(object? _)
    {
        char[] letters = ConvertLettersToLowerCaseCharArray();
        WordList = await Task.Run(() => wordModel.SolveLetters(letters));
    }

    private bool CanSolve(object? _, bool isExecuting)
    {
        return !(HasErrors || isExecuting || letters.Any(s => string.IsNullOrEmpty(s)) || WordList.Any());
    }


    private void ExecuteChooseLetters(object? _)
    {
        int vowelCount = Settings.Data.ChooseLettersIndex + 3;   // index 0 is "3 vowels and 6 consonants"

        IList<char> letters = wordModel.GenerateLettersData(vowelCount);

        // set the backing store directly, the letters don't need validating
        for (int index = 0; index < WordModel.cLetterCount; ++index)
        {
            this.letters[index] = letters[index].ToString();
            RaisePropertyChanged(propertyNames[index]);
            ClearValidationError(index);
        }

        if (WordList.Any())
            WordList = new List<string>();

        UpdateCommandsExecuteStatus();
    }

    private void ExecuteChooseLettersMenuOption(object? p)
    {
        int newIndex = 0;

        if (int.TryParse(p as string, out int value))
            newIndex = value;

        Settings.Data.ChooseLettersIndex = newIndex;
        ChooseLettersCommand.Execute(null);
    }

    private char[] ConvertLettersToLowerCaseCharArray()
    {
        char[] data = new char[WordModel.cLetterCount];

        for (int index = 0; index < WordModel.cLetterCount; ++index)
        {
            Debug.Assert(letters[index].Length == 1);
            data[index] = char.ToLower(letters[index][0]);
        }

        return data;
    }
}