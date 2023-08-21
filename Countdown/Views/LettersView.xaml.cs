using Countdown.ViewModels;

namespace Countdown.Views;

internal sealed partial class LettersView : Page
{
    private TreeViewList? treeViewList;
    private TextBox? textBox;
    private LettersViewModel? viewModel;

    public LettersView()
    {
        this.InitializeComponent();

        Loaded += (s, e) =>
        {
            if (textBox is null)
            {
                textBox = FindChild<TextBox>(SuggestionBox);

                if (textBox is not null)
                {
                    textBox.CharacterCasing = CharacterCasing.Lower;
                    textBox.MaxLength = Models.WordModel.cMaxLetters;

                    textBox.BeforeTextChanging += (s, a) =>
                    {
                        if (a.NewText.Length > 0)
                            a.Cancel = a.NewText.Any(c => c is < 'a' or > 'z');
                    };
                }
            }
        };
    }

    public LettersViewModel? ViewModel
    {
        get => viewModel;
        set
        {
            Debug.Assert(value is not null);

            if (!ReferenceEquals(viewModel, value))
            {
                viewModel = value;
                viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.WordList))
            LoadTreeView();
    }

    private void LoadTreeView()
    {
        WordTreeView.RootNodes.Clear();

        if (ViewModel is null || ViewModel.WordList is null)
            return;

        IEnumerable<IGrouping<int, string>> query = from word in ViewModel.WordList
                                                    group word by word.Length into g
                                                    orderby g.Key descending
                                                    select g;

        foreach (IGrouping<int, string> group in query)
        {
            TreeViewNode parent = new TreeViewNode();
            parent.Content = new WordHeading(group.Key);

            foreach (string word in group.OrderBy(w => w))
            {
                TreeViewNode child = new TreeViewNode();
                child.Content = word;
                parent.Children.Add(child);
            }

            WordTreeView.RootNodes.Add(parent);
        }

        if (WordTreeView.RootNodes.Count > 0)
            WordTreeView.Expand(WordTreeView.RootNodes[0]);
    }

    private void CopyCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        TreeViewNode selectedNode = WordTreeView.SelectedNode;

        if (selectedNode is not null)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(selectedNode.Content.ToString());

            foreach (TreeViewNode node in selectedNode.Children)
                sb.AppendLine(node.Content.ToString());

            if (sb.Length > 0)
            {
                DataPackage dp = new DataPackage();
                dp.SetText(sb.ToString());
                Clipboard.SetContent(dp);
            }
        }
    }

    private void CopyCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
    {
        args.CanExecute = WordTreeView.SelectedNode is not null;
    }

    private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            List<string> suitableItems = new List<string>();
            
            if (sender.Text.Length > 0)
            {
                foreach (TreeViewNode parent in WordTreeView.RootNodes)
                {
                    foreach (TreeViewNode child in parent.Children)
                    {
                        string word = (string)child.Content;

                        if (word.StartsWith(sender.Text))
                            suitableItems.Add(word);
                    }
                }
            }

            sender.ItemsSource = suitableItems;
        }
    }

    private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        // the selected item will be a valid existing word
        FindItem((string)args.SelectedItem, Equals);
    }

    private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (string.IsNullOrEmpty(args.QueryText))
            return;

        if (!FindItem(args.QueryText, Equals) &&
            !FindItem(args.QueryText, StartsWith) &&
            !FindItem(args.QueryText, Contains))
        {
            Utils.User32Sound.PlayExclamation();
        }
    }

    private bool FindItem(string target, Func<string, string, bool> compare)
    {
        foreach (TreeViewNode parent in WordTreeView.RootNodes)
        {
            foreach (TreeViewNode child in parent.Children)
            {
                string word = (string)child.Content;

                if (compare(word, target))
                {
                    treeViewList ??= FindChild<TreeViewList>(WordTreeView);

                    if (treeViewList is not null)
                    {
                        WordTreeView.Expand(parent);
                        treeViewList.ScrollIntoView(child);
                        WordTreeView.SelectedNode = child;
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static bool Equals(string a, string b) => a.Equals(b, StringComparison.CurrentCulture);

    private static bool StartsWith(string a, string b) => a.StartsWith(b, StringComparison.CurrentCulture);

    private static bool Contains(string a, string b) => a.Contains(b, StringComparison.CurrentCulture);


    private static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
    {
        int count = VisualTreeHelper.GetChildrenCount(parent);

        for (int index = 0; index < count; index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, index);

            if (child is T target)
                return target;

            T? result = FindChild<T>(child);

            if (result is not null)
                return result;
        }

        return null;
    }

    internal static void MenuFlyout_Opening(object sender, object e)
    {
        MenuFlyout menu = (MenuFlyout)sender;
        int selectedIndex = Settings.Data.ChooseLettersIndex;

        for (int index = 0; index < menu.Items.Count; index++)
            ((RadioMenuFlyoutItem)menu.Items[index]).IsChecked = index == selectedIndex;
    }
}

internal record struct WordHeading(int Count)
{
    public override readonly string? ToString() => $"{Count} letter words";
}

internal sealed class WordTreeTemplateSelector : DataTemplateSelector
{
    public DataTemplate? HeadingTemplate { get; set; }
    public DataTemplate? WordTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object obj)
    {
        bool IsHeading = ((TreeViewNode)obj).Content is WordHeading;

        return IsHeading ? HeadingTemplate : WordTemplate;
    }
}
