using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.CLI.Renderers;
using NexusMods.CLI;

namespace NexusMods.App;

public static class Services
{
    public static IServiceCollection AddRenderers(this IServiceCollection services)
    {
        services.AddScoped<IRenderer, CLI.Renderers.Spectre>();
        services.AddScoped<IRenderer, Json>();
        return services;
    }
}