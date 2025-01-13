using System;
using System.Net;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Sidekick.Avalonia.Services;
using Sidekick.Common.Ui.Views;
using Xilium.CefGlue;
using Xilium.CefGlue.Avalonia;
using Xilium.CefGlue.Common.Handlers;

namespace Sidekick.Avalonia;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private AvaloniaCefBrowser? browser;
    private readonly AvaloniaViewLocator viewLocator;
    private bool isClosing;

    private IServiceScope Scope { get; set; }

    public Guid Id { get; set; }

    public MainWindow(IViewLocator viewLocator)
    {
        InitializeComponent();
        Scope = App.ServiceProvider.CreateScope();
        Resources.Add("services", Scope.ServiceProvider);
        this.viewLocator = (AvaloniaViewLocator)viewLocator;
        

        browser = new AvaloniaCefBrowser();
        BrowserWrapper.Child = browser;
        
        Deactivated += (_, _) =>
        {
            if (SidekickView?.CloseOnBlur == true)
            {
                viewLocator.Close(SidekickView);
            }
        };
    }

    internal SidekickView? SidekickView { get; set; }

    internal string? CurrentWebPath => WebUtility.UrlDecode(browser?.Address);

    public void Ready()
    {
        // if (!Debugger.IsAttached)
        // {
        //     // browser.acc
        //     WebView.WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        //     WebView.WebView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
        //     WebView.WebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
        // }
        //
        // // var wasAlreadyVisible = WebView.Visibility == Visibility.Visible;
        //
        // // This avoids the white flicker which is caused by the page content not being loaded initially. We show the webview control only when the content is ready.
        // WebView.Visibility = Visibility.Visible;
        
        // The window background is transparent to avoid any flickering when opening a window. When the webview content is ready we need to set a background color. Otherwise, mouse clicks will go through the window.
        Background = (Brush?)new BrushConverter().ConvertFrom("#000000");
        Opacity = 0.01;
        
        Activate();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);

        if (isClosing || !IsVisible || !CanResize || WindowState == WindowState.Maximized)
        {
            return;
        }

        // Save the window position and size.
        try
        {
            var width = (int)Bounds.Width;
            var height = (int)Bounds.Height;
            var x = Position.X;
            var y = Position.Y;

            _ = viewLocator.ViewPreferenceService.Set(SidekickView?.CurrentView.Key, width, height, x, y);
        }
        catch (Exception)
        {
            // If the save fails, we don't want to stop the execution.
        }

        Resources.Remove("services");
        viewLocator.Windows.Remove(this);
        Scope.Dispose();
        try
        {
            browser?.Dispose();
        }
        catch (Exception)
        {
            // If the dispose fails, we don't want to stop the execution.
        }
        finally
        {
            browser = null;
        }
       
        //TODO
        // UnregisterName("Grid");
        // UnregisterName("OverlayContainer");
        // UnregisterName("TopBorder");
        // UnregisterName("WebView");

        isClosing = true;
    }

    protected override void OnResized(WindowResizedEventArgs e)
    {
        base.OnResized(e);
        
        Grid.Margin = WindowState == WindowState.Maximized ? new Thickness(0) : new Thickness(5);
    }
    
    private void TopBorder_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }
}
