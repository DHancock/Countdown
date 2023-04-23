using Countdown.Models;

namespace Countdown.ViewModels;

internal sealed class ConundrumViewModel : PropertyChangedBase
{
    private const string emptySolution = "         ";
    
    private string solution = emptySolution;

    private readonly string[] conundrum = new string[WordModel.cMaxLetters];

    private readonly WordModel wordModel;
    public StopwatchController StopwatchController { get; }
    public ObservableCollection<ConundrumItem> SolutionList { get; } = new ObservableCollection<ConundrumItem>();
    public ICommand ChooseCommand { get; }
    public RelayCommand SolveCommand { get; }


    public ConundrumViewModel(WordModel model, StopwatchController sc)
    {
        wordModel = model;
        StopwatchController = sc;

        SolveCommand = new RelayCommand(ExecuteSolve, CanSolve);
        ChooseCommand = new RelayCommand(ExecuteChoose, CanChoose);

        ExecuteChoose(null);
    }

    /// <summary>
    /// Conundrum properties. 
    /// The char type cannot an have empty value so use strings.
    /// </summary>
    public string Conundrum_0
    {
        get => conundrum[0];
        set
        {
            conundrum[0] = value;
            UpdateProperties();
        }
    }

    public string Conundrum_1
    {
        get => conundrum[1];
        set
        {
            conundrum[1] = value;
            UpdateProperties();
        }
    }

    public string Conundrum_2
    {
        get => conundrum[2];
        set
        {
            conundrum[2] = value;
            UpdateProperties();
        }
    }

    public string Conundrum_3
    {
        get => conundrum[3];
        set
        {
            conundrum[3] = value;
            UpdateProperties();
        }
    }

    public string Conundrum_4
    {
        get => conundrum[4];
        set
        {
            conundrum[4] = value;
            UpdateProperties();
        }
    }

    public string Conundrum_5
    {
        get => conundrum[5];
        set
        {
            conundrum[5] = value;
            UpdateProperties();
        }
    }

    public string Conundrum_6
    {
        get => conundrum[6];
        set
        {
            conundrum[6] = value;
            UpdateProperties();
        }
    }

    public string Conundrum_7
    {
        get => conundrum[7];
        set
        {
            conundrum[7] = value;
            UpdateProperties();
        }
    }

    public string Conundrum_8
    {
        get => conundrum[8];
        set 
        { 
            conundrum[8] = value;
            UpdateProperties(); 
        }
    }


    private void UpdateProperties([CallerMemberName] string? propertyName = default)
    {
        RaisePropertyChanged(propertyName);
        SolveCommand.RaiseCanExecuteChanged();
        Solution = emptySolution;
    }

    public string Solution
    {
        get { return solution; }
        set
        {
            solution = value;
            RaisePropertyChanged(nameof(Solution));
        }
    }

    private void ExecuteChoose(object? _)
    {
        IList<char> conundrum = wordModel.GenerateConundrum();

        // there's not much validation code so just set properties directly 
        Conundrum_0 = conundrum[0].ToString();
        Conundrum_1 = conundrum[1].ToString();
        Conundrum_2 = conundrum[2].ToString();
        Conundrum_3 = conundrum[3].ToString();
        Conundrum_4 = conundrum[4].ToString();
        Conundrum_5 = conundrum[5].ToString();
        Conundrum_6 = conundrum[6].ToString();
        Conundrum_7 = conundrum[7].ToString();
        Conundrum_8 = conundrum[8].ToString();

        Solution = emptySolution;
        SolveCommand.RaiseCanExecuteChanged();
    }

    private bool CanChoose(object? _) => wordModel.HasConundrums;

    private void ExecuteSolve(object? _)
    {
        string solution = wordModel.SolveConundrum(ConvertLetters(toLowerCase: true));

        if (solution.Length > 0)
        {
            Solution = solution;
            SolutionList.Insert(0, new ConundrumItem(new string(ConvertLetters()), solution));

            SolveCommand.RaiseCanExecuteChanged();
        }
    }

    private bool CanSolve(object? _)
    {
        return string.IsNullOrWhiteSpace(Solution) && 
                conundrum.All(s => !string.IsNullOrEmpty(s)) && 
                !string.IsNullOrEmpty(wordModel.SolveConundrum(ConvertLetters(toLowerCase: true)));
    }

    private char[] ConvertLetters(bool toLowerCase = false)
    {
        char[] data = new char[WordModel.cLetterCount];

        for (int index = 0; index < WordModel.cLetterCount; ++index)
        {
            Debug.Assert(conundrum[index].Length == 1);
            char c = conundrum[index][0];
            data[index] = toLowerCase ? char.ToLower(c) : c;
        }

        return data;
    }
}
