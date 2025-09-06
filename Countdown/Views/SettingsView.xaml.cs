using Microsoft.UI.Xaml.Controls.Primitives;

using Countdown.Utilities;
using Countdown.ViewModels;

namespace Countdown.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
internal sealed partial class SettingsView : Page, IPageItem
{
    public SettingsViewModel? ViewModel { get; set; }

    public SettingsView()
    {
        this.InitializeComponent();

        VersionTextBlock.Text = $"Version: {typeof(App).Assembly.GetName().Version?.ToString(3)}";
    }

    private void RootScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        App.MainWindow.SetWindowDragRegions();
    }

    private void Expander_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        App.MainWindow.SetWindowDragRegions();
    }

    public int PassthroughCount => 4;

    public void AddPassthroughContent(in RectInt32[] rects)
    {
        double topClip = 0.0;

        if (RootScrollViewer.ComputedVerticalScrollBarVisibility == Visibility.Visible)
        {
            topClip = Utils.GetOffsetFromXamlRoot(RootScrollViewer).Y;

            ScrollBar? vScrollBar = RootScrollViewer.FindChild<ScrollBar>("VerticalScrollBar");
            Debug.Assert(vScrollBar is not null);

            if (vScrollBar is not null)
            {
                rects[0] = Utils.GetPassthroughRect(vScrollBar);
            }
        }

        rects[1] = Utils.GetPassthroughRect(ThemeExpander, topClip);
        rects[2] = Utils.GetPassthroughRect(VolumeExpander, topClip);
        rects[3] = Utils.GetPassthroughRect(HyperlinkTextBlock, topClip);   
    }
}
