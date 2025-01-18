using System;
using Avalonia.Platform;

namespace Sidekick.Avalonia.BlazorWebView.ControlHandle;

public interface INativeControl
{
    /// <param name="isSecond">Used to specify which control should be displayed as a demo</param>
    /// <param name="parent"></param>
    /// <param name="createDefault"></param>
    IPlatformHandle CreateControl(bool isSecond, IPlatformHandle parent, Func<IPlatformHandle> createDefault);
}
