using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using ApexCharts;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using Sidekick.Apis.GitHub;
using Sidekick.Apis.Poe;
using Sidekick.Apis.PoeNinja;
using Sidekick.Apis.PoePriceInfo;
using Sidekick.Apis.PoeWiki;
using Sidekick.Avalonia.Services;
using Sidekick.Common;
using Sidekick.Common.Blazor;
using Sidekick.Common.Browser;
using Sidekick.Common.Database;
using Sidekick.Common.Platform;
using Sidekick.Common.Platform.Interprocess;
using Sidekick.Common.Settings;
using Sidekick.Common.Ui;
using Sidekick.Common.Updater;
using Sidekick.Mock;
using Sidekick.Modules.Chat;
using Sidekick.Modules.Development;
using Sidekick.Modules.General;
using Sidekick.Modules.Maps;
using Sidekick.Modules.Trade;
using Sidekick.Modules.Wealth;
using Velopack;
using IViewLocator = Sidekick.Common.Ui.Views.IViewLocator;

namespace Sidekick.Avalonia;


    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static ServiceProvider ServiceProvider { get; private set; } = null!;

        private readonly ILogger<App> logger;
        private readonly ISettingsService settingsService;
        private readonly IInterprocessService interprocessService;
        private readonly ITrayProvider trayProvider;
        private readonly IApplicationService appService;
        private IViewLocator viewLocator;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var settingDirectory = settingsService.GetString(SettingKeys.CurrentDirectory).Result;
            if (string.IsNullOrEmpty(settingDirectory) || settingDirectory != currentDirectory)
            {
                logger.LogDebug("[Startup] Current Directory set to: {0}", currentDirectory);
                settingsService.Set(SettingKeys.CurrentDirectory, currentDirectory).Wait();
                settingsService.Set(SettingKeys.WealthEnabled, false).Wait();
            }

            

            AttachErrorHandlers();
            interprocessService.StartReceiving();
            viewLocator = ServiceProvider.GetRequiredService<IViewLocator>();
            _ = viewLocator.Open("/");
            
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                _ = HandleInterprocessCommunications(desktop.Args ?? []);
                desktop.Exit += OnExit;
                desktop.MainWindow = new MainWindow(viewLocator);
            }
            
            InitializeTray();
            
            base.OnFrameworkInitializationCompleted();
        }
        
        private void InitializeTray()
        {
            var browserProvider = ServiceProvider.GetRequiredService<IBrowserProvider>();
            
            var menuItems = new List<TrayMenuItem>();

            //todo localization
            menuItems.AddRange(new List<TrayMenuItem>()
            {
                new(label: "Sidekick - Avalonia"),
                new(label: "Open_Website",
                    onClick: () =>
                    {
                        browserProvider.OpenSidekickWebsite();
                        return Task.CompletedTask;
                    }),

                new(label: "Wealth", onClick: () => viewLocator.Open("/wealth")),

                new(label: "Settings", onClick: () => viewLocator.Open("/settings")),
                new(label: "Exit",
                    onClick: () =>
                    {
                        appService.Shutdown();
                        return Task.CompletedTask;
                    }),
            });

            trayProvider.Initialize(menuItems);
        }


        private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
        {
            if (ServiceProvider != null!)
            {
                ServiceProvider.Dispose();
            }
        }

        public App()
        {
            VelopackApp.Build().Run();

            DeleteStaticAssets();
            ServiceProvider = GetServiceProvider();
            logger = ServiceProvider.GetRequiredService<ILogger<App>>();
            settingsService = ServiceProvider.GetRequiredService<ISettingsService>();
            interprocessService = ServiceProvider.GetRequiredService<IInterprocessService>();
            trayProvider = ServiceProvider.GetRequiredService<ITrayProvider>();
            appService = ServiceProvider.GetRequiredService<IApplicationService>();
        }


        private async Task HandleInterprocessCommunications(string[] startupArgs)
        {
            if (HasApplicationStartedUsingSidekickProtocol(startupArgs) && interprocessService.IsAlreadyRunning())
            {
                // If we reach here, that means the application was started using a sidekick:// link. We send a message to the already running instance in this case and close this new instance after.
                try
                {
                    await interprocessService.SendMessage(startupArgs[0]);
                }
                finally
                {
                    logger.LogDebug("[Startup] Application is shutting down due to another instance running.");
                    ShutdownAndExit();
                }
            }

            // Wait a second before starting to listen to interprocess communications.
            // This is necessary as when we are restarting as admin, the old non-admin instance is still running for a fraction of a second.
            await Task.Delay(2000);
            if (interprocessService.IsAlreadyRunning())
            {
                logger.LogDebug("[Startup] Application is already running.");
                var viewLocator = ServiceProvider.GetRequiredService<IViewLocator>();
                await viewLocator.CloseAll();
                var sidekickDialogs = ServiceProvider.GetRequiredService<ISidekickDialogs>();
                await sidekickDialogs.OpenOkModal("Another instance of Sidekick is already running. Make sure to close all instances of Sidekick inside the Task Manager.");
                logger.LogDebug("[Startup] Application is shutting down due to another instance running.");
                ShutdownAndExit();
            }
        }

        private bool HasApplicationStartedUsingSidekickProtocol(string[] startupArgs)
        {
            return startupArgs.Length > 0 && startupArgs[0].StartsWith("SIDEKICK://", StringComparison.CurrentCultureIgnoreCase);
        }

        private void ShutdownAndExit()
        {
            Dispatcher.UIThread.InvokeShutdown();
            Environment.Exit(0);
        }

        private ServiceProvider GetServiceProvider()
        {
            var services = new ServiceCollection();

            services.AddLocalization();

            services

                // Common
                .AddSidekickCommon()
                .AddSidekickCommonBlazor()
                .AddSidekickCommonDatabase(SidekickPaths.DatabasePath)
                .AddSidekickCommonUi()
                .AddSidekickCommonPlatform(o =>
                {
                    o.WindowsIconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets/favicon.ico");
                })

                // Apis
                .AddSidekickGitHubApi()
                .AddSidekickPoeApi()
                .AddSidekickPoeNinjaApi()
                .AddSidekickPoePriceInfoApi()
                .AddSidekickPoeWikiApi()
                .AddSidekickUpdater()

                // Modules
                .AddSidekickChat()
                .AddSidekickDevelopment()
                .AddSidekickGeneral()
                .AddSidekickMaps()
                .AddSidekickTrade()
                .AddSidekickWealth();

            services.AddSingleton<IApplicationService, MockApplicationService>();
            services.AddSingleton<ITrayProvider, AvaloniaTrayProvider>();
            services.AddSingleton<IViewLocator, AvaloniaViewLocator>();
            services.AddSingleton(sp => (AvaloniaViewLocator)sp.GetRequiredService<IViewLocator>());

            services.AddApexCharts();
            return services.BuildServiceProvider();
        }


        private void AttachErrorHandlers()
        {
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                LogException((Exception)e.ExceptionObject);
            };

            //TODO
            // Dispatcher.UnhandledException += (_, e) =>
            // {
            //     LogException(e.Exception);
            // };

            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                LogException(e.Exception);
            };
        }

        private void DeleteStaticAssets()
        {
            // While debugging, we do not want to delete static assets as the environment is different than when deployed.
            if (Debugger.IsAttached)
            {
                return;
            }

            try
            {
                var directory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()?.Location);
                if (directory == null)
                {
                    return;
                }

                var path = Path.Combine(directory, "Sidekick.staticwebassets.runtime.json");
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // If we fail to delete static assets, the app should not be stopped from running.
            }
        }
        
        private void LogException(Exception ex)
        {
            logger.LogCritical(ex, "Unhandled exception.");
        }
    }
