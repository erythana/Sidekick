using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Sidekick.Avalonia;

internal class OverlayControl : ContentControl, IDisposable
{
    private OverlayWindow? window;

    public OverlayControl()
    {
        Loaded += OverlayControl_Loaded;
        Unloaded += OverlayControl_Unloaded;
    }

    private void OverlayControl_Loaded(object? sender, RoutedEventArgs e)
    {
        window = new OverlayWindow { Content = Content, };
        window.Show();
    }

    private void OverlayControl_Unloaded(object? sender, RoutedEventArgs e)
    {
        Dispose(); // Call cleanup in Unloaded
    }

    public void Dispose()
    {
        // Unsubscribe from events
        Loaded -= OverlayControl_Loaded;
        Unloaded -= OverlayControl_Unloaded; // Unsubscribe here as well

        // Dispose of any resources
        window?.Close();
    }
}
