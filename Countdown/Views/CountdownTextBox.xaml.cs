namespace Countdown.Views
{
    public sealed partial class CountdownTextBox : UserControl
    {
        public enum ContentType { Number, Letter }
        public enum AutoTabType { Off, TabIfErrorFree, AlwaysTab }

        public CountdownTextBox()
        {
            this.InitializeComponent();

            tb.IsSpellCheckEnabled = false;
            tb.AllowDrop = false;
            tb.IsTextPredictionEnabled = false;
            tb.IsColorFontEnabled = false;

            SetContentProperties(tb, Contents);
            SetReadOnlyProperties(tb, IsReadOnly);

            tb.TextChanged += Tb_TextChanged;
            tb.BeforeTextChanging += Tb_BeforeTextChanging;
            tb.PreviewKeyDown += Tb_PreviewKeyDown;
            tb.GotFocus += Tb_GotFocus;
        }


        public event TextChangedEventHandler TextChanged
        {
            add { tb.TextChanged += value; }
            remove { tb.TextChanged -= value; }
        }


        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text),
                typeof(string),
                typeof(CountdownTextBox),
                new PropertyMetadata(string.Empty, TextPropertyChanged));

        private static void TextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TextBox tb = ((CountdownTextBox)d).tb;
            string newText = (string)e.NewValue;

            if (string.CompareOrdinal(tb.Text, newText) != 0)
            {
                tb.Text = newText;
            }
        }


        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(nameof(IsReadOnly),
                typeof(bool),
                typeof(CountdownTextBox),
                new PropertyMetadata(false, IsReadOnlyPropertyChanged));

        private static void IsReadOnlyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SetReadOnlyProperties(((CountdownTextBox)d).tb, (bool)e.NewValue);
        }

        private static void SetReadOnlyProperties(TextBox textBox, bool isReadOnly)
        {
            textBox.IsReadOnly = isReadOnly;
            textBox.IsHitTestVisible = !isReadOnly;
            textBox.IsTabStop = !isReadOnly;
        }

        /// <summary>
        /// Defines which characters are allowed, letters or numbers.
        /// </summary>
        public ContentType Contents
        {
            get { return (ContentType)GetValue(ContentsProperty); }
            set { SetValue(ContentsProperty, value); }
        }

        public static readonly DependencyProperty ContentsProperty =
            DependencyProperty.Register(nameof(Contents),
                typeof(ContentType),
                typeof(CountdownTextBox),
                new PropertyMetadata(ContentType.Number, ContentsPropertyChanged));

        private static void ContentsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        { 
            SetContentProperties(((CountdownTextBox)d).tb, (ContentType)e.NewValue);
        }

        private static void SetContentProperties(TextBox textBox, ContentType content)
        {
            if (content == ContentType.Letter)
            {
                textBox.MaxLength = 1;
                textBox.MinWidth = 30;
            }
            else
            {
                textBox.MaxLength = 3;
                textBox.MinWidth = 42;
            }
        }

        /// <summary>
        /// Defines if auto tabbing is off or conditionally on. 
        /// </summary>
        public AutoTabType AutoTab
        {
            get { return (AutoTabType)GetValue(AutoTabProperty); }
            set { SetValue(AutoTabProperty, value); }
        }

        public static readonly DependencyProperty AutoTabProperty =
            DependencyProperty.Register(nameof(AutoTab),
                typeof(AutoTabType),
                typeof(CountdownTextBox),
                new PropertyMetadata(AutoTabType.AlwaysTab));


        public string ErrorToolTipText
        {
            get { return (string)GetValue(ErrorToolTipTextProperty); }
            set { SetValue(ErrorToolTipTextProperty, value); }
        }

        public static readonly DependencyProperty ErrorToolTipTextProperty =
            DependencyProperty.Register(nameof(ErrorToolTipText),
                typeof(string),
                typeof(CountdownTextBox),
                new PropertyMetadata(string.Empty, ErrorToolTipTextPropertyChanged));

        private static void ErrorToolTipTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CountdownTextBox ctb = (CountdownTextBox)d;
            string errorText = (string)e.NewValue;

            bool noErrors = string.IsNullOrWhiteSpace(errorText);

            ToolTipService.SetToolTip(ctb, noErrors ? null : errorText);
            bool stateFound = VisualStateManager.GoToState(ctb, noErrors ? "ErrorInvisible" : "ErrorVisible", false);
            Debug.Assert(stateFound);
        }

        private void Tb_GotFocus(object sender, RoutedEventArgs e)
        {
            tb.SelectAll();
        }

        private void Tb_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = e.Key == VirtualKey.Space;
        }

        private void Tb_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            if (!IsReadOnly)
            {
                if (Contents == ContentType.Number)
                {
                    args.Cancel = !args.NewText.All(c => c is >= '0' and <= '9');
                }
                else
                {
                    args.Cancel = !args.NewText.All(c => c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z'));
                }
            }
        }

        /// <summary>
        /// After the text changes move focus to the next control if required
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Tb_TextChanged(object sender, TextChangedEventArgs e)
        {
            Text = tb.Text;

            if ((tb.FocusState != FocusState.Unfocused) &&
                (tb.MaxLength > 0) &&
                (tb.Text.Length == tb.MaxLength) &&
                ((AutoTab == AutoTabType.AlwaysTab) || ((AutoTab == AutoTabType.TabIfErrorFree) && string.IsNullOrWhiteSpace(ErrorToolTipText))))
            {
                try
                {
                    FindNextElementOptions fneo = new FindNextElementOptions() 
                    {
                        SearchRoot = tb.XamlRoot.Content 
                    };

                    bool moved = FocusManager.TryMoveFocus(FocusNavigationDirection.Next, fneo);
                    Debug.Assert(moved);
                }
                catch (Exception ex)
                {
                    Debug.Fail(ex.Message);
                }
            }            
        }
    }
}
