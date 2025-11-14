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
        List<(int index, object item)> indexedList = new(ConundrumList.Items.Count);

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

    private void CopyMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is ConundrumItem ci)
        {
            if (ConundrumList.SelectedItems.Contains(ci))
            {
                CopyItems(ConundrumList.SelectedItems);
            }
            else
            {
                CopyItems([ci]);
            }
        }
    }

    private void DeleteMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is ConundrumItem ci)
        {
            if (ConundrumList.SelectedItems.Contains(ci))
            {
                ViewModel?.DeleteItems(ConundrumList.SelectedItems);
            }
            else
            {
                ViewModel?.DeleteItems([ci]);
            }
        }
    }

    private void ConundrumList_KeyUp(object sender, KeyRoutedEventArgs e)
    {
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
    }
}
