using Countdown.ViewModels;

namespace Countdown.Views;

internal sealed partial class LettersView : Page
{
    private TreeViewList? treeViewList;
    private LettersViewModel? viewModel;

    public LettersView()
    {
        this.InitializeComponent();

        Loaded += (s, e) =>
        {
            LoadTreeView();
            SuggestionBox.Text = viewModel?.SuggestionText;
        };

        Unloaded += (s, e) =>
        {
            Debug.Assert(viewModel is not null);
            viewModel.SuggestionText = SuggestionBox.Text;
        };
    }

    public LettersViewModel? ViewModel
    {
        get => viewModel;
        set
        {
            Debug.Assert(value is not null);
            viewModel = value;
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
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
            parent.Content = new TreeViewWordItem(group.Key);

            foreach (string word in group.OrderBy(w => w))
            {
                TreeViewNode child = new TreeViewNode();
                child.Content = new TreeViewWordItem(word);
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
            string text = Filter(sender.Text.ToLower());

            if (text.Length > 0)
            {
                foreach (TreeViewNode parent in WordTreeView.RootNodes)
                {
                    foreach (TreeViewNode child in parent.Children)
                    {
                        string word = ((TreeViewWordItem)child.Content).Text;

                        if (word.Contains(text))
                            suitableItems.Add(word);
                    }
                }
            }

            sender.ItemsSource = suitableItems;
        }
    }

    private static string Filter(string text)
    {
        // intercepting key events isn't allowed in an AutoSuggestBox
        char[] output = new char [text.Length];
        int index = 0;

        foreach (char c in text)
        {
            if (c >= 'a' && c <= 'z')
                output[index++] = c;
        }

        return new string(output, 0, index);
    }

    private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        // the selected item will be a valid existing word
        FindItem(args.SelectedItem.ToString());
    }

    private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (!FindItem(Filter(args.QueryText.ToLower())))
            Utils.User32Sound.PlayExclamation();
    }

    private bool FindItem(string? target)
    {
        if (target is null || target.Length < Models.WordDictionary.cMinLetters || target.Length > Models.WordDictionary.cMaxLetters)
            return false;

        foreach (TreeViewNode parent in WordTreeView.RootNodes)
        {
            foreach (TreeViewNode child in parent.Children)
            {
                string word = ((TreeViewWordItem)child.Content).Text;

                if (word.Length != target.Length)
                    break;

                if (string.Equals(word, target, StringComparison.CurrentCulture))
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
}

internal sealed class TreeViewWordItem
{
    public bool IsHeading { get; } = false;
    public string Text { get; }

    public TreeViewWordItem(string text)
    {
        Text = text;
    }

    public TreeViewWordItem(int count)
    {
        Text = $"{count} letter words";
        IsHeading = true;
    }

    public override string? ToString()
    {
        return Text;
    }
}

internal class WordItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? WordHeadingTemplate { get; set; }
    public DataTemplate? WordItemTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object obj)
    {
        Debug.Assert(obj is TreeViewNode);
        Debug.Assert(((TreeViewNode)obj).Content is TreeViewWordItem);

        TreeViewWordItem item = (TreeViewWordItem)((TreeViewNode)obj).Content;

        return item.IsHeading ? WordHeadingTemplate : WordItemTemplate;
    }
}
