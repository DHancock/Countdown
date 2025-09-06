using Countdown.Utilities;
using Countdown.ViewModels;

namespace Countdown.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
internal sealed partial class StopwatchView : Page, IPageItem
{
    public StopwatchView()
    {
        this.InitializeComponent();
    }

    public StopwatchViewModel? ViewModel { get; set; }

    public int PassthroughCount => 1;

    public void AddPassthroughContent(in RectInt32[] rects)
    {
        rects[0] = Utils.GetPassthroughRect(StopwatchButton);
    }
}
