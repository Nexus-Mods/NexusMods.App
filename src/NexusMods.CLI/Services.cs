using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.CLI;

public static class Services
{
    public static IServiceCollection AddCLI(this IServiceCollection services)
    {
        services.AddScoped<Configurator>();
        services.AddSingleton<CommandLineBuilder>();
        return services;
    }
    
}