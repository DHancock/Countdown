using Countdown.Utils;
using Countdown.ViewModels;

namespace Countdown.Views;

internal sealed partial class NumbersView : Page
{
    public NumbersView()
    {
        this.InitializeComponent();

        Loaded += (s, e) =>
        {
            Debug.Assert(NavigationCacheMode != NavigationCacheMode.Disabled);

            if (EquationList.Items.Any() && SystemParameters.WindowAnimationsEnabled)
            {
                // trigger the list add items animation
                EquationList.ItemsSource = null;
                Bindings.Update();
            }
        };
    }

    public NumbersViewModel? ViewModel { get; set; }

    private void CopyCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        StringBuilder sb = new StringBuilder();

        foreach (EquationItem equationItem in EquationList.SelectedItems)
            sb.AppendLine(equationItem.ToString());

        if (sb.Length > 0)
        {
            DataPackage dp = new();
            dp.SetText(sb.ToString());
            Clipboard.SetContent(dp);
        }
    }

    private void CopyCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
    {
        args.CanExecute = EquationList.SelectedItems.Any();
    }
}
