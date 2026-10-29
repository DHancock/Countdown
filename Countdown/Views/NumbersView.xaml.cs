using Countdown.Utilities;
using Countdown.ViewModels;

namespace Countdown.Views;

internal sealed partial class NumbersView : Page, IPageItem
{
    public NumbersView()
    {
        this.InitializeComponent();

        FirstFocusableElement.Loaded += FirstFocusableElement_Loaded;
        TargetCTB.Loaded += TargetCTB_Loaded;
    }

    private void FirstFocusableElement_Loaded(object sender, RoutedEventArgs e)
    {
        // When the first navigated page is shown on app start up this text box gets focus which also selects it's text. 
        // While that may seem ok, the behaviour isn't replicated when navigating to other tabs.
        // This will bump focus back to the parent tab view.
        FirstFocusableElement.Loaded -= FirstFocusableElement_Loaded;
        FocusManager.TryMoveFocus(FocusNavigationDirection.Previous, new FindNextElementOptions() { SearchRoot = XamlRoot.Content });
    }

    private static void TargetCTB_Loaded(object sender, RoutedEventArgs e)
    {
        CountdownTextBox ctb = (CountdownTextBox)sender;
        ctb.Loaded -= TargetCTB_Loaded;

        FixTextBoxContextFlyoutMenuForThemeChange(ctb); // this is the first page loaded

        static void FixTextBoxContextFlyoutMenuForThemeChange(DependencyObject root)
        {
            TextBox? tb = root.FindChild(typeof(TextBox)) as TextBox;

            Debug.Assert(tb is not null);
            Debug.Assert(tb.ContextFlyout is not null);

            if ((tb is not null) && (tb.ContextFlyout is not null))
            {
                // The context flyout is the standard cut/copy/paste menu provided by the sdk.
                // This event handler affects all other TextBox instances, seems that they're
                // all sharing a single context flyout.
                tb.ContextFlyout.Opening += ContextFlyout_Opening;
            }

            static void ContextFlyout_Opening(object? sender, object e)
            {
                if ((sender is TextCommandBarFlyout tcbf) && (tcbf.Target is TextBox tb))
                {
                    foreach (ICommandBarElement icbe in tcbf.SecondaryCommands)
                    {
                        if ((icbe is AppBarButton abb) && (abb.ActualTheme != tb.ActualTheme))
                        {
                            // update the menu item's text colour for theme changes occurring after the context flyout was created
                            // (this will also update each menu item's tool tip colours)
                            abb.RequestedTheme = tb.ActualTheme;
                        }
                    }
                }
            }
        }
    }

    public NumbersViewModel? ViewModel { get; set; }

    private static void CopyItems(IList<object> items)
    {
        List<string> copy = new(items.Cast<string>());
        StringBuilder sb = new StringBuilder().AppendJoin(Environment.NewLine, copy.Order(new EquationComparer()));

        if (sb.Length > 0)
        {
            DataPackage dp = new();
            dp.SetText(sb.ToString());
            Clipboard.SetContent(dp);
        }
    }

    internal static void ChooseMenuFlyout_Opening(object sender, object e)
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

    private void CopyCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
    {
        args.CanExecute = EquationList.IsAnySelected;
    }

    private void CopyCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        CopyItems(EquationList.SelectedItems);
    }

    private void SelectAllCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
    {
        if (EquationList.IsAllSelected)
        {
            sender.Label = "Deselect All";
            sender.Description = "Deselects all content";
        }
        else
        {
            sender.Label = "Select All";
            sender.Description = "Selects all content";
        }
    }

    private void SelectAllCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        if (EquationList.IsAllSelected)
        {
            EquationList.DeselectAll();
        }
        else
        {
            EquationList.SelectAll();
        }
    }

    private void EquationList_ProcessKeyboardAccelerators(UIElement sender, ProcessKeyboardAcceleratorEventArgs args)
    {
        if ((args.Modifiers == VirtualKeyModifiers.Shift) && (args.Key == VirtualKey.F10))
        {
            if (EquationList.IsAnySelected)
            {
                int index = Utils.FindSuitablePlacementIndex(EquationList);

                if ((index >= 0) && (index < EquationList.Items.Count))
                {
                    EquationList.ScrollIntoView(EquationList.Items[index]);

                    if (EquationList.ContainerFromIndex(index) is ListViewItem lvi)
                    {
                        lvi.ContextFlyout.ShowAt(lvi);
                    }
                }
            }

            args.Handled = true;
        }
        else if (EquationList.ContainerFromIndex(((ItemsStackPanel)EquationList.ItemsPanelRoot).FirstVisibleIndex) is ListViewItem lvi)
        {
            // using the first visible index ensures that it's container exists, the menu item commands aren't item specific
            args.Handled = Utils.InvokeMenuItemForKeyboardAccelerator(((MenuFlyout)lvi.ContextFlyout).Items, args.Modifiers, args.Key);
        }
    }

    private void EquationList_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        PointerPoint pointerPoint = e.GetCurrentPoint(null);

        if (pointerPoint.Properties.IsRightButtonPressed)
        {
            object? conundrum = (e.OriginalSource as FrameworkElement)?.DataContext;

            if (EquationList.ContainerFromItem(conundrum) is ListViewItem lvi)
            {
                if (!lvi.IsSelected)
                {
                    EquationList.DeselectAll();
                    lvi.IsSelected = true;
                }
            }
        }
    }
}
