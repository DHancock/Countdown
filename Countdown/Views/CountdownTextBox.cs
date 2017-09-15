using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Countdown.Views
{
    internal class CountdownTextBox : TextBox
    {
        public enum ContentType { Number, Letter }

        public enum AutoTabType { Off, TabIfErrorFree, AlwaysTab }



        public CountdownTextBox() : base()
        {
            PreviewKeyDown += CountdownTextBox_PreviewKeyDown;
            PreviewTextInput += CountdownTextBox_PreviewTextInput;
            PreviewDragEnter += CountdownTextBox_DragPreviewHandler;
            PreviewDragOver += CountdownTextBox_DragPreviewHandler;
            GotFocus += CountdownTextBox_GotFocus;
            PreviewMouseLeftButtonDown += CountdownTextBox_PreviewMouseLeftButtonDown;
            TextChanged += CountdownTextBox_TextChanged;

            CommandBindings.Add(new CommandBinding(
                ApplicationCommands.Paste,
                PasteCommandExecuted,
                PasteCommandCanExecute));
        }



        /// <summary>
        /// Defines which characters are allowed, letters or numbers.
        /// Its a dependency property so that it can be set in styles.
        /// </summary>
        public ContentType ContentStyle
        {
            get { return (ContentType)GetValue(ContentStyleProperty); }
            set { SetValue(ContentStyleProperty, value); }
        }


        public static readonly DependencyProperty ContentStyleProperty =
            DependencyProperty.Register(nameof(ContentStyle),
                typeof(ContentType),
                typeof(CountdownTextBox),
                new PropertyMetadata(ContentType.Letter)); // default to letters


        

        /// <summary>
        /// defines what characters are allowed
        /// </summary>
        private Func<char, bool> GetPredicate()
        {
            if (ContentStyle == ContentType.Number)
                return c => (c >= '0') && (c <= '9');
            
            return c => ((c >= 'A') && (c <= 'Z')) || ((c >= 'a') && (c <= 'z'));
        }






        /// <summary>
        /// Defines if auto tabbing is off or conditionally on. 
        /// </summary>
        public AutoTabType AutoTabStyle
        {
            get { return (AutoTabType)GetValue(AutoTabStyleProperty); }
            set { SetValue(AutoTabStyleProperty, value); }
        }


        public static readonly DependencyProperty AutoTabStyleProperty =
            DependencyProperty.Register(nameof(AutoTabStyle),
                typeof(AutoTabType),
                typeof(CountdownTextBox),
                new PropertyMetadata(AutoTabType.Off)); // default to off




        /// <summary>
        /// filter out the space bar key 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CountdownTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e != null)
                e.Handled = e.Key == Key.Space;
        }



        /// <summary>
        /// Stops the event if any new characters are not digits. 
        /// This event won't intercept paste or drag events, nor will it be  
        /// called if the space bar is pressed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CountdownTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if ((e != null) && (e.Text != null))
                e.Handled = !e.Text.All(GetPredicate());
        }


        /// <summary>
        /// After the text changes move focus to the next control if required
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CountdownTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if ((sender is TextBox tb) && (tb.MaxLength > 0) && (tb.Text?.Length == tb.MaxLength))
            {
                if ((AutoTabStyle == AutoTabType.AlwaysTab) || ((AutoTabStyle == AutoTabType.TabIfErrorFree) && !Validation.GetHasError(tb)))
                    tb.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }



        /// <summary>
        /// Stops the text box advertising itself as a drop target if max length will
        /// be exceeded or if the text to be dropped contains invalid characters.
        /// Called if the text box property AllowDrop is true, the default value.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CountdownTextBox_DragPreviewHandler(Object sender, DragEventArgs e)
        {
            if ((sender is TextBox tb) && (tb.Text != null) && (e != null))
            {
                bool dragTargetIsInvalid = true;

                if (e.Data.GetDataPresent(DataFormats.StringFormat))
                {
                    string dataString = (string)e.Data.GetData(DataFormats.StringFormat);

                    if ((tb.MaxLength == 0) || (tb.Text.Length + dataString.Length) <= tb.MaxLength)
                        dragTargetIsInvalid = !dataString.All(GetPredicate());
                }

                if (dragTargetIsInvalid)
                {
                    e.Effects = DragDropEffects.None;
                    e.Handled = true;
                }
            }
        }


        /// <summary>
        /// Handles the GotFocus event of the TextBox control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CountdownTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
                tb.SelectAll();
        }




        /// <summary>
        /// Intercept the left mouse down event. Set focus to this text box and stop  
        /// further processing but only if the text box doesn't currently have 
        /// focus. This stops the text being selected on mouse down but deselected when
        /// the mouse is subsequently released. The mouse click would have set the claret 
        /// position removing any selection. See the GotFocus event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CountdownTextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if ((sender is TextBox tb) && (e != null))
            {
                if (!tb.IsKeyboardFocused)
                {
                    e.Handled = true;
                    tb.Focus();
                }
            }
        }


        /// <summary>
        /// Handle the paste event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void PasteCommandExecuted(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
                tb.Paste();
        }



        /// <summary>
        /// Stops the paste command being active if the text box max length will
        /// be exceeded or if the text to be pasted contains invalid characters
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void PasteCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if ((sender is TextBox tb) && (tb.Text != null) && (e != null))
            {
                bool pasteIsValid = false;

                if (Clipboard.ContainsText())
                {
                    string dataString = Clipboard.GetText();

                    if ((tb.MaxLength == 0) || ((tb.Text.Length - tb.SelectionLength) + dataString.Length) <= tb.MaxLength)
                        pasteIsValid = dataString.All(GetPredicate());
                }

                e.CanExecute = pasteIsValid;
                e.Handled = true;
            }
        }
    }
}
