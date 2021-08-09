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

        private StopwatchStateEnum _stopwatchState;

        private long _ticks;

        private CancellationTokenSource _cts;

        /// <summary>
        /// expose a command that buttons can bind to
        /// </summary>
        public ICommand StartStopTimerCommand { get; }


        public StopwatchController()
        {
            StartStopTimerCommand = new RelayCommand(ExecuteTimer);
            _cts = new CancellationTokenSource();
        }

        // the elapsed time in system ticks 
        public long Ticks
        {
            get { return _ticks; }
            private set { HandlePropertyChanged(ref _ticks, value); }
        }


        public StopwatchStateEnum StopwatchState
        {
            get { return _stopwatchState; }
            private set { HandlePropertyChanged(ref _stopwatchState, value); }
        }

        private async Task ClockForwardAnimation()
        {
            long startTime = DateTime.UtcNow.Ticks;

            try
            {
                await Task.Run(async () =>
                {
                    while (true)
                    {
                        if (_cts.Token.IsCancellationRequested)
                            throw new TaskCanceledException();

                        Ticks = DateTime.UtcNow.Ticks - startTime;

                        if (Ticks < cForwardDurationTicks)
                            await Task.Delay(cUpdateRateMilliseconds, _cts.Token); // it's not a high precession timer
                        else
                            break;
                    }
                }, _cts.Token);
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

                await Task.Run(async () =>
                {
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
                });

                Ticks = 0;  // guarantee a final value
            }
        }

        // this method is re-entrant due to the await operator but will only
        // ever be run on the UI thread
        private async void ExecuteTimer(object p)
        {
            switch (StopwatchState)
            {
                case StopwatchStateEnum.AtStart:
                    {
                        StopwatchState = StopwatchStateEnum.Running;

                        await ClockForwardAnimation();

                        if (_cts.IsCancellationRequested)
                        {
                            _cts.Dispose();
                            _cts = new CancellationTokenSource();
                        }
                        else
                            System.Media.SystemSounds.Exclamation.Play();

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
                        _cts.Cancel();
                        break;
                    }

                case StopwatchStateEnum.Rewinding: break;

                default: throw new InvalidOperationException();
            }
        }

        public void Dispose()
        {
            _cts.Dispose();
        }
    }
}
