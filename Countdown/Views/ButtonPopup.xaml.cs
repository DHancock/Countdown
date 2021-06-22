using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Countdown.Views
{
    /// <summary>
    /// Interaction logic for ButtonPopup.xaml
    /// </summary>
    public partial class ButtonPopup : UserControl
    {

        /// <summary>
        /// the selected index in the drop down list 
        /// </summary>
        public int SelectedIndex
        {
            get { return (int)GetValue(SelectedIndexProperty); }
            set { SetValue(SelectedIndexProperty, value); }
        }


        public static readonly DependencyProperty SelectedIndexProperty =
                DependencyProperty.Register(nameof(SelectedIndex),
                typeof(int),
                typeof(ButtonPopup),
                new PropertyMetadata(-1, OnSelectedIndexPropertyChanged));



        private static void OnSelectedIndexPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((d is ButtonPopup control) && (e.NewValue is int selectedIndex))
            {
                IEnumerator itemsEnumerator = control.popup_items_control?.ItemsSource?.GetEnumerator();

                if (itemsEnumerator != null)
                {
                    int index = 0;

                    while (itemsEnumerator.MoveNext())
                    {
                        if (itemsEnumerator.Current is Control item)
                        {
                            if (index++ == selectedIndex)
                            {
                                item.Background = SystemColors.MenuBrush;
                                item.BorderBrush = SystemColors.ControlLightBrush;
                            }
                            else
                            {
                                item.Background = SystemColors.WindowBrush;
                                item.BorderBrush = SystemColors.WindowBrush;
                            }
                        }
                    }
                }
            }
        }
        



        /// <summary>
        /// Controls the drop downs visibility
        /// </summary>
        public bool IsPopupOpen
        {
            get { return (bool)GetValue(IsPopupOpenProperty); }
            set { SetValue(IsPopupOpenProperty, value); }
        }


        public static readonly DependencyProperty IsPopupOpenProperty =
                DependencyProperty.Register(nameof(IsPopupOpen),
                typeof(bool),
                typeof(ButtonPopup),
                new PropertyMetadata(false));
        




        /// <summary>
        /// The choose buttons command 
        /// </summary>
        public ICommand ChooseCommand
        {
            get { return (ICommand)GetValue(ChooseCommandProperty); }
            set { SetValue(ChooseCommandProperty, value); }
        }


        public static readonly DependencyProperty ChooseCommandProperty =
                DependencyProperty.Register(nameof(ChooseCommand),
                typeof(ICommand),
                typeof(ButtonPopup),
                new PropertyMetadata(null));


      

        /// <summary>
        /// Expose the drop downs ItemsSource property
        /// </summary>
        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }



        public static readonly DependencyProperty ItemsSourceProperty =
                DependencyProperty.Register(nameof(ItemsSource),
                typeof(IEnumerable),
                typeof(ButtonPopup),
                new PropertyMetadata(null, OnItemsSourceChanged));




        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((d is ButtonPopup control) && (control.popup_items_control != null) && (e.NewValue is IEnumerable items))
            {
                int index = 0;
                List<Button> list = new List<Button>();

                IEnumerator itemsEnumerator = items.GetEnumerator();

                while (itemsEnumerator.MoveNext())
                {
                    Button button = new Button()
                    {
                        HorizontalContentAlignment = HorizontalAlignment.Left,
                        Background = SystemColors.WindowBrush,
                        BorderBrush = SystemColors.WindowBrush,
                        Padding = new Thickness(8, 1, 8, 1),
                        Tag = index++,
                        Content = itemsEnumerator.Current
                    };

                    button.Click += (s, a) =>
                    {
                        control.IsPopupOpen = false;
                        control.SelectedIndex = (int)button.Tag;
                    };
                    
                    list.Add(button);
                }

                control.popup_items_control.ItemsSource = list;
            }
        }






        public ButtonPopup()
        {
            InitializeComponent();

            // add event handlers to the popup so it mimics a combo box drop down
            popup_part.Opened += Popup_part_Opened;
            popup_part.PreviewKeyDown += Popup_part_PreviewKeyDown;
            popup_part.PreviewMouseLeftButtonDown += Popup_part_PreviewMouseLeftButtonDown;
        }



        private void Popup_part_Opened(object sender, EventArgs e)
        {
            // bump the focus to the first focusable item within the popup
            if ((sender is Popup popup) && (popup.Child != null))
                popup.Child.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }




        /// <summary>
        /// Stops mouse clicks being passed to the placement target UIElement
        /// if the popup is closed. The pop up is closed on a mouse down but
        /// controls are activated on mouse ups which would then reopen
        /// the pop up.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Popup_part_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if ((sender is Popup popup) && (e != null) && (popup.PlacementTarget != null))
            {
                if (!popup.IsOpen)  // the pop up has already been closed
                {
                    // get the mouse location relative to the placement target
                    Point pt = e.GetPosition(popup.PlacementTarget);

                    // if the placement target is to be the hit object, block the event
                    HitTestResult result = VisualTreeHelper.HitTest(popup.PlacementTarget, pt);

                    e.Handled = result != null;
                }
            }
        }


        private void Popup_part_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((sender is Popup popup) && (e != null))
            {
                if (e.Key == Key.Escape)
                {
                    popup.IsOpen = false;
                    e.Handled = true;
                }
            }
        }
    }
}
