using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Countdown.Views
{
    class ScrollListView : ListView
    {
       
        public object ScrollToItem
        {
            get { return GetValue(ScrollToItemProperty); }
            set { SetValue(ScrollToItemProperty, value); }
        }


        public static readonly DependencyProperty ScrollToItemProperty =
            DependencyProperty.Register(nameof(ScrollToItem),
                typeof(object),
                typeof(ScrollListView),
                new PropertyMetadata(null, OnScrollToItemPropertyChanged));



        private static void OnScrollToItemPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            if ((source is ScrollListView listView) && (e.NewValue != null))
            {
                ICollectionView view = CollectionViewSource.GetDefaultView(listView.ItemsSource);

                if (view.MoveCurrentTo(e.NewValue))
                {
                    listView.SelectedItem = e.NewValue;

                    // dastardly hack - wait until the items in the recently expanded group have 
                    // been created before scrolling the item into view. (Or at least add it to
                    // the queue after the create event has been, if that's whats happening. The
                    // duration of the delay may be immaterial.)
                    // 
                    // This method is called when a bound property, a list item is updated. It's updated
                    // immediately after the item's group has been expanded via another property bound to
                    // the expander. The problem shows up when the item's group hasn't previously been
                    // expanded and if it was expanded the item wouldn't then be visible without scrolling.
                    // When I say visible that may mean outside the virtualizing bounds. The list view 
                    // VirtualizationMode makes no difference, nor does using a simple StackPanel for the
                    // items panel. Calling ScrollIntoView() directly worked in Net4.5
#if false
                    listView.ScrollIntoView(e.NewValue);
#else
                    Task t = new Task(async () =>
                    {
                        await Task.Delay(100);
                        _ = listView.Dispatcher.BeginInvoke(new Action(() => listView.ScrollIntoView(e.NewValue)));
                    });

                    t.Start();
#endif
                }
            }
        }
    }
}
