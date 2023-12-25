using Countdown.ViewModels;

namespace Countdown.Views;

internal sealed partial class NumbersView : Page
{
    private bool firstLoad = true;

    public NumbersView()
    {
        this.InitializeComponent();

        Loaded += (s, e) =>
        {
            if (firstLoad)
            {
                firstLoad = false;
                App.MainWindow?.AddDragRegionEventHandlers(this);
            }

            // defer until after the GroupBox text is rendered when the transform will be correct
            DispatcherQueue.TryEnqueue(() =>
            {
                App.MainWindow?.SetWindowDragRegions();
            });
        };
    }

    public NumbersViewModel? ViewModel { get; set; }

    private void CopyCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        StringBuilder sb = new StringBuilder();

        foreach (object equationItem in EquationList.SelectedItems)
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
        args.CanExecute = EquationList.SelectedItems.Count > 0;
    }

    internal static void MenuFlyout_Opening(object sender, object e)
    {
        MenuFlyout menu = (MenuFlyout)sender;
        int selectedIndex = Settings.Data.ChooseNumbersIndex;

        for (int index = 0; index < menu.Items.Count; index++)
            ((RadioMenuFlyoutItem)menu.Items[index]).IsChecked = index == selectedIndex;
    }
}
