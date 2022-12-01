using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel;

namespace NexusMods.CLI;

public static class Services
{
    public static IServiceCollection AddCLI(this IServiceCollection services)
    {
        services.AddScoped<Configurator>();
        services.AddSingleton<CommandLineBuilder>();
        services.AddDataModel();
        return services;
    }
    
}