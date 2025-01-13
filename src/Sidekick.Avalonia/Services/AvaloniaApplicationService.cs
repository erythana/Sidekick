using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Sidekick.Common.Platform;

namespace Sidekick.Avalonia.Services
{
    public class AvaloniaApplicationService : IApplicationService
    {
        public void Shutdown()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp)
                desktopApp.Shutdown();
            
            Environment.Exit(0);
        }
    }
}
