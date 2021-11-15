using Countdown.Utils;
using Countdown.ViewModels;

namespace Countdown.Views;

internal sealed partial class ConundrumView : Page
{
    public ConundrumView()
    {
        this.InitializeComponent();

        Loaded += (s, e) =>
        {
            Debug.Assert(NavigationCacheMode != NavigationCacheMode.Disabled);

            if (ConundrumList.Items.Any() && SystemParameters.WindowAnimationsEnabled)
            {
                // trigger the list add items animation
                ConundrumList.ItemsSource = null;
                Bindings.Update();
            }
        };
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
