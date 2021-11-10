namespace Countdown.ViewModels
{
    public enum StopwatchState { Undefined, AtStart, Running, Stopped, Rewinding }

    internal sealed class StopwatchController : PropertyChangedBase
    {
        private StopwatchState stopwatchState = StopwatchState.AtStart;
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
                case StopwatchState.AtStart: return "Start Timer";
                case StopwatchState.Running: return "Stop Timer";
                case StopwatchState.Stopped: return "Reset Timer";
                case StopwatchState.Rewinding: return "Rewinding";
                default: throw new InvalidOperationException();
            }
        }

        public string CommandText
        {
            get => commandText;
            private set => HandlePropertyChanged(ref commandText, value);
        }


        private void ExecuteTimer(object? _)
        {
            switch (State)
            {
                case StopwatchState.AtStart: State = StopwatchState.Running; break;
                case StopwatchState.Stopped: State = StopwatchState.Rewinding; break;
                case StopwatchState.Running: State = StopwatchState.Stopped; break;
                default: throw new InvalidOperationException();
            }
        }

        private bool CanExecuteTimer(object? _)
        {
            switch (State)
            {
                case StopwatchState.AtStart:
                case StopwatchState.Stopped:
                case StopwatchState.Running: return true;
                default: return false;
            }
        }
    }
}
