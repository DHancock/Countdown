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

    private void ConundrumList_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        // handle the list's context menu items keyboard accelerators here because if it was left to  
        // the api they would only be active after the context menu has been opened for the first time.

        if (ConundrumList.SelectedItems.Count > 0)
        {
            if (e.Key == VirtualKey.Delete)
            {
                ViewModel?.DeleteItems(ConundrumList.SelectedItems);
            }
            else if ((e.Key == VirtualKey.C) && Utils.IsControlKeyDown())
            {
                CopyItems(ConundrumList.SelectedItems);
            }
        }
        else if ((ConundrumList.SelectedItems.Count != ConundrumList.Items.Count) && (e.Key == VirtualKey.A) && Utils.IsControlKeyDown())
        {
            ConundrumList.SelectRange(new ItemIndexRange(0, (uint)ConundrumList.Items.Count));
        }
    }

    private void CopyCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
    {
        args.CanExecute = ConundrumList.SelectedItems.Count > 0;
    }

    private void CopyCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        CopyItems(ConundrumList.SelectedItems);
    }

    private void SelectAllCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
    {
        args.CanExecute = ConundrumList.SelectedItems.Count != ConundrumList.Items.Count;
    }

    private void SelectAllCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        ConundrumList.SelectRange(new ItemIndexRange(0, (uint)ConundrumList.Items.Count));
    }

    private void DeleteCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
    {
        args.CanExecute = ConundrumList.SelectedItems.Count > 0;
    }

    private void DeleteCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        ViewModel?.DeleteItems(ConundrumList.SelectedItems);
    }
}
