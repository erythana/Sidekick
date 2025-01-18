using System.Runtime.Versioning;
using WebKit;

namespace Sidekick.Avalonia.BlazorWebView;

[UnsupportedOSPlatform("OSX")]
[UnsupportedOSPlatform("Windows")]
public class BlazorWebView : WebView
{
	public BlazorWebView(IServiceProvider serviceProvider)
	{
		_ = new WebViewManager(this, serviceProvider);
	}
}
