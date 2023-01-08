using Countdown.ViewModels;

namespace Countdown.Views;

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
        args.CanExecute = EquationList.SelectedItems.Any();
    }

    private void MenuFlyout_Opening(object sender, object e)
    {
        MenuFlyout menu = (MenuFlyout)sender;
        int selectedIndex = Settings.Data.ChooseNumbersIndex;

        for (int index = 0; index < menu.Items.Count; index++)
            ((RadioMenuFlyoutItem)(menu.Items[index])).IsChecked = index == selectedIndex;
    }
}
