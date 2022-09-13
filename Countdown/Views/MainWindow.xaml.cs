using Countdown.Utils;
using Countdown.ViewModels;

namespace Countdown.Views;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
internal sealed partial class MainWindow : SubClassWindow
{
    private readonly ViewModel rootViewModel;
    private readonly AppWindow appWindow;

    private readonly FrameNavigationOptions frameNavigationOptions = new FrameNavigationOptions()
    {
        TransitionInfoOverride = new SuppressNavigationTransitionInfo(),
        IsNavigationStackEnabled = false,
    };


    public MainWindow()
    {
        this.InitializeComponent();

        RootNavigationView.MenuItems.Add(CreateSettingsNavigationViewItem());

        rootViewModel = new ViewModel(ReadSettings());

        MinWidth = 660;
        MinHeight = 500;

        appWindow = GetAppWindowForCurrentWindow();

        appWindow.Closing += (s, a) =>
        {
            rootViewModel.WindowPlacement = GetWindowPlacement();
            SaveSettings();
        };

        this.Activated += (s, a) =>
        {
            ThemeHelper.Instance.UpdateTheme(rootViewModel.SettingsViewModel.SelectedTheme);
        };

        if (AppWindowTitleBar.IsCustomizationSupported() && appWindow.TitleBar is not null)
        {
            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            SetTitleBar(CustomTitleBar);
            ThemeHelper.Instance.Register(LayoutRoot, appWindow.TitleBar);
        }
        else
        {
            SetWindowIcon();
            appWindow.Title = CustomTitle.Text;
            CustomTitleBar.Visibility = Visibility.Collapsed;
            ThemeHelper.Instance.Register(LayoutRoot);
        }

        // SelectionFollowsFocus is disabled to avoid multiple selection changed events
        // see https://github.com/microsoft/microsoft-ui-xaml/issues/5744
        if (RootNavigationView.SelectionFollowsFocus == NavigationViewSelectionFollowsFocus.Disabled)
            RootNavigationView.SelectedItem = RootNavigationView.MenuItems[0];

        SetWindowPlacement(rootViewModel.WindowPlacement);
    }

    private AppWindow GetAppWindowForCurrentWindow()
    {
        IntPtr windowHandle = WindowNative.GetWindowHandle(this);
        WindowId windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
        return AppWindow.GetFromWindowId(windowId);
    }


    private object CreateSettingsNavigationViewItem()
    {
        return new NavigationViewItem()
        {
            Tag = nameof(SettingsView),
            AccessKey = "S",
            Icon = new AnimatedIcon()
            {
                Source = new AnimatedSettingsVisualSource(),
                FallbackIconSource = new SymbolIconSource()
                {
                    Symbol = Symbol.Setting,
                },
            },
        };
    }


    private void RootNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            Type? type = Type.GetType($"Countdown.Views.{item.Tag}");

            if (type is not null)
                _ = ContentFrame.NavigateToType(type, null, frameNavigationOptions);
        }
    }

    private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
    {
        switch (e.SourcePageType.Name)
        {
            case nameof(NumbersView): ((NumbersView)e.Content).ViewModel = rootViewModel.NumbersViewModel; break;
            case nameof(LettersView): ((LettersView)e.Content).ViewModel = rootViewModel.LettersViewModel; break;
            case nameof(ConundrumView): ((ConundrumView)e.Content).ViewModel = rootViewModel.ConundrumViewModel; break;
            case nameof(StopwatchView): ((StopwatchView)e.Content).ViewModel = rootViewModel.StopwatchViewModel; break;
            case nameof(SettingsView): ((SettingsView)e.Content).ViewModel = rootViewModel.SettingsViewModel; break;
            default:
                throw new InvalidOperationException();
        }
    }


    private static string ReadSettings()
    {
        string path = GetSettingsFilePath();

        if (File.Exists(path))
        {
            try
            {
                return File.ReadAllText(path, Encoding.Unicode);
            }
            catch (Exception ex)
            {
                Debug.Fail(ex.ToString());
            }
        }

        return String.Empty;
    }

    private async void SaveSettings()
    {
        try
        {
            string path = GetSettingsFilePath();
            string? directory = Path.GetDirectoryName(path);

            if (string.IsNullOrWhiteSpace(directory))
                throw new InvalidOperationException();

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            await File.WriteAllTextAsync(path, rootViewModel.SerializeSettings(), Encoding.Unicode, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Debug.Fail(ex.ToString());
        }
    }
}
