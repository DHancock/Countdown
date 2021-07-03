using System;
using System.Windows;
using System.Windows.Controls;
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
            if (e.NewValue is not null)
            {
                ScrollListView listView = (ScrollListView)source;

                listView.SelectedItem = e.NewValue;

                if (listView.IsGrouping)
                {
                    // Work around a bug that stops ScrollIntoView() working on Net5.0
                    // It appears that the ItemContainerGenerator status has been set to
                    // GeneratorStatus.ContainersGenerated too early for the ScrollIntoView()
                    // method when grouping. See:
                    //
                    // https://github.com/dotnet/wpf/blob/2fe5451ed1ff47ff03a741cc56bf201ff3d1acc1/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Controls/ListBox.cs#L123
                    //
                    // The equivalent code below would have been called by ScrollIntoView() if the 
                    // ItemContainerGenerator indicated the expanded group hadn't been generated...

                    _ = listView.Dispatcher.BeginInvoke(DispatcherPriority.Loaded,
                                                        new Action(() => listView.ScrollIntoView(e.NewValue)));
                }
                else
                {
                    listView.ScrollIntoView(e.NewValue);
                }
            }
        }
    }
}
