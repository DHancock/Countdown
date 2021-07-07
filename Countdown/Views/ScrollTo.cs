using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Countdown.Views
{
    internal class ScrollTo
    {
        public static void SetItem(UIElement element, object value)
        {
            element.SetValue(ItemProperty, value);
        }

        public static object GetItem(UIElement element)
        {
            return element.GetValue(ItemProperty);
        }

        public static readonly DependencyProperty ItemProperty =
            DependencyProperty.RegisterAttached("Item",
                typeof(object),
                typeof(ScrollTo),
                new FrameworkPropertyMetadata(OnScrollToItemPropertyChanged));

        private static void OnScrollToItemPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            if ((e.NewValue is not null) && (source is ListBox list))
            {
                list.SelectedItem = e.NewValue;

                if (list.IsGrouping)
                {
                    // Work around a bug that stops ScrollIntoView() working on Net5.0
                    // see https://github.com/dotnet/wpf/issues/4797

                    _ = list.Dispatcher.BeginInvoke(DispatcherPriority.Loaded,
                                                        new Action(() =>
                                                            {
                                                                list.ScrollIntoView(e.NewValue);
                                                                _ = list.Focus();
                                                            }));
                }
                else
                {
                    list.ScrollIntoView(e.NewValue);
                    _ = list.Focus();
                }

                
            }
        }
    }
}
