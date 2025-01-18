using Avalonia.Controls;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using Sidekick.Apis.Poe.CloudFlare;
using WebKit;

namespace Sidekick.Avalonia;

public partial class CloudflareWindow : Window
{
    private BlazorWebView.BlazorWebView? browser;

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
        browser = new BlazorWebView.BlazorWebView(App.ServiceProvider);
        browser.OnLoadChanged += BrowserOnOnLoadChanged;
        
        //
        // BrowserWrapper.Child = browser;
        //
        //
        // Dispatcher.UIThread.Invoke(() =>
        // {
        //     Topmost = true;
        //     ShowInTaskbar = true;
        //
        //     browser.LoadRequest(uri.ToString);
        //     
        //     // The window background is transparent to avoid any flickering when opening a window. When the webview content is ready we need to set a background color. Otherwise, mouse clicks will go through the window.
        //     Background = (Brush?)new BrushConverter().ConvertFrom("#000000");
        //     Opacity = 0.01;
        //
        //     CenterHelper.Center(this);
        //     Activate();
        // });
    }

    private void BrowserOnOnLoadChanged(WebView sender, WebView.LoadChangedSignalArgs args)
    {
        if (args.LoadEvent != WebKit.LoadEvent.Finished)
            return;

        try
        {
            //TODO: Cookie stuff for Cloudflare auth
            // if(!manager.VisitUrlCookies(uri.GetLeftPart(UriPartial.Authority), false, cookieVisitor))
            //     return;
            
            
    
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
