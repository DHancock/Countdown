using Countdown.Models;

namespace Countdown.ViewModels;

internal sealed class NumbersViewModel : DataErrorInfoBase
{
    // this tile value indicates an empty string
    private const int cEmptyTileValue = -1;

    // the solver results that the ui can bind to
    private List<string> equationList = new List<string>();

    // the number of input text boxes which need validating (6 tiles plus the target)
    private const int cInputCount = 7;  

    public ICommand ChooseNumbersCommand { get; }
    public RelayTaskCommand SolveCommand { get; }
    public ICommand ChooseOptionCommand { get; }
    private Model Model { get; }
    public StopwatchController StopwatchController { get; }
   

    public NumbersViewModel(Model model, StopwatchController sc) : base(cInputCount)
    {
        Model = model;
        StopwatchController = sc;

        ChooseNumbersCommand = new RelayCommand(ExecuteChoose);
        SolveCommand = new RelayTaskCommand(ExecuteSolveAsync, CanSolve);
        ChooseOptionCommand = new RelayCommand(ExecuteChooseOption);

        // initialise numbers
        ChooseNumbersCommand.Execute(null);
    }

    public string Tile_0
    {
        get => Convert(Model.Tiles[0]);
        set => SetTile(ref Model.Tiles[0], value);
    }

    public string Tile_1
    {
        get => Convert(Model.Tiles[1]);
        set => SetTile(ref Model.Tiles[1], value);
    }

    public string Tile_2
    {
        get => Convert(Model.Tiles[2]);
        set => SetTile(ref Model.Tiles[2], value);
    }

    public string Tile_3
    {
        get => Convert(Model.Tiles[3]);
        set => SetTile(ref Model.Tiles[3], value);
    }

    public string Tile_4
    {
        get => Convert(Model.Tiles[4]);
        set => SetTile(ref Model.Tiles[4], value);
    }

    public string Tile_5
    {
        get => Convert(Model.Tiles[5]);
        set => SetTile(ref Model.Tiles[5], value);
    }

    private void SetTile(ref int existing, string data, [CallerMemberName] string? propertyName = default)
    {
        int temp = Convert(data);

        if (temp != existing)
        {
            existing = temp;
            ValidateTiles();
            RaisePropertyChanged(propertyName);
            SolveCommand.RaiseCanExecuteChanged();
        }
    }

    public string Target
    {
        get => Convert(Model.Target);
        set
        {
            int temp = Convert(value);

            if (temp != Model.Target)
            {
                Model.Target = temp;
                ValidateTarget();
                RaisePropertyChanged(nameof(Target));
                SolveCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Converts string properties bound to text boxes to int rather than using the 
    /// built in xaml converters. They throw an error when converting an empty string
    /// and insert a cryptic message in the data error notifications. A custom binding 
    /// converter would be just more untestable code behind.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private static int Convert(string input)
    {
        if ((input.Length > 0) && int.TryParse(input, out int output))
            return output;

        return cEmptyTileValue;
    }

    /// <summary>
    /// Converts int to a string for properties bound to text boxes rather than using the
    /// built in xaml converters. Converts negative values to an empty string.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private static string Convert(int input)
    {
        if (input is cEmptyTileValue)
            return string.Empty;

        return input.ToString();
    }

    /// <summary>
    /// Checks that the tiles and target contain valid values.
    /// </summary>
    /// <returns></returns>
    private void ValidateTiles()
    {
        int[] validTileValues = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 25, 50, 75, 100 };

        // count of how many tiles of each valid tile value
        int[] tileCount = new int[validTileValues.Length];
        // record each property's value position in the tile values array
        // used to index into the tile count array
        int[] searchResult = new int[Model.Tiles.Length];

        // first check for invalid tile values
        for (int index = 0; index < Model.Tiles.Length; index++)
        {
            if (Model.Tiles[index] > cEmptyTileValue)
            {
                searchResult[index] = Array.BinarySearch(validTileValues, Model.Tiles[index]);

                if (searchResult[index] < 0) // not found, an invalid value
                    SetValidationError(index, "Tile values must be from 1 to 10, or 25, 50, 75 or 100");
                else
                    tileCount[searchResult[index]] += 1; // count how many of each value
            }
            else
                ClearValidationError(index);
        }

        // check the tile counts of valid tiles
        for (int index = 0; index < Model.Tiles.Length; index++)
        {
            if ((Model.Tiles[index] > cEmptyTileValue) && (searchResult[index] >= 0))
            {
                int validTileCount = (Model.Tiles[index] > 10) ? 1 : 2;

                if (tileCount[searchResult[index]] > validTileCount)
                {
                    if (validTileCount == 1)
                        SetValidationError(index, "Only one 25, 50, 75 or 100 tile is allowed.");
                    else
                        SetValidationError(index, "Only two tiles with the same value of 10 or less are allowed.");
                }
                else
                    ClearValidationError(index);
            }
        }

        // No checking if the number of large and small tiles match the choose menu option
        // It will only be incorrect if the user enters tiles manually and as long as they are valid
        // tiles so be it...
    }

    private void ValidateTarget()
    {
        const int cIndex = cInputCount - 1;

        if (((Model.Target < Model.cMinTarget) || (Model.Target > Model.cMaxTarget)) && (Model.Target != cEmptyTileValue))
            SetValidationError(cIndex, $"The target must be between {Model.cMinTarget} and {Model.cMaxTarget}");
        else
            ClearValidationError(cIndex);
    }

    public int TileOptionIndex
    {
        get => Settings.Data.ChooseNumbersIndex;
        set
        {
            Settings.Data.ChooseNumbersIndex = value;
            ChooseNumbersCommand.Execute(null);
        }
    }

    private void ExecuteChoose(object? _)
    {
        Model.GenerateNumberData(TileOptionIndex);

        // notify the ui of the updated data 
        RaisePropertyChanged(nameof(Tile_0));
        RaisePropertyChanged(nameof(Tile_1));
        RaisePropertyChanged(nameof(Tile_2));
        RaisePropertyChanged(nameof(Tile_3));
        RaisePropertyChanged(nameof(Tile_4));
        RaisePropertyChanged(nameof(Tile_5));
        RaisePropertyChanged(nameof(Target));

        // clear any error states
        ClearAllErrors();

        SolveCommand.RaiseCanExecuteChanged();
    }

    private async Task ExecuteSolveAsync(object? _)
    {
        // copy the model data now, it could be changed before 
        // the task is run maybe to an invalid value
        int target = Model.Target;
        int[] tiles = Model.Tiles.ToArray();

        SolverResults results = await Task.Run(() => Model.Solve(tiles, target));

        if (results.Solutions.Count == 0)
        {
            results.Solutions.Add("There are no solutions.");

            if (results.HasClosestResult)
            {
                results.Solutions.Add($"The closest match is {results.Difference} away.");
                results.Solutions.Add(string.Empty);
                results.Solutions.Add($"{results.ClosestEquation} = {results.ClosestResult}");
            }
        }
        else
        {
            // guarantee ordering independent of parallel partition order
            results.Solutions.Sort((a, b) =>
            {
                // shorter strings first, then reverse alphabetical (numbers before parenthesis)
                int lengthCompare = a.Length - b.Length;
                return lengthCompare == 0 ? string.Compare(b, a, StringComparison.CurrentCulture) : lengthCompare;
            });   

#if false
            results.Solutions.Add(string.Empty);
            results.Solutions.Add($"There are {results.Solutions.Count - 1} solutions.");
            results.Solutions.Add($"Evaluated in {results.Elapsed.TotalMilliseconds} milliseconds.");
            results.Solutions.Add($"Tiles are {tiles[0]}, {tiles[1]}, {tiles[2]}, {tiles[3]}, {tiles[4]}, {tiles[5]}");
            results.Solutions.Add($"Target is {target}");
#endif
        }

        // update the ui
        EquationList = results.Solutions;
    }

    private bool CanSolve(object? _, bool isExecuting)
    {
        return !(HasErrors || isExecuting || Model.Tiles.Contains(cEmptyTileValue) || Model.Target is cEmptyTileValue);
    }

    /// <summary>
    /// Expose the list so it can be bound to by the ui 
    /// </summary>
    public List<string> EquationList
    {
        get => equationList;
        private set => HandlePropertyChanged(ref equationList, value);
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
