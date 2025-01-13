using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using Sidekick.Apis.Poe.Clients;
using Sidekick.Apis.Poe.CloudFlare;
using Sidekick.Avalonia.Helpers;
using Xilium.CefGlue;
using Xilium.CefGlue.Avalonia;
using Xilium.CefGlue.Common.Events;

namespace Sidekick.Avalonia;

public partial class CloudflareWindow : Window
{
    private AvaloniaCefBrowser? browser;

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
        Ready();
    }

    public void Ready()
    {
        browser = new AvaloniaCefBrowser();
        browser.LoadEnd += BrowserOnLoadEnd;
        
        
        BrowserWrapper.Child = browser;
        

        Dispatcher.UIThread.Invoke(() =>
        {
            Topmost = true;
            ShowInTaskbar = true;

            browser.Address = uri.ToString();
            
            // The window background is transparent to avoid any flickering when opening a window. When the webview content is ready we need to set a background color. Otherwise, mouse clicks will go through the window.
            Background = (Brush?)new BrushConverter().ConvertFrom("#000000");
            Opacity = 0.01;
    
            CenterHelper.Center(this);
            Activate();
        });
    }
    
    public class CloudflareCookieVisitor : CefCookieVisitor
    {
        protected override bool Visit(CefCookie cookie, int count, int total, out bool delete)
        {
            delete = false;
            return true;
        }
    }

    private void BrowserOnLoadEnd(object sender, LoadEndEventArgs e)
    {
        try
        {
            var manager = CefCookieManager.GetGlobal(null);
            var cookieVisitor = new CloudflareCookieVisitor();
            if(!manager.VisitUrlCookies(uri.GetLeftPart(UriPartial.Authority), false, cookieVisitor))
                return;
            
            
    
            // Store the Cloudflare cookie
            challengeCompleted = true;
            // _ = cloudflareService.CaptchaChallengeCompleted(cookies.ToDictionary(c => c.Name, c => c.Value));
            logger.LogInformation("[CloudflareWindow] Cookie check completed, challenge likely completed");
    
            Dispatcher.UIThread.Invoke(Close);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[CloudflareWindow] Error handling cookie check");
        }
    }

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

   
}
