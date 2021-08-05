using System;
using System.Reflection;
using System.Threading.Tasks;
using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sidekick.Apis.GitHub;
using Sidekick.Application;
using Sidekick.Common.Platform;
using Sidekick.Common.Settings;
using Sidekick.Domain.Initialization.Commands;
using Sidekick.Domain.Views;
using Sidekick.Infrastructure;
using Sidekick.Localization;
using Sidekick.Logging;
using Sidekick.Mapper;
using Sidekick.Mediator;
using Sidekick.Mock.Platforms;
using Sidekick.Mock.Views;
using Sidekick.Modules.Cheatsheets;
using Sidekick.Modules.Settings;
using Sidekick.Persistence;

namespace Sidekick.Presentation.Blazor
{
    public class Startup
    {
        private readonly IConfiguration configuration;
        private readonly IHostEnvironment environment;

        public Startup(IConfiguration configuration, IHostEnvironment environment)
        {
            this.configuration = configuration;
            this.environment = environment;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            var mvcBuilder = services
                .AddRazorPages(options =>
                {
                    options.RootDirectory = "/Shared";
                })
                .AddFluentValidation(options =>
                {
                    options.RegisterValidatorsFromAssembly(Assembly.Load("Sidekick.Presentation.Blazor"));
                });
            services.AddServerSideBlazor();

            services
                // Layers
                .AddSidekickApplication(configuration)
                .AddSidekickInfrastructure()
                .AddSidekickLocalization()
                .AddSidekickPersistence()
                .AddSidekickPlatform()
                .AddSidekickPresentationBlazor()

                // Common
                .AddSidekickLogging(configuration, environment)
                .AddSidekickMapper()
                .AddSidekickMediator()

                // Apis
                .AddSidekickGitHubApi()

                // Modules
                .AddSidekickCheatsheets()
                .AddSidekickSettings(configuration);

            // Mock services
            services.AddSingleton<IViewLocator, MockViewLocator>();
            services.AddScoped<IViewInstance, MockViewInstance>();
            services.AddSingleton<IKeybindProvider, MockKeybindProvider>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider, ISettingsService settingsService, IMediator mediator)
        {
            serviceProvider.UseSidekickMapper();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });

            Task.Run(async () =>
            {
                await settingsService.Save(nameof(ISettings.Language_Parser), "en");
                await settingsService.Save(nameof(ISettings.Language_UI), "en");
                await settingsService.Save(nameof(ISettings.LeagueId), "Expedition");
                await mediator.Send(new InitializeCommand(true, false));
            });
        }
    }
}
