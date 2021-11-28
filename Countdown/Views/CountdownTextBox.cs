namespace Countdown.Views;

internal class CountdownTextBox : TextBox
{

    public enum ContentType { Number, Letter }

    public enum AutoTabType { Off, TabIfErrorFree, AlwaysTab }

    public CountdownTextBox() : base()
    {
        IsSpellCheckEnabled = false;

        BeforeTextChanging += CountdownTextBox_BeforeTextChanging;
        TextChanged += CountdownTextBox_TextChanged;
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
            new PropertyMetadata(ContentType.Letter)); // default

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
    /// defines what characters are allowed
    /// </summary>
    private Func<char, bool> GetPredicate()
    {
        if (ContentStyle == ContentType.Number)
            return c => c is >= '0' and <= '9';

        return c => c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z');
    }

    protected async override void OnDragOver(DragEventArgs e)
    {
        bool dragTargetIsValid = false;

        if (e.DataView.Contains(StandardDataFormats.Text))
        {
            string data = await e.DataView.GetTextAsync();

            if ((MaxLength == 0) || (Text.Length + data.Length) <= MaxLength)
                dragTargetIsValid = data.All(GetPredicate());
        }

        e.AcceptedOperation = dragTargetIsValid ? DataPackageOperation.Copy : DataPackageOperation.None;
    }

    protected async override void OnDrop(DragEventArgs e)
    {
        // TODO: Is this correct? What is the default behavior?

        if (e.DataView.Contains(StandardDataFormats.Text))
        {
            string data = await e.DataView.GetTextAsync();

            if (SelectionLength == 0)
                Text += data;
            else
                Text = Text.Insert(SelectionStart, data);
        }
    }

    protected override void OnGotFocus(RoutedEventArgs e)
    {
        base.OnGotFocus(e);
        SelectAll();
    }

    protected override void OnPreviewKeyDown(KeyRoutedEventArgs e)
    {
        e.Handled = e.Key == VirtualKey.Space;
    }

    private void CountdownTextBox_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
    {
        args.Cancel = args.NewText.All(GetPredicate()) == false;
    }

    /// <summary>
    /// After the text changes move focus to the next control if required
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void CountdownTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if ((sender is TextBox tb) &&
            (tb.FocusState != FocusState.Unfocused) &&
            (tb.MaxLength > 0) &&
            (tb.Text.Length == tb.MaxLength) &&
            ((AutoTabStyle == AutoTabType.AlwaysTab)))//|| ((AutoTabStyle == AutoTabType.TabIfErrorFree) && !Validation.GetHasError(tb)))
        {
            try
            {
                FindNextElementOptions fneo = new() { SearchRoot = tb.XamlRoot.Content };
                _ = FocusManager.TryMoveFocus(FocusNavigationDirection.Next, fneo);
            }
            catch (Exception)
            {

            }
        }
    }
}
