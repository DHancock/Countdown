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
                    // been created before scrolling the item into view. 
                    Task t = new Task(async () =>
                    {
                        await Task.Delay(100);
                        _ = listView.Dispatcher.BeginInvoke(new Action(() => listView.ScrollIntoView(e.NewValue)));
                    });

                    t.Start();
                }
            }
        }
    }
}
