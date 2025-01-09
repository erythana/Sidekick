using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using Sidekick.Apis.Poe.Clients;
using Sidekick.Apis.Poe.CloudFlare;
using Sidekick.Avalonia.Helpers;

namespace Sidekick.Avalonia;

public partial class CloudflareWindow : Window
{
    private readonly ILogger logger;
    private readonly ICloudflareService cloudflareService;
    private readonly Uri uri;
    private bool challengeCompleted;

    public CloudflareWindow(ILogger logger, ICloudflareService cloudflareService, Uri uri)
    {
        InitializeComponent();
        this.logger = logger;
        this.cloudflareService = cloudflareService;
        this.uri = uri;
        // Ready();
    }

    // public void Ready()
    // {
    //     _ = Dispatcher.UIThread.Invoke(async () =>
    //     {
    //         Topmost = true;
    //         ShowInTaskbar = true;
    //
    //         await WebView.EnsureCoreWebView2Async();
    //         WebView.CoreWebView2.Settings.UserAgent = PoeTradeHandler.UserAgent;
    //
    //         // Handle cookie changes by checking cookies after navigation
    //         WebView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
    //
    //         WebView.Source = uri;
    //
    //         // This avoids the white flicker which is caused by the page content not being loaded initially. We show the webview control only when the content is ready.
    //         WebView.Visibility = Visibility.Visible;
    //
    //         // The window background is transparent to avoid any flickering when opening a window. When the webview content is ready we need to set a background color. Otherwise, mouse clicks will go through the window.
    //         Background = (Brush?)new BrushConverter().ConvertFrom("#000000");
    //         Opacity = 0.01;
    //
    //         CenterHelper.Center(this);
    //         Activate();
    //     });
    // }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!challengeCompleted)
        {
            logger.LogInformation("[CloudflareWindow] Closing the window without completing the challenge, marking as failed");
            _ = cloudflareService.CaptchaChallengeFailed();
        }

        // UnregisterName("Grid");
        // UnregisterName("WebView");
        base.OnClosing(e);
    }

    // private async void CoreWebView2_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    // {
    //     try
    //     {
    //         var cookies = await WebView.CoreWebView2.CookieManager.GetCookiesAsync(uri.GetLeftPart(UriPartial.Authority));
    //         var cfCookie = cookies.FirstOrDefault(c => c.Name == "cf_clearance");
    //         if (cfCookie == null)
    //         {
    //             return;
    //         }
    //
    //         // Store the Cloudflare cookie
    //         challengeCompleted = true;
    //         _ = cloudflareService.CaptchaChallengeCompleted(cookies.ToDictionary(c => c.Name, c => c.Value));
    //         logger.LogInformation("[CloudflareWindow] Cookie check completed, challenge likely completed");
    //
    //         Dispatcher.UIThread.Invoke(Close);
    //     }
    //     catch (Exception ex)
    //     {
    //         logger.LogError(ex, "[CloudflareWindow] Error handling cookie check");
    //     }
    // }
}
