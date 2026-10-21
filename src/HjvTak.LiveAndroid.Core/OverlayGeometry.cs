using System;
using System.Drawing;

namespace HjvTak.LiveAndroid.Core
{
    public static class OverlayGeometry
    {
        // All screen coordinates use the caller's Win32 DPI context, including negative monitor origins.
        public static bool TryMap(Rectangle client, double slideWidth, double slideHeight,
            double left, double top, double width, double height, out Rectangle result)
        {
            result = Rectangle.Empty;
            foreach (var n in new[] { slideWidth, slideHeight, left, top, width, height })
                if (double.IsNaN(n) || double.IsInfinity(n)) return false;
            if (client.Width <= 0 || client.Height <= 0 || slideWidth <= 0 || slideHeight <= 0 ||
                width <= 0 || height <= 0 || left < 0 || top < 0 ||
                left + width > slideWidth + 0.01 || top + height > slideHeight + 0.01) return false;
            double scale = Math.Min(client.Width / slideWidth, client.Height / slideHeight);
            double x = client.X + (client.Width - slideWidth * scale) / 2;
            double y = client.Y + (client.Height - slideHeight * scale) / 2;
            int x1 = (int)Math.Round(x + left * scale);
            int y1 = (int)Math.Round(y + top * scale);
            int x2 = (int)Math.Round(x + (left + width) * scale);
            int y2 = (int)Math.Round(y + (top + height) * scale);
            result = Rectangle.FromLTRB(x1, y1, x2, y2);
            return result.Width > 0 && result.Height > 0;
        }
    }
}
