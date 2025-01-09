using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using Sidekick.Apis.Poe.CloudFlare;
using Sidekick.Common.Settings;
using Sidekick.Avalonia.Helpers;
using Sidekick.Common.Ui.Views;

namespace Sidekick.Avalonia.Services
{
    public class AvaloniaViewLocator : IViewLocator
    {
        private readonly ILogger<AvaloniaViewLocator> logger;
        private readonly ICloudflareService cloudflareService;
        private readonly ISettingsService settingsService;
        internal readonly IViewPreferenceService ViewPreferenceService;

        internal List<MainWindow> Windows { get; } = new();

        internal string? NextUrl { get; set; }

        public AvaloniaViewLocator(ILogger<AvaloniaViewLocator> logger, ICloudflareService cloudflareService, ISettingsService settingsService, IViewPreferenceService viewPreferenceService)
        {
            this.logger = logger;
            this.cloudflareService = cloudflareService;
            this.settingsService = settingsService;
            ViewPreferenceService = viewPreferenceService;
            cloudflareService.ChallengeStarted += CloudflareServiceOnChallengeStarted;
        }

        /// <inheritdoc/>
        public async Task Initialize(SidekickView view)
        {
            if (!TryGetWindow(view.CurrentView, out var window))
            {
                return;
            }

            window.SidekickView = view;
            view.CurrentView.ViewChanged += CurrentViewOnViewChanged;
            var preferences = await ViewPreferenceService.Get(view.CurrentView.Key);

            _ = Dispatcher.UIThread.InvokeAsync(async () =>
            {
                window.Title = view.CurrentView.Title.StartsWith("Sidekick") ? view.CurrentView.Title.Trim() : $"Sidekick {view.CurrentView.Title}".Trim();
                window.MinHeight = view.ViewHeight + 20;
                window.MinWidth = view.ViewWidth + 20;

                if (view.ViewType != SidekickViewType.Modal && preferences != null)
                {
                    window.Height = preferences.Height;
                    window.Width = preferences.Width;
                }
                else
                {
                    window.Height = view.ViewHeight + 20;
                    window.Width = view.ViewWidth + 20;
                }

                if (view.ViewType == SidekickViewType.Overlay)
                {
                    window.Topmost = true;
                    window.ShowInTaskbar = false;
                    window.CanResize = true;
                }
                else if (view.ViewType == SidekickViewType.Modal)
                {
                    window.Topmost = false;
                    window.ShowInTaskbar = true;
                    window.CanResize = false;
                }
                else
                {
                    window.Topmost = false;
                    window.ShowInTaskbar = true;
                    window.CanResize = true;
                }

                // Set the window position.
                var saveWindowPositions = await settingsService.GetBool(SettingKeys.SaveWindowPositions);
                if (saveWindowPositions && preferences is
                    {
                        X: not null,
                        Y: not null,
                    })
                {
                    window.Position = new PixelPoint(x: preferences.X.Value, y: preferences.Y.Value);
                }
                else
                {
                    CenterHelper.Center(window);
                }

                window.Ready();
            });
        }

        private void CurrentViewOnViewChanged(ICurrentView view)
        {
            if (!TryGetWindow(view, out var window))
            {
                return;
            }

            Dispatcher.UIThread.Invoke(() =>
            {
                var center = false;

                if (view.Width != null)
                {
                    window.Width = view.Width.Value;
                    window.MinWidth = view.Width.Value;
                    center = true;
                }

                if (view.Height != null)
                {
                    window.Height = view.Height.Value;
                    window.MinHeight = view.Height.Value;
                    center = true;
                }

                if (view.MinWidth != null)
                {
                    window.MinWidth = view.MinWidth.Value;
                    center = true;
                }

                if (view.MinHeight != null)
                {
                    window.MinHeight = view.MinHeight.Value;
                    center = true;
                }

                if (center)
                {
                    CenterHelper.Center(window);
                }

                window.Title = $"Sidekick {view.Title}".Trim();
            });
        }

        /// <inheritdoc/>
        public async Task Maximize(SidekickView view)
        {
            if (!TryGetWindow(view.CurrentView, out var window))
            {
                return;
            }

            var preferences = await ViewPreferenceService.Get($"view_preference_{view.CurrentView.Key}");

            Dispatcher.UIThread.Invoke(() =>
            {
                if (window.WindowState == WindowState.Normal)
                {
                    window.WindowState = WindowState.Maximized;
                }
                else
                {
                    window.WindowState = WindowState.Normal;

                    if (preferences != null)
                    {
                        window.Height = preferences.Height;
                        window.Width = preferences.Width;
                    }
                    else
                    {
                        window.Height = view.ViewHeight;
                        window.Width = view.ViewWidth;
                    }
                }

                CenterHelper.Center(window);
            });
        }

        /// <inheritdoc/>
        public Task Minimize(SidekickView view)
        {
            if (!TryGetWindow(view.CurrentView, out var window))
            {
                return Task.CompletedTask;
            }

            Dispatcher.UIThread.Invoke(() =>
            {
                if (window.WindowState == WindowState.Normal)
                {
                    window.WindowState = WindowState.Minimized;
                }
                else
                {
                    window.WindowState = WindowState.Normal;
                    CenterHelper.Center(window);
                }
            });
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task Close(SidekickView view)
        {
            if (!TryGetWindow(view.CurrentView, out var window))
            {
                return Task.CompletedTask;
            }

            Dispatcher.UIThread.Invoke(() =>
            {
                try
                {
                    window.Close();
                    Windows.Remove(window);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }
                catch (InvalidOperationException ex)
                {
                    logger.LogWarning($"Error Closing Window - {ex.Message}");
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task CloseAll()
        {
            foreach (var window in Windows.ToList())
            {
                if (window.SidekickView == null)
                {
                    continue;
                }

                await Close(window.SidekickView);
            }
        }

        /// <inheritdoc/>
        public async Task CloseAllOverlays()
        {
            foreach (var overlay in Windows.Where(x => x.SidekickView?.ViewType == SidekickViewType.Overlay).ToList())
            {
                if (overlay.SidekickView == null)
                {
                    continue;
                }

                await Close(overlay.SidekickView);
            }
        }

        /// <inheritdoc/>
        public bool IsOverlayOpened() => Windows.Any(x => x.SidekickView?.ViewType == SidekickViewType.Overlay);

        /// <inheritdoc/>
        public async Task Open(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return;
            }

            NextUrl = url;

            var culture = await settingsService.GetString(SettingKeys.LanguageUi);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (string.IsNullOrEmpty(culture))
                {
                    return;
                }

                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            });

            Dispatcher.UIThread.Invoke(() =>
            {
                var window = new MainWindow(this)
                {
                    Topmost = true,
                    ShowInTaskbar = false,
                    CanResize = false
                };
                Windows.Add(window);
                window.Show();
            });
        }

        private void CloudflareServiceOnChallengeStarted(Uri uri)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                var window = new CloudflareWindow(logger, cloudflareService, uri)
                {
                    Topmost = true,
                    ShowInTaskbar = false,
                    CanResize = false
                };
                window.Show();
            });
        }

        private bool TryGetWindow(ICurrentView view, out MainWindow window)
        {
            var windowResult = Windows.FirstOrDefault(x => x.Id == view.Id);
            if (windowResult == null)
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    var viewUrl = WebUtility.UrlDecode(view.Url);
                    windowResult = Windows.FirstOrDefault(x => x.CurrentWebPath == viewUrl);
                });
            }

            window = windowResult!;

            if (windowResult != null)
            {
                windowResult.Id = view.Id;
                return true;
            }

            logger.LogError("Unable to find view {viewUrl}", view.Url);
            return false;
        }
    }
}
