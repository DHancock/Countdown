using Countdown.Utilities;
using Countdown.ViewModels;

namespace Countdown.Views;

internal sealed partial class ConundrumView : Page, IPageItem
{
    public ConundrumView()
    {
        this.InitializeComponent();

        Loaded += ConundrumView_Loaded;
    }

    private void ConundrumView_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= ConundrumView_Loaded;
        ViewModel?.ChooseCommand.Execute(null);

#if DEBUG
        for (int i = 0; i < 20; i++)
        {
            ViewModel?.ChooseCommand.Execute(null);
            ViewModel?.SolveCommand.Execute(null);
        }
#endif
    }

    public ConundrumViewModel? ViewModel { get; set; }

    private void CopyItems(IList<object> items)
    {
        // convert to unsorted list order rather than the order items were selected in
        List<(int index, object item)> indexedList = new(items.Count);

        foreach (object item in items)
        {
            indexedList.Add((ConundrumList.Items.IndexOf(item), item));
        }

        StringBuilder sb = new StringBuilder();

        foreach ((int index, object item) in indexedList.OrderBy(x => x.index))   // convert to list order
        {
            sb.AppendLine(item.ToString());        
        }

        if (sb.Length > 0)
        {
            DataPackage dp = new();
            dp.SetText(sb.ToString());
            Clipboard.SetContent(dp);
        }
    }

    public int PassthroughCount => 13;

    public void AddPassthroughContent(in RectInt32[] rects)
    {
        int index = 0;

        foreach (UIElement element in LettersGrid.Children)
        {
            rects[index++] = Utils.GetPassthroughRect(element); // 9
        }

        foreach (UIElement element in ButtonGrid.Children)
        {
            rects[index++] = Utils.GetPassthroughRect(element); // 3
        }

        rects[index++] = Utils.GetPassthroughRect(ConundrumList);
        Debug.Assert(index == PassthroughCount);
    }

    private void CopyCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
    {
        args.CanExecute = ConundrumList.IsAnySelected;
    }

    private void CopyCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        CopyItems(ConundrumList.SelectedItems);
    }

    private void SelectAllCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
    {
        if (ConundrumList.IsAllSelected)
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
        if (ConundrumList.IsAllSelected)
        {
            ConundrumList.DeselectAll();
        }
        else
        {
            ConundrumList.SelectAll();
        }
    }

    private void DeleteCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
    {
        args.CanExecute = ConundrumList.IsAnySelected;
    }

    private void DeleteCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        ViewModel?.DeleteItems(ConundrumList.SelectedItems);
    }

    private void ConundrumList_ProcessKeyboardAccelerators(UIElement sender, ProcessKeyboardAcceleratorEventArgs args)
    {
        if ((args.Modifiers == VirtualKeyModifiers.Shift) && (args.Key == VirtualKey.F10))
        {
            if (ConundrumList.IsAnySelected)
            {
                int index = Utils.FindSuitablePlacementIndex(ConundrumList);

                if ((index >= 0) && (index < ConundrumList.Items.Count))
                {
                    ConundrumList.ScrollIntoView(ConundrumList.Items[index]);

                    if (ConundrumList.ContainerFromIndex(index) is ListViewItem lvi)
                    {
                        lvi.ContextFlyout.ShowAt(lvi);
                    }
                }
            }

            args.Handled = true;
        }
        else if ((args.Modifiers == VirtualKeyModifiers.Control) && (args.Key == VirtualKey.A))
        {
            // ignore select/deselect all, the sdk's default list view accelerators handle it
            // it isn't clear why this is needed here while other lists work ok without it
            args.Handled = true;
        }
        else if (ConundrumList.ContainerFromIndex(((ItemsStackPanel)ConundrumList.ItemsPanelRoot).FirstVisibleIndex) is ListViewItem lvi)
        {
            // using the first visible index ensures that it's container exists, the menu item commands aren't item specific
            args.Handled = Utils.InvokeMenuItemForKeyboardAccelerator(((MenuFlyout)lvi.ContextFlyout).Items, args.Modifiers, args.Key);
        }
    }

    private void ConundrumList_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        PointerPoint pointerPoint = e.GetCurrentPoint(null);

        if (pointerPoint.Properties.IsRightButtonPressed)
        {
            object? conundrum = (e.OriginalSource as FrameworkElement)?.DataContext;

            if (ConundrumList.ContainerFromItem(conundrum) is ListViewItem lvi)
            {
                if (!lvi.IsSelected)
                {
                    ConundrumList.DeselectAll();
                    lvi.IsSelected = true;
                }
            }
        }
    }
}
