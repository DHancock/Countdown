using System;
using Microsoft.UI.Xaml;

namespace Countdown.ViewModels
{
    internal sealed class StopwatchController : PropertyChangedBase
    {
        private const long cForwardDurationTicks = 30 * TimeSpan.TicksPerSecond;
        private const long cRewindDurationTicks = 1 * TimeSpan.TicksPerSecond;
        private const int cUpdateRateMilliseconds = 16;   // 60Hz refresh rate

        public enum StopwatchStateEnum { AtStart, Running, Stopped, Rewinding }

        private StopwatchStateEnum stopwatchState;
        private long ticks;

        private readonly DispatcherTimer dispatcherTimer;
        private long startTicks;
        private long startTime;

        private string commandText = string.Empty;

        public RelayCommand TimerCommand { get; }


        public StopwatchController()
        {
            TimerCommand = new RelayCommand(ExecuteTimer, CanExecuteTimer);

            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Interval = TimeSpan.FromMilliseconds(cUpdateRateMilliseconds);

            CommandText = ConvertStateToCommandText();
        }



        private void DispatcherTimer_Tick(object? sender, object e)
        {
            if (StopwatchState == StopwatchStateEnum.Running)
            {
                long newTicks = DateTime.UtcNow.Ticks - startTime;

                if (newTicks >= cForwardDurationTicks)
                {
                    Utils.User32Sound.PlayExclamation();
                    StopwatchState = StopwatchStateEnum.Stopped;
                    dispatcherTimer.Stop();
                    Ticks = cForwardDurationTicks;
                }
                else
                    Ticks = newTicks;
            }
            else if (StopwatchState == StopwatchStateEnum.Rewinding)
            {
                // accelerate elapsed time by the rewind speed
                long elapsed = (DateTime.UtcNow.Ticks - startTime) * (cForwardDurationTicks / cRewindDurationTicks);
                long newTicks = startTicks - elapsed;

                if (newTicks <= 0)
                {
                    Ticks = 0;
                    dispatcherTimer.Stop();
                    StopwatchState = StopwatchStateEnum.AtStart;
                }
                else
                    Ticks = newTicks;
            }
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

        private void StartClockForwardAnimation()
        {
            startTime = DateTime.UtcNow.Ticks;
            dispatcherTimer.Start();
        }

        private void StartClockRewindAnimation()
        {
            startTicks = Ticks;
            startTime = DateTime.UtcNow.Ticks;
            dispatcherTimer.Start();
        }


        private void ExecuteTimer(object? _)
        {
            switch (StopwatchState)
            {
                case StopwatchStateEnum.AtStart:
                    {
                        StopwatchState = StopwatchStateEnum.Running;
                        StartClockForwardAnimation();
                        break;
                    }

                case StopwatchStateEnum.Stopped:
                    {
                        StopwatchState = StopwatchStateEnum.Rewinding;
                        StartClockRewindAnimation();
                        break;
                    }

                case StopwatchStateEnum.Running:
                    {
                        StopwatchState = StopwatchStateEnum.Stopped;
                        dispatcherTimer.Stop();
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
    }
}
