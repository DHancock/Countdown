using Countdown.Models;

namespace Countdown.ViewModels;

internal sealed class ConundrumViewModel : PropertyChangedBase
{
    private readonly string emptySolution = new string(' ', Model.cLetterCount);

    private Model Model { get; }
    public StopwatchController StopwatchController { get; }
    public ObservableCollection<ConundrumItem> SolutionList { get; } = new ObservableCollection<ConundrumItem>();

    private string solution;

    public ICommand ChooseCommand { get; }
    public RelayCommand SolveCommand { get; }


    public ConundrumViewModel(Model model, StopwatchController sc)
    {
        Model = model;
        StopwatchController = sc;
        solution = emptySolution;

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
        get { return Model.Conundrum[0]; }
        set { SetConundrum(value, ref Model.Conundrum[0]); }
    }

    public string Conundrum_1
    {
        get { return Model.Conundrum[1]; }
        set { SetConundrum(value, ref Model.Conundrum[1]); }
    }

    public string Conundrum_2
    {
        get { return Model.Conundrum[2]; }
        set { SetConundrum(value, ref Model.Conundrum[2]); }
    }

    public string Conundrum_3
    {
        get { return Model.Conundrum[3]; }
        set { SetConundrum(value, ref Model.Conundrum[3]); }
    }

    public string Conundrum_4
    {
        get { return Model.Conundrum[4]; }
        set { SetConundrum(value, ref Model.Conundrum[4]); }
    }

    public string Conundrum_5
    {
        get { return Model.Conundrum[5]; }
        set { SetConundrum(value, ref Model.Conundrum[5]); }
    }

    public string Conundrum_6
    {
        get { return Model.Conundrum[6]; }
        set { SetConundrum(value, ref Model.Conundrum[6]); }
    }

    public string Conundrum_7
    {
        get { return Model.Conundrum[7]; }
        set { SetConundrum(value, ref Model.Conundrum[7]); }
    }

    public string Conundrum_8
    {
        get { return Model.Conundrum[8]; }
        set { SetConundrum(value, ref Model.Conundrum[8]); }
    }


    private void SetConundrum(string newValue, ref string existing, [CallerMemberName] string propertyName = "")
    {
        if (newValue != existing)
        {
            existing = newValue;
            RaisePropertyChanged(propertyName);
            SolveCommand.RaiseCanExecuteChanged();
            Solution = emptySolution;
        }
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
        Solution = emptySolution;

        Model.GenerateConundrum();

        RaisePropertyChanged(nameof(Conundrum_0));
        RaisePropertyChanged(nameof(Conundrum_1));
        RaisePropertyChanged(nameof(Conundrum_2));
        RaisePropertyChanged(nameof(Conundrum_3));
        RaisePropertyChanged(nameof(Conundrum_4));
        RaisePropertyChanged(nameof(Conundrum_5));
        RaisePropertyChanged(nameof(Conundrum_6));
        RaisePropertyChanged(nameof(Conundrum_7));
        RaisePropertyChanged(nameof(Conundrum_8));

        SolveCommand.RaiseCanExecuteChanged();
    }

    private bool CanChoose(object? _) => Model.HasConundrums;

    private void ExecuteSolve(object? _)
    {
        string word = Model.Solve();

        if (word.Length > 0)
        {
            char[] conundrum = new char[Model.cLetterCount];

            for (int index = 0; index < Model.cLetterCount; ++index)
                conundrum[index] = Model.Conundrum[index][0];

            Solution = word;

            SolutionList.Insert(0, new ConundrumItem(new string(conundrum), word));

            SolveCommand.RaiseCanExecuteChanged();
        }
    }

    private bool CanSolve(object? _)
    {
        return string.IsNullOrWhiteSpace(Solution) && !Model.Conundrum.Any(s => string.IsNullOrEmpty(s)) && !string.IsNullOrEmpty(Model.Solve());
    }
}
