using Microsoft.Extensions.DependencyInjection;
using NexusMods.CLI.OptionParsers;
using NexusMods.DataModel;
using NexusMods.DataModel.ModLists.Markers;
using NexusMods.Interfaces.Components;
using NexusMods.Paths;

namespace NexusMods.CLI;

public static class Services
{
    public static IServiceCollection AddCLI(this IServiceCollection services)
    {
        services.AddScoped<Configurator>();
        services.AddSingleton<CommandLineBuilder>();
        services.AddSingleton<IOptionParser<AbsolutePath>, AbsolutePathParser>();
        services.AddSingleton<IOptionParser<IGame>, GameParser>();
        services.AddSingleton<IOptionParser<ModListMarker>, ModListMarkerParser>();
        services.AddSingleton<IOptionParser<Version>, VersionParser>();
        services.AddDataModel();
        return services;
    }
    
}