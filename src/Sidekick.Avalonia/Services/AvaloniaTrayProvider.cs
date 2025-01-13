using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ReactiveUI;
using Sidekick.Common.Platform;
using IViewLocator = Sidekick.Common.Ui.Views.IViewLocator;

namespace Sidekick.Avalonia.Services;

public class AvaloniaTrayProvider(IViewLocator viewLocator) : ITrayProvider, IDisposable
{
    private bool Initialized { get; set; }

    private TrayIcon? Icon { get; set; }

    public void Initialize(List<TrayMenuItem> items)
    {
        if (Initialized)
        {
            return;
        }

        Icon = new TrayIcon();
        Icon.Menu = [];
        
        var bmp = new Bitmap(AssetLoader.Open(new Uri("avares://Sidekick.Avalonia/Assets/favicon.ico")));
        Icon.Icon = new WindowIcon(bmp);
        Icon.ToolTipText = "Sidekick";
        Icon.Command = ReactiveCommand.Create(() => viewLocator.Open("/settings"));

        if (Debugger.IsAttached)
        {
            var developmentMenuItem = new NativeMenuItem()
            {
                Header = "Development",
                IsEnabled = true,
            };

            developmentMenuItem.Click += async (_, _) =>
            {
                await viewLocator.Open("/development");
            };

            Icon.Menu.Items.Add(developmentMenuItem);
        }

        foreach (var item in items)
        {
            var menuItem = new NativeMenuItem()
            {
                Header = item.Label,
                IsEnabled = !item.Disabled,
            };

            menuItem.Click += async (_, _) =>
            {
                if (item.OnClick != null)
                {
                    await item.OnClick();
                }
            };

            Icon.Menu.Items.Add(menuItem);
        }

        Initialized = true;
    }

    public void Dispose()
    {
        if (Icon != null)
        {
            Icon.Dispose();
        }
    }
}
