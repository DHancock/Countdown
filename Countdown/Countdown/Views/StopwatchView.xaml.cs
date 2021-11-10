using Countdown.ViewModels;

using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

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
