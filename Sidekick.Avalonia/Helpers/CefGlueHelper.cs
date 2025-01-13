using System;
using Avalonia;
using Sidekick.Apis.Poe.Clients;
using Xilium.CefGlue;
using Xilium.CefGlue.Common;

namespace Sidekick.Avalonia.Helpers;

public static class CefGlueHelper
{
    public static AppBuilder RegisterCefSettings(this AppBuilder appBuilder)
    {
        return appBuilder.AfterSetup(_ => CefRuntimeLoader.Initialize(new CefSettings()
        {
            RootCachePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Sidekick"),
            CachePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Sidekick", "Sidekick.Avalonia"),
            UserAgent = PoeTradeHandler.UserAgent,
            WindowlessRenderingEnabled = false,
            LogSeverity = CefLogSeverity.Debug
        }));
    }
}
