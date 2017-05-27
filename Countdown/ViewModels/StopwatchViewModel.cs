using System;

namespace Countdown.ViewModels
{
    internal sealed class StopwatchViewModel 
    {
        // bindable property
        public StopwatchController StopwatchController { get; }

        public StopwatchViewModel(StopwatchController sc)
        {
            StopwatchController = sc ?? throw new ArgumentNullException(nameof(sc));
        }
    }
}
