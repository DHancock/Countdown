using Countdown.ViewModels;

namespace Countdown.Views;

internal sealed partial class LettersView : Page
{
    private bool firstLoad = true;

    private TreeViewList? treeViewList;
    private LettersViewModel? viewModel;

    public LettersView()
    {
        this.InitializeComponent();

        Loaded += (s, e) =>
        {
            if (firstLoad)
            {
                firstLoad = false;

                TextBox? textBox = FindChild<TextBox>(SuggestionBox);

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

    // Guarantee that there is a one to one mapping between tree view nodes and grouped word list items
    // to enable the scroll in to view feature. If the grouped word list is supplied directly, the 
    // tree view will allocate and recycle tree view nodes as it sees fit.
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
            if ((sender.Text.Length > 0) && (ViewModel is not null))
            {
                List<string> suggestions = new(ViewModel.WordList.Where(x =>
                {
                    if (x.StartsWith(sender.Text, StringComparison.Ordinal))
                        return true;

                    return DistanceFilter(x, sender.Text);
                }));

                suggestions.Sort((a, b) =>
                {
                    int result = a.Length - b.Length;

                    if (result == 0)
                    {
                        bool aStartsWith = a.StartsWith(sender.Text, StringComparison.Ordinal);
                        bool bStartsWith = b.StartsWith(sender.Text, StringComparison.Ordinal);

                        if (aStartsWith == bStartsWith)
                            return string.Compare(a, b);

                        else if (aStartsWith)
                            return -1;

                        return +1;
                    }

                    return result;
                });

                sender.ItemsSource = suggestions;
            }
            else
            {
                sender.ItemsSource = null;
            }
        }
    }

    private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        // the selected item is an existing word
        FindTreeViewItem((string)args.SelectedItem);
    }

    private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (string.IsNullOrEmpty(args.QueryText))
            return;

        if (!FindTreeViewItem(args.QueryText) && !FindTreeViewItem(FindClosestItem(args.QueryText)))
            Utils.User32Sound.PlayExclamation();
    }

    private bool FindTreeViewItem(string target)
    {
        foreach (TreeViewNode parent in WordTreeView.RootNodes)
        {
            if (target.Length != ((WordHeading)parent.Content).Count)
                continue;

            foreach (TreeViewNode child in parent.Children)
            {
                string word = (string)child.Content;

                if (string.Equals(word, target, StringComparison.Ordinal))
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

    private static bool DistanceFilter(string word, string target)
    {
        return word.Length > 0
                && target.Length > 0
                && (word[0] == target[0])
                && (Math.Abs(word.Length - target.Length) <= 1)
                && (DamerauLevenshteinDistance(word, target) <= (target.Length <= 6 ? 1 : 2));
    }

    private string FindClosestItem(string target)
    {
        if ((target.Length == 0) || (ViewModel is null))
            return string.Empty;

        List<string> subset = ViewModel.WordList.Where(s => DistanceFilter(s, target)).ToList();

        string foundWord = string.Empty;

        foreach (string word in subset)
        {
            if (word == target)
                return word;

            if ((foundWord.Length < word.Length) || ((foundWord.Length == word.Length) && (string.Compare(foundWord, word) >= 0)))
                foundWord = word;
        }

        return foundWord;
    }

    private static int DamerauLevenshteinDistance(string s1, string s2)
    {
        const int cThreshold = 100;
        int size = (s1.Length + 1) * (s2.Length + 1);

        if (size <= cThreshold)
            return DamerauLevenshteinDistance(s1, s2, stackalloc int[size]);

        int[] buffer = ArrayPool<int>.Shared.Rent(size);
        int distance = DamerauLevenshteinDistance(s1, s2, buffer);
        ArrayPool<int>.Shared.Return(buffer);
        return distance;
    }

    private static int DamerauLevenshteinDistance(string s1, string s2, Span<int> buffer)
    {
        int width = s1.Length + 1;
        int height = s2.Length + 1;

        if ((width * height) > buffer.Length)
            throw new ArgumentException("buffer too small");

        int idx(int x, int y) => x + (y * width);

        for (int i = 0; i < width; i++)
            buffer[idx(i, 0)] = i;

        for (int j = 0; j < height; j++)
            buffer[idx(0, j)] = j;

        for (int i = 1; i < width; i++)
        {
            for (int j = 1; j < height; j++)
            {
                int cost = s1[i - 1] == s2[j - 1] ? 0 : 1;

                int deletionCost = buffer[idx(i - 1, j)] + 1;
                int insertionCost = buffer[idx(i, j - 1)] + 1;
                int substitutionCost = buffer[idx(i - 1, j - 1)] + cost;

                int distance = Math.Min(deletionCost, Math.Min(insertionCost, substitutionCost));

                if ((i > 1) && (j > 1) && (s1[i - 1] == s2[j - 2]) && (s1[i - 2] == s2[j - 1])) // adjacent transpositions
                    buffer[idx(i, j)] = Math.Min(distance, buffer[idx(i - 2, j - 2)] + cost);
                else
                    buffer[idx(i, j)] = distance;
            }
        }

        return buffer[idx(s1.Length, s2.Length)];
    }

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

    protected override DataTemplate? SelectTemplateCore(object obj, DependencyObject container)
    {
        bool IsHeading = ((TreeViewNode)obj).Content is WordHeading;

        return IsHeading ? HeadingTemplate : WordTemplate;
    }
}
