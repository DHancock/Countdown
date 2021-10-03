using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Countdown.ViewModels
{
    internal sealed class StopwatchController : PropertyChangedBase, IDisposable
    {
        private const long cForwardDurationTicks = 30 * TimeSpan.TicksPerSecond;
        private const long cRewindDurationTicks = 1 * TimeSpan.TicksPerSecond;
        private const int cUpdateRateMilliseconds = 5;

        public enum StopwatchStateEnum { AtStart, Running, Stopped, Rewinding }

        private StopwatchStateEnum stopwatchState;
        private long ticks;
        private CancellationTokenSource cts;
        private string commandText = string.Empty;

        public RelayCommand TimerCommand { get; }


        public StopwatchController()
        {
            TimerCommand = new RelayCommand(ExecuteTimer, CanExecuteTimer);
            cts = new CancellationTokenSource();
            CommandText = ConvertStateToCommandText();
        }

        // the elapsed stopwatch time in system ticks 
        public long Ticks
        {
            get { return ticks; }
            private set { HandlePropertyChanged(ref ticks, value); }
        }

        private StopwatchStateEnum StopwatchState
        {
            get => stopwatchState;

            set
            {
                if (stopwatchState != value)
                {
                    stopwatchState = value;
                    CommandText = ConvertStateToCommandText();
                    TimerCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private string ConvertStateToCommandText()
        {
            switch (StopwatchState)
            {
                case StopwatchStateEnum.AtStart: return "Start Timer";
                case StopwatchStateEnum.Running: return "Stop Timer";
                case StopwatchStateEnum.Stopped: return "Reset Timer";
                case StopwatchStateEnum.Rewinding: return "Rewinding";
                default: throw new InvalidOperationException();
            }
        }

        public string CommandText
        {
            get => commandText;
            private set => HandlePropertyChanged(ref commandText, value);
        }

        private async Task ClockForwardAnimation()
        {
            long startTime = DateTime.UtcNow.Ticks;

            try
            {
                while (true)
                {
                    Ticks = DateTime.UtcNow.Ticks - startTime;

                    if (Ticks < cForwardDurationTicks)
                        await Task.Delay(cUpdateRateMilliseconds, cts.Token); // it's not a high precession timer
                    else
                        break;
                }
            }
            catch (TaskCanceledException)
            {
                return;
            }

            Ticks = cForwardDurationTicks;   // guarantee a final value
        }

        private async Task ClockRewindAnimation()
        {
            long startTicks = Ticks;

            if (startTicks > 0)
            {
                long startTime = DateTime.UtcNow.Ticks;

                while (true)
                {
                    // accelerate elapsed time by the rewind speed
                    long elapsed = (DateTime.UtcNow.Ticks - startTime) * (cForwardDurationTicks / cRewindDurationTicks);
                    Ticks = startTicks - elapsed;

                    if (Ticks > 0)
                        await Task.Delay(cUpdateRateMilliseconds);   // it's not a high precession timer
                    else
                        break;
                }

                Ticks = 0;  // guarantee a final value
            }
        }

        // this method is re-entrant due to the await operator but will only
        // ever be run on the UI thread
        private async void ExecuteTimer(object? _)
        {
            switch (StopwatchState)
            {
                case StopwatchStateEnum.AtStart:
                    {
                        StopwatchState = StopwatchStateEnum.Running;

                        await ClockForwardAnimation();

                        if (cts.IsCancellationRequested)
                        {
                            cts.Dispose();
                            cts = new CancellationTokenSource();
                        }
                        else
                            Utils.User32Sound.PlayExclamation();

                        StopwatchState = StopwatchStateEnum.Stopped;
                        break;
                    }

                case StopwatchStateEnum.Stopped:
                    {
                        StopwatchState = StopwatchStateEnum.Rewinding;

                        await ClockRewindAnimation();

                        StopwatchState = StopwatchStateEnum.AtStart;
                        break;
                    }

                case StopwatchStateEnum.Running:
                    {
                        StopwatchState = StopwatchStateEnum.Stopped;
                        cts.Cancel();
                        break;
                    }

                case StopwatchStateEnum.Rewinding: break;

                default: throw new InvalidOperationException();
            }
        }

        private bool CanExecuteTimer(object? _)
        {
            return StopwatchState != StopwatchStateEnum.Rewinding;
        }

        public void Dispose()
        {
            cts.Dispose();
        }
    }
}
