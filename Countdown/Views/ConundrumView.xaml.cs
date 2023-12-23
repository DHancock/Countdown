using Countdown.ViewModels;

namespace Countdown.Views;

internal sealed partial class ConundrumView : Page
{
    public ConundrumView()
    {
        this.InitializeComponent();

        Loaded += (s, e) =>
        {
            // defer until after the GroupBox text is rendered when the transform will be correct
            DispatcherQueue.TryEnqueue(() =>
            {
                App.MainWindow?.SetWindowDragRegions();
            });
        };
    }

    public ConundrumViewModel? ViewModel { get; set; }

    private void DeleteCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
    {
        args.CanExecute = ConundrumList.SelectedItems.Count > 0;
    }

    private void DeleteCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        List<object> items = new List<object>(ConundrumList.SelectedItems);
        IList<ConundrumItem> source = (IList<ConundrumItem>)ConundrumList.ItemsSource;

        foreach (object item in items)
        {
            Debug.Assert(item is ConundrumItem);
            source.Remove((ConundrumItem)item);
        }
    }

    private void CopyCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        StringBuilder sb = new StringBuilder();

        foreach (object item in ConundrumList.SelectedItems)
        {
            Debug.Assert(item is ConundrumItem);
            sb.AppendLine($"{((ConundrumItem)item).Conundrum}\t\t{((ConundrumItem)item).Solution}");
        }

        if (sb.Length > 0)
        {
            DataPackage dp = new();
            dp.SetText(sb.ToString());
            Clipboard.SetContent(dp);
        }
    }

    private void CopyCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
    {
        args.CanExecute = ConundrumList.SelectedItems.Count > 0;
    }

    internal static void ContextFlyout_Opening(object sender, object e)
    {
        App.MainWindow?.ClearWindowDragRegions();
    }

    internal static void ContextFlyout_Closed(object sender, object e)
    {
        App.MainWindow?.SetWindowDragRegions();
    }
}
