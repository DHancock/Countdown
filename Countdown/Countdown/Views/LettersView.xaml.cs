using Countdown.ViewModels;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using System.Text;

using Windows.ApplicationModel.DataTransfer;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Countdown.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    internal sealed partial class LettersView : Page
    {
        public LettersView()
        {
            this.InitializeComponent();
        }

        public LettersViewModel? ViewModel { get; set; }

        private void CopyCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            StringBuilder sb = new StringBuilder();

            foreach (WordItem wordItem in WordList.SelectedItems)
                sb.AppendLine(wordItem.ToString());
             
            if (sb.Length > 0)
            {
                DataPackage dp = new();
                dp.SetText(sb.ToString());
                Clipboard.SetContent(dp);
            }
        }

        private void CopyCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = WordList.SelectedItems.Any();
        }
    }
}
