using Microsoft.Extensions.DependencyInjection;

namespace Sidekick.Avalonia.BlazorWebView;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBlazorWebViewOptions(this IServiceCollection services, BlazorWebViewOptions options)
    {
        return services
            .AddBlazorWebView()
            .AddSingleton(options);
    }
}
