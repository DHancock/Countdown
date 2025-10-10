namespace Countdown.Utilities;

internal static class Utils
{
    public static Point GetOffsetFromXamlRoot(UIElement e)
    {
        GeneralTransform gt = e.TransformToVisual(e.XamlRoot.Content);
        return gt.TransformPoint(new Point(0f, 0f));
    }

    public static RectInt32 ScaledRect(in Point location, in Vector2 size, double scale)
    {
        Debug.Assert(location.X >= 0.0);
        Debug.Assert(location.Y >= 0.0);
        Debug.Assert(size.X >= 0f);
        Debug.Assert(size.Y >= 0f);

        return new RectInt32((int)Math.FusedMultiplyAdd(location.X, scale, 0.5),
                             (int)Math.FusedMultiplyAdd(location.Y, scale, 0.5),
                             (int)Math.FusedMultiplyAdd(size.X, scale, 0.5),
                             (int)Math.FusedMultiplyAdd(size.Y, scale, 0.5));
    }

    public static RectInt32 GetPassthroughRect(UIElement e, double topBounds = 0.0)
    {
        Point offset = GetOffsetFromXamlRoot(e);
        Vector2 visibleSize = e.ActualSize;

        if (offset.Y < topBounds) // may be clipped if it's above the top edge of the scroll viewer
        {
            visibleSize.Y = (float)(offset.Y + visibleSize.Y - topBounds);

            if (visibleSize.Y < 0.1) // it's scrolled up out of view
            {
                return default;
            }

            offset.Y = topBounds;
        }

        // ignore clipping when part or all of the element is below the window bottom, it can't be clicked anyway

        return ScaledRect(offset, visibleSize, e.XamlRoot.RasterizationScale);
    }

    public static void PlayExclamation()
    {
        bool succeeded = PInvoke.MessageBeep(MESSAGEBOX_STYLE.MB_ICONEXCLAMATION);
        Debug.Assert(succeeded);
    }
}
