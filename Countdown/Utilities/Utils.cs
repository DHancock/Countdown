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
        return new RectInt32(Convert.ToInt32(location.X * scale),
                             Convert.ToInt32(location.Y * scale),
                             Convert.ToInt32(size.X * scale),
                             Convert.ToInt32(size.Y * scale));
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
        else if (offset.Y > e.XamlRoot.Size.Height) // it's scrolled off the window bottom
        {
            return default;
        }

        // ignore clipping when part of the element is below the window bottom, it can't be clicked anyway

        return ScaledRect(offset, visibleSize, e.XamlRoot.RasterizationScale);
    }
}
