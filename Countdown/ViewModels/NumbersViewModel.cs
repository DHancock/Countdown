using Countdown.Models;

namespace Countdown.ViewModels;

internal sealed partial class NumbersViewModel : DataErrorInfoBase
{
    // this tile value indicates an empty string
    private const int cEmptyTileValue = -1;

    // the solver results that the ui can bind to
    private IEnumerable<string> equationList = new List<string>();

    // for raising property change events when generating data
    private static readonly string[] propertyNames = [ nameof(Tile_0),
                                                        nameof(Tile_1),
                                                        nameof(Tile_2),
                                                        nameof(Tile_3),
                                                        nameof(Tile_4),
                                                        nameof(Tile_5)];

    private readonly int[] validTileValues = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 25, 50, 75, 100];

    private readonly NumberModel numberModel;

    private readonly int[] tiles = new int[NumberModel.cNumberTileCount];

    private int target = NumberModel.cMinTarget;

    public ICommand ChooseNumbersCommand { get; }
    public RelayTaskCommand SolveCommand { get; }
    public ICommand ChooseOptionCommand { get; }
    
    public StopwatchController StopwatchController { get; }
   

    public NumbersViewModel(NumberModel model, StopwatchController sc) : base(NumberModel.cNumberTileCount + 1)
    {
        numberModel = model;
        StopwatchController = sc;

        ChooseNumbersCommand = new RelayCommand(ExecuteChoose);
        SolveCommand = new RelayTaskCommand(ExecuteSolveAsync, CanSolve);
        ChooseOptionCommand = new RelayCommand(ExecuteChooseNumbersMenuOption);

        // initialise numbers
        ChooseNumbersCommand.Execute(null);
    }

    public string Tile_0
    {
        get => Convert(tiles[0]);
        set
        {
            tiles[0] = Convert(value);
            UpdateProperties();
        }
    }

    public string Tile_1
    {
        get => Convert(tiles[1]);
        set
        {
            tiles[1] = Convert(value);
            UpdateProperties();
        }
    }

    public string Tile_2
    {
        get => Convert(tiles[2]);
        set
        {
            tiles[2] = Convert(value);
            UpdateProperties();
        }
    }

    public string Tile_3
    {
        get => Convert(tiles[3]);
        set
        {
            tiles[3] = Convert(value);
            UpdateProperties();
        }
    }

    public string Tile_4
    {
        get => Convert(tiles[4]);
        set
        {
            tiles[4] = Convert(value);
            UpdateProperties();
        }
    }

    public string Tile_5
    {
        get => Convert(tiles[5]);
        set
        {
            tiles[5] = Convert(value);
            UpdateProperties();
        }
    }

    public string Target
    {
        get => Convert(target);
        set
        {
            target = Convert(value);
            UpdateProperties();
        }
    }

    private void UpdateProperties([CallerMemberName] string? propertyName = default)
    {
        if (propertyName == nameof(Target))
        {
            ValidateTarget();
        }
        else
        {
            ValidateTiles();
        }

        if (EquationList.Any())
        {
            EquationList = new List<string>();
        }

        RaisePropertyChanged(propertyName);
        SolveCommand.RaiseCanExecuteChanged();
    }

    private static int Convert(string input)
    {
        if ((input.Length > 0) && int.TryParse(input, out int output))
        {
            return output;
        }

        return cEmptyTileValue;
    }

    private static string Convert(int input)
    {
        if (input is cEmptyTileValue)
        {
            return string.Empty;
        }

        return input.ToString();
    }

    /// <summary>
    /// Checks that the tiles and target contain valid values.
    /// </summary>
    /// <returns></returns>
    private void ValidateTiles()
    {
        // count of how many tiles of each valid tile value
        int[] tileCount = new int[validTileValues.Length];
        // record each property's value position in the tile values array
        // used to index into the tile count array
        int[] searchResult = new int[tiles.Length];

        // first check for invalid tile values
        for (int index = 0; index < tiles.Length; index++)
        {
            if (tiles[index] > cEmptyTileValue)
            {
                searchResult[index] = Array.BinarySearch(validTileValues, tiles[index]);

                if (searchResult[index] < 0) // not found, an invalid value
                {
                    SetValidationError(index, "Tile values must be from 1 to 10, or 25, 50, 75 or 100");
                }
                else
                {
                    tileCount[searchResult[index]] += 1; // count how many of each value
                }
            }
            else
            {
                ClearValidationError(index);
            }
        }

        // check the tile counts of valid tiles
        for (int index = 0; index < tiles.Length; index++)
        {
            if ((tiles[index] > cEmptyTileValue) && (searchResult[index] >= 0))
            {
                int validTileCount = (tiles[index] > 10) ? 1 : 2;

                if (tileCount[searchResult[index]] > validTileCount)
                {
                    if (validTileCount == 1)
                    {
                        SetValidationError(index, "Only one 25, 50, 75 or 100 tile is allowed.");
                    }
                    else
                    {
                        SetValidationError(index, "Only two tiles with the same value of 10 or less are allowed.");
                    }
                }
                else
                {
                    ClearValidationError(index);
                }
            }
        }

        // No checking if the number of large and small tiles match the choose menu option
        // It will only be incorrect if the user enters tiles manually and as long as they are valid
        // tiles so be it...
    }

    private void ValidateTarget()
    {
        const int cIndex = NumberModel.cNumberTileCount;

        if (((target < NumberModel.cMinTarget) || (target > NumberModel.cMaxTarget)) && (target != cEmptyTileValue))
        {
            SetValidationError(cIndex, $"The target must be between {NumberModel.cMinTarget} and {NumberModel.cMaxTarget}");
        }
        else
        {
            ClearValidationError(cIndex);
        }
    }

    private void ExecuteChoose(object? _)
    {
        int[] numbers = numberModel.GenerateNumberData(Settings.Instance.ChooseNumbersIndex);

        // set the backing store directly, no validation is required
        for (int index = 0; index < NumberModel.cNumberTileCount; ++index)
        {
            tiles[index] = numbers[index];
            RaisePropertyChanged(propertyNames[index]);
        }

        target = numberModel.GenerateTarget();
        RaisePropertyChanged(nameof(Target));

        ClearAllErrors();

        if (EquationList.Any())
        {
            EquationList = new List<string>();
        }

        SolveCommand.RaiseCanExecuteChanged();
    }

    private async Task ExecuteSolveAsync(object? _)
    {
        // the data could change before the task is run
        int targetCopy = target;
        int[] tilesCopy = tiles.ToArray();

        SolverResults results = await Task.Run(() => NumberModel.Solve(tilesCopy, targetCopy));

        List<string> output = results.GetResults();

        if (output.Count == 0)
        {
            output.Add($"No solutions are {SolvingEngine.cNonMatchThreshold} or less away.");
        }
        else
        {
            // guarantee ordering independent of parallel partition order
            output.Sort((a, b) =>
            {
                int lengthCompare = a.Length - b.Length;
                return lengthCompare == 0 ? string.Compare(b, a, StringComparison.CurrentCulture) : lengthCompare;
            });

            if (!results.HasSolutions)
            {
                output.Insert(0, $"The closest match is {results.LowestDifference} away.");
                output.Insert(1, string.Empty);
            }
        }

        EquationList = output;
    }

    private bool CanSolve(object? _, bool isExecuting)
    {
        return !(HasErrors || isExecuting || tiles.Contains(cEmptyTileValue) || target is cEmptyTileValue || EquationList.Any());
    }

    /// <summary>
    /// Expose the list so it can be bound to by the ui 
    /// </summary>
    public IEnumerable<string> EquationList
    {
        get => equationList;
        private set
        {
            equationList = value;
            RaisePropertyChanged();
        }
    }

    private void ExecuteChooseNumbersMenuOption(object? p)
    {
        int newIndex = 0;

        if (int.TryParse(p as string, out int value))
        {
            newIndex = value;
        }
       
        Settings.Instance.ChooseNumbersIndex = newIndex;
        ChooseNumbersCommand.Execute(null);
    }
}
