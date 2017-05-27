using System.ComponentModel;
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
            if ((source is ScrollListView listView) && (e != null) && (e.NewValue != null))
            {
                ICollectionView view = CollectionViewSource.GetDefaultView(listView.ItemsSource);

                if (view.MoveCurrentTo(e.NewValue))
                {
                    listView.SelectedItem = e.NewValue;
                    listView.ScrollIntoView(e.NewValue);
                }
            }
        }
    }
}
