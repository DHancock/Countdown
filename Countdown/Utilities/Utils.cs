namespace Countdown.Utilities;

internal static class Utils
{
    public static Vector3 GetOffsetFromXamlRoot(UIElement e)
    {
        Vector3 offset = e.ActualOffset;

        // FrameworkElement.Parent is the logical parent
        DependencyObject? dependencyObject = VisualTreeHelper.GetParent(e);

        while (dependencyObject != null)
        {
            if (dependencyObject is UIElement uie)
            {
                offset += uie.ActualOffset;

                if (uie is ScrollView sv)
                {
                    offset.X -= (float)sv.HorizontalOffset;
                    offset.Y -= (float)sv.VerticalOffset;
                }
                else if (uie is ScrollViewer svr)
                {
                    offset.X -= (float)svr.HorizontalOffset;
                    offset.Y -= (float)svr.VerticalOffset;
                }
            }

            dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
        }

        return offset;
    }

    public static RectInt32 ScaledRect(in Vector3 location, in Vector2 size, in float scale)
    {
        Debug.Assert(location.X >= 0f);
        Debug.Assert(location.Y >= 0f);
        Debug.Assert(size.X >= 0f);
        Debug.Assert(size.Y >= 0f);

        return new RectInt32((int)MathF.FusedMultiplyAdd(location.X, scale, 0.5f),
                             (int)MathF.FusedMultiplyAdd(location.Y, scale, 0.5f),
                             (int)MathF.FusedMultiplyAdd(size.X, scale, 0.5f),
                             (int)MathF.FusedMultiplyAdd(size.Y, scale, 0.5f));
    }

    public static RectInt32 GetPassthroughRect(UIElement e, float topBounds = 0f)
    {
        Vector3 offset = GetOffsetFromXamlRoot(e);
        Vector2 visibleSize = e.ActualSize;

        if (offset.Y < topBounds) // may be clipped if it's above the top edge of the scroll viewer
        {
            visibleSize.Y = offset.Y + visibleSize.Y - topBounds;

            if (visibleSize.Y < 0.1) // it's scrolled up out of view
            {
                return default;
            }

            offset.Y = topBounds;
        }

        // ignore clipping when part or all of the element is below the window bottom, it can't be clicked anyway

        return ScaledRect(offset, visibleSize, (float)e.XamlRoot.RasterizationScale);
    }

    public static void PlayExclamation()
    {
        bool succeeded = PInvoke.MessageBeep(MESSAGEBOX_STYLE.MB_ICONEXCLAMATION);
        Debug.Assert(succeeded);
    }

    public static bool IsControlKeyDown()
    {
        return IsKeyDown(VirtualKey.Control) || IsKeyDown(VirtualKey.LeftControl) || IsKeyDown(VirtualKey.RightControl);

        static bool IsKeyDown(VirtualKey key)
        {
            return InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        }
    }
}
