
namespace Countdown.Utils
{
    internal sealed class ThemeHelper
    {
        public static readonly ThemeHelper Instance = new ThemeHelper();

        // this is a single window app, with no printing
        private AppWindowTitleBar? titleBar;
        private FrameworkElement? root;

        private ThemeHelper()
        {
        }

        public void Register(FrameworkElement root, AppWindowTitleBar titleBar)
        {
            this.titleBar = titleBar;
            Register(root);
        }

        public void Register(FrameworkElement root)
        {
            this.root = root;
        }



        public void UpdateTheme(ElementTheme requestedTheme)
        {
            UpdateRoot(requestedTheme);
            UpdateTitleBar(requestedTheme);
        }



        private void UpdateRoot(ElementTheme requestedTheme)
        {
            if ((root is not null) && (root.RequestedTheme != requestedTheme))
                root.RequestedTheme = requestedTheme;

        }

        private void UpdateTitleBar(ElementTheme requestedTheme)
        {
            if (titleBar is not null)
            {
                if (requestedTheme == ElementTheme.Default)
                    requestedTheme = (App.Current.RequestedTheme == ApplicationTheme.Light) ? ElementTheme.Light : ElementTheme.Dark;

                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonHoverBackgroundColor = Colors.Transparent;
                titleBar.ButtonPressedBackgroundColor = Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

                if (requestedTheme == ElementTheme.Light)
                {
                    titleBar.ButtonForegroundColor = Colors.Black;
                    titleBar.ButtonPressedForegroundColor = Colors.Black;
                    titleBar.ButtonHoverForegroundColor = Colors.Black;
                    titleBar.ButtonHoverBackgroundColor = Colors.White;
                    titleBar.ButtonInactiveForegroundColor = Colors.DarkGray;
                }
                else
                {
                    titleBar.ButtonForegroundColor = Colors.White;
                    titleBar.ButtonPressedForegroundColor = Colors.White;
                    titleBar.ButtonHoverForegroundColor = Colors.White;
                    titleBar.ButtonHoverBackgroundColor = Colors.DimGray;
                    titleBar.ButtonInactiveForegroundColor = Colors.DimGray;
                }
            }
        }
    }
}
