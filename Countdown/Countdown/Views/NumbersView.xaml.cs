using System.Text;

using Countdown.ViewModels;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using Windows.ApplicationModel.DataTransfer;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Countdown.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    internal sealed partial class NumbersView : Page
    {
        public NumbersView()
        {
            this.InitializeComponent();
        }

        public NumbersViewModel? ViewModel { get; set; }

        private void CopyCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            StringBuilder sb = new StringBuilder();

            foreach (EquationItem equationItem in EquationList.SelectedItems)
                sb.AppendLine(equationItem.ToString());

            if (sb.Length > 0)
            {
                DataPackage dp = new();
                dp.SetText(sb.ToString());
                Clipboard.SetContent(dp);
            }
        }

        private void CopyCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = EquationList.SelectedItems.Any();
        }
    }
}
