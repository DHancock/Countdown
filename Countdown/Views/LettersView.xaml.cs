using Countdown.ViewModels;

namespace Countdown.Views;

internal sealed partial class LettersView : Page
{
    private TreeViewList? treeViewList;
    private LettersViewModel? viewModel;

    public LettersView()
    {
        this.InitializeComponent();

        Loaded += (s, e) => LoadTreeView();
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
            string[] splitText = sender.Text.ToLower().Split(" ");

            foreach (TreeViewNode parent in WordTreeView.RootNodes)
            {
                foreach (TreeViewNode child in parent.Children)
                {
                    bool found = splitText.All((key) =>
                    {
                        return ((TreeViewWordItem)child.Content).Text.Contains(key);
                    });

                    if (found)
                        suitableItems.Add(((TreeViewWordItem)child.Content).Text);
                }
            }

            if (suitableItems.Count == 0)
                suitableItems.Add("No results found");

            sender.ItemsSource = suitableItems;
        }
    }

    private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        string? target = args.SelectedItem.ToString();

        if (!string.IsNullOrWhiteSpace(target))
            FindItem(target);
    }

    private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        string? target = args.QueryText;

        if (!string.IsNullOrWhiteSpace(target))
            FindItem(target);
    }

    private bool FindItem(string target)
    {
        foreach (TreeViewNode parent in WordTreeView.RootNodes)
        {
            foreach (TreeViewNode child in parent.Children)
            {
                string word = ((TreeViewWordItem)child.Content).Text;

                if (word.Length != target.Length)
                    break;

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
