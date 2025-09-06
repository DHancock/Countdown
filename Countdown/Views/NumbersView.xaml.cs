using Countdown.Utilities;
using Countdown.ViewModels;

namespace Countdown.Views;

internal sealed partial class NumbersView : Page, IPageItem
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
        {
            sb.AppendLine(equationItem.ToString());
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
        args.CanExecute = EquationList.SelectedItems.Count > 0;
    }

    internal static void MenuFlyout_Opening(object sender, object e)
    {
        MenuFlyout menu = (MenuFlyout)sender;
        int selectedIndex = Settings.Instance.ChooseNumbersIndex;

        for (int index = 0; index < menu.Items.Count; index++)
        {
            ((RadioMenuFlyoutItem)menu.Items[index]).IsChecked = index == selectedIndex;
        }
    }

    public int PassthroughCount => 11;

    public void AddPassthroughContent(in RectInt32[] rects)
    {
        int index = 0;

        foreach (UIElement element in TileGrid.Children)
        {
            rects[index++] = Utils.GetPassthroughRect(element); // 6
        }

        rects[index++] = Utils.GetPassthroughRect(TargetCTB);

        foreach (UIElement element in ButtonGrid.Children)
        {
            rects[index++] = Utils.GetPassthroughRect(element); // 3
        }

        rects[index++] = Utils.GetPassthroughRect(EquationList);

        Debug.Assert(index == PassthroughCount);
    }
}
