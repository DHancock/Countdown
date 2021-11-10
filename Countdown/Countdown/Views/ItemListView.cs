using Microsoft.UI.Xaml.Controls.Primitives;

namespace Countdown.Views;

internal class ItemListView : ListView
{
    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);

        if (element is ListViewItem listItem)
        {
            Binding binding = new()
            {
                Mode = BindingMode.TwoWay,
                Source = item,
                Path = new PropertyPath("IsSelected"),
            };

            listItem.SetBinding(SelectorItem.IsSelectedProperty, binding);
        }
    }
}
