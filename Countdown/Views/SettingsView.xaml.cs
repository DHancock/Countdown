using Countdown.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Diagnostics.CodeAnalysis;

namespace Countdown.Views
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : UserControl
    {

        public SettingsView()
        {
            InitializeComponent();
        }


        /// <summary>
        /// The text edit boxes in the list consume the keys used to navigate the 
        /// list items. This preview event handles list navigation before they get
        /// to the text box. Note this event handler will only work as expected 
        /// if the list isn't grouping.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private void SettingsListPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((e != null) && (sender is ListView list))
            {
                if (list.IsGrouping)
                    throw new InvalidOperationException(nameof(sender));

                int nextIndex = int.MinValue;

                switch (e.Key)
                {
                    case Key.RightShift:
                    case Key.LeftShift:
                    case Key.Tab:
                        {
                            bool shiftDown = (e.Key == Key.LeftShift) || (e.Key == Key.RightShift);
                            bool tabDown = e.Key == Key.Tab;

                            if (shiftDown)
                                tabDown = Keyboard.IsKeyDown(Key.Tab);
                            else if (tabDown)
                                shiftDown = e.KeyboardDevice.Modifiers == ModifierKeys.Shift;

                            if (tabDown)
                            {
                                nextIndex = list.SelectedIndex + ((shiftDown) ? -1 : 1);

                                if ((shiftDown && (nextIndex < 0)) || (!shiftDown && (nextIndex == list.Items.Count)))
                                {
                                    FocusNavigationDirection direction = (shiftDown) ? FocusNavigationDirection.Previous : FocusNavigationDirection.Next;
                                    // moving focus will also change the lists selected index to 0
                                    list.MoveFocus(new TraversalRequest(direction));
                                    return;
                                }
                            }

                            break;
                        }

                    case Key.Up:
                        nextIndex = list.SelectedIndex - 1; break;
                    case Key.Down:
                        nextIndex = list.SelectedIndex + 1; break;
                    case Key.Home:
                        nextIndex = 0; break;
                    case Key.End:
                        nextIndex = list.Items.Count - 1; break;
                    case Key.PageUp:
                    case Key.PageDown:
                        {
                            ScrollViewer scrollViewer = GetVisualChild<ScrollViewer>(list);

                            if (scrollViewer != null)
                            {
                                int pageSize = (int)scrollViewer.ViewportHeight - 1;
                                nextIndex = list.SelectedIndex + ((e.Key == Key.PageDown) ? pageSize : -pageSize);
                            }

                            break;
                        }
                }

                if (nextIndex > int.MinValue)
                {
                    if (nextIndex < 0)
                        nextIndex = 0;
                    else if (nextIndex >= list.Items.Count)
                        nextIndex = list.Items.Count - 1;

                    list.SelectedIndex = nextIndex;
                    list.ScrollIntoView(list.SelectedItem);

                    e.Handled = true;
                }
            }
        }



        private static T GetVisualChild<T>(Visual parent) where T : Visual
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); ++i)
            {
                if (VisualTreeHelper.GetChild(parent, i) is Visual child)
                {
                    if (child is T item)
                        return item;

                    item = GetVisualChild<T>(child);

                    if (item != null)
                        return item;
                }
            }

            return null;
        }



        private void SettingsListGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if ((e != null) && (sender is ListView list) && (list.SelectedItem != null))
                list.ScrollIntoView(list.SelectedItem);
        }




        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "bogus, its defined in xaml")]
        private void FrequencyTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is CountdownTextBox ctb)
            {
                // if invalid revert to the last good value stored in the tag
                ValidationResult vr = new FrequencyValidationRule().Validate(ctb.Text, null);

                if (!vr.IsValid)
                    ctb.Text = ctb.Tag.ToString();
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification="bogus, its defined in xaml")]
        private void FrequencyTextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is CountdownTextBox ctb)
            {
                // store the last good value in the tag, just in case it needs to be reverted
                if (Int32.TryParse(ctb.Text, out int i)) 
                    ctb.Tag = i;
            }
        }
    }


    public class FrequencyValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            if ((value is string s) && (s.Length > 0) && Int32.TryParse(s, out int i) && (i >= LetterTile.cMinFrequency) && (i <= LetterTile.cMaxFrequency))
                return new ValidationResult(true, null);

            return new ValidationResult(false, $"Please enter a value between {LetterTile.cMinFrequency} and {LetterTile.cMaxFrequency}.");
        }
    }
}
