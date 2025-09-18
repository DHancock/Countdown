﻿using Countdown.Utilities;
using Countdown.ViewModels;

namespace Countdown.Views;

internal sealed partial class NumbersView : Page, IPageItem
{
    public NumbersView()
    {
        this.InitializeComponent();

        TargetCTB.Loaded += TargetCTB_Loaded;
    }

    private static void TargetCTB_Loaded(object sender, RoutedEventArgs e)
    {
        CountdownTextBox ctb = (CountdownTextBox)sender;
        ctb.Loaded -= TargetCTB_Loaded;

        FixTextBoxContextFlyoutMenuForThemeChange(ctb); // this is the first page loaded

        static void FixTextBoxContextFlyoutMenuForThemeChange(DependencyObject root)
        {
            TextBox? tb = root.FindChild<TextBox>();

            Debug.Assert(tb is not null);
            Debug.Assert(tb.ContextFlyout is not null);

            if ((tb is not null) && (tb.ContextFlyout is not null))
            {
                // The context flyout is the standard cut/copy/paste menu provided by the sdk.
                // This event handler affects all other TextBox instances, seems that they're
                // all sharing a single context flyout.
                tb.ContextFlyout.Opening += ContextFlyout_Opening;
            }

            static void ContextFlyout_Opening(object? sender, object e)
            {
                if ((sender is TextCommandBarFlyout tcbf) && (tcbf.Target is TextBox tb))
                {
                    foreach (ICommandBarElement icbe in tcbf.SecondaryCommands)
                    {
                        if ((icbe is FrameworkElement fe) && (fe.ActualTheme != tb.ActualTheme))
                        {
                            // update the menu item's text colour for theme changes occuring after the context flyout was created
                            // (this will also update each menu item's tool tip colours)
                            fe.RequestedTheme = tb.ActualTheme;
                        }
                    }
                }
            }
        }
    }

    public NumbersViewModel? ViewModel { get; set; }

    private void CopyCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        StringBuilder sb = new StringBuilder();

        foreach (object equationItem in EquationList.SelectedItems)
        {
            sb.AppendLine(equationItem.ToString());
        }

        if (sb.Length > 0)
        {
            DataPackage dp = new();
            dp.SetText(sb.ToString());
            Clipboard.SetContent(dp);
        }
    }

    private void CopyCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
    {
        args.CanExecute = EquationList.SelectedItems.Count > 0;
    }

    internal static void MenuFlyout_Opening(object sender, object e)
    {
        MenuFlyout menu = (MenuFlyout)sender;
        int selectedIndex = Settings.Instance.ChooseNumbersIndex;

        for (int index = 0; index < menu.Items.Count; index++)
        {
            ((RadioMenuFlyoutItem)menu.Items[index]).IsChecked = index == selectedIndex;
        }
    }

    public int PassthroughCount => 11;

    public void AddPassthroughContent(in RectInt32[] rects)
    {
        int index = 0;

        foreach (UIElement element in TileGrid.Children)
        {
            rects[index++] = Utils.GetPassthroughRect(element); // 6
        }

        rects[index++] = Utils.GetPassthroughRect(TargetCTB);

        foreach (UIElement element in ButtonGrid.Children)
        {
            rects[index++] = Utils.GetPassthroughRect(element); // 3
        }

        rects[index++] = Utils.GetPassthroughRect(EquationList);

        Debug.Assert(index == PassthroughCount);
    }
}
