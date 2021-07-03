using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

namespace Countdown.Views
{
    internal class ScrollListView : ListView
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

                    // Work around a bug that stops ScrollIntoView() working on Net5.0
                    // It appears that the ItemContainerGenerator status has been set to
                    // GeneratorStatus.ContainersGenerated too early for the ScrollIntoView()
                    // method. See:
                    //
                    // https://referencesource.microsoft.com/#PresentationFramework/src/Framework/System/Windows/Controls/ListBox.cs,0e6481e67cd3cffc
                    //
                    // The code below would have been called by ScrollIntoView() if the 
                    // ItemContainerGenerator indicated the expanded group hadn't been generated...

                    _ = listView.Dispatcher.BeginInvoke(DispatcherPriority.Loaded,
                                                        new Action(() => listView.ScrollIntoView(e.NewValue)));
                }
            }
        }
    }
}
