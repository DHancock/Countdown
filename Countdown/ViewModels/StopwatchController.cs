namespace Countdown.ViewModels;

public enum StopwatchState { Undefined, Initializing, AtStart, Running, Stopped, Completed, Rewinding }

internal sealed class StopwatchController : PropertyChangedBase
{
    private StopwatchState stopwatchState = StopwatchState.Initializing;
    private string commandText = string.Empty;
    
    public RelayCommand TimerCommand { get; }

    public StopwatchController()
    {
        TimerCommand = new RelayCommand(ExecuteTimer, CanExecuteTimer);
        CommandText = ConvertStateToCommandText();
    }

    public StopwatchState State
    {
        get => stopwatchState;

        set
        {
            if (stopwatchState != value)
            {
                stopwatchState = value;
                RaisePropertyChanged();
                CommandText = ConvertStateToCommandText();
                TimerCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private string ConvertStateToCommandText()
    {
        switch (State)
        {
            case StopwatchState.Initializing:
            case StopwatchState.AtStart: return "Start Timer";
            case StopwatchState.Running: return "Stop Timer";
            case StopwatchState.Stopped:
            case StopwatchState.Completed: return "Reset Timer";
            case StopwatchState.Rewinding: return "Rewinding";

            default: throw new Exception($"invalid state: {State}"); ;
        }
    }

    public string CommandText
    {
        get => commandText;
        private set
        {
            commandText = value;
            RaisePropertyChanged();
        }
    }

    private void ExecuteTimer(object? _)
    {
        switch (State)
        {
            case StopwatchState.AtStart: State = StopwatchState.Running; break;
            case StopwatchState.Stopped:
            case StopwatchState.Completed: State = StopwatchState.Rewinding; break;
            case StopwatchState.Running: State = StopwatchState.Stopped; break;

            default: throw new Exception($"invalid state: {State}"); ;
        }
    }

    private bool CanExecuteTimer(object? _)
    {
        return State != StopwatchState.Rewinding && State != StopwatchState.Initializing;
    }
}
