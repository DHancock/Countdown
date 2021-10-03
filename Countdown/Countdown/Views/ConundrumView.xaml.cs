using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Countdown.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Countdown.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    internal sealed partial class ConundrumView : Page
    {
        public ConundrumView()
        {
            this.InitializeComponent();
        }

        public ConundrumViewModel? ViewModel { get; set; }

        private void DeleteCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = ConundrumList.SelectedItems.Any();
        }

        private void DeleteCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            List<object> items = new List<object>(ConundrumList.SelectedItems);
            IList<ConundrumItem> source = (IList<ConundrumItem>)ConundrumList.ItemsSource;

            foreach (ConundrumItem item in items)
                source.Remove(item);
        }

        private void CopyCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            StringBuilder sb = new StringBuilder();

            foreach (ConundrumItem c in ConundrumList.SelectedItems)
                sb.AppendLine(c.ToString());

            if (sb.Length > 0)
            {
                DataPackage dp = new();
                dp.SetText(sb.ToString());
                Clipboard.SetContent(dp);
            }
        }

        private void CopyCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = ConundrumList.SelectedItems.Any();
        }
    }
}
