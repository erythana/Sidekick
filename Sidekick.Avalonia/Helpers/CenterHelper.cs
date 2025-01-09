using Avalonia;
using Avalonia.Controls;

namespace Sidekick.Avalonia.Helpers;

public static class CenterHelper
{
    public static void Center(Window window)
    {
            // Calculate center position
            var screenWidth = window.Bounds.Width;
            var screenHeight = window.Bounds.Height;

            var windowWidth = window.Width;
            var windowHeight = window.Height;

            var x = ((screenWidth - windowWidth) / 2) + window.Bounds.X;
            var y = ((screenHeight - windowHeight) / 2) + window.Bounds.Y;

            // Set the window's position
            window.Position = new PixelPoint(x: (int)x, y: (int)y);
    }
}
