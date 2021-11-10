using Countdown.ViewModels;

namespace Countdown.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    internal sealed partial class StopwatchView : Page
    {
        public StopwatchView()
        {
            this.InitializeComponent();
        }

        public StopwatchViewModel? ViewModel { get; set; }
    }
}
