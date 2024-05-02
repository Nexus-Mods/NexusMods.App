using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.CLI.OptionParsers;
using NexusMods.CLI.Types;
using NexusMods.CLI.Types.DownloadHandlers;
using NexusMods.CLI.Types.IpcHandlers;
using NexusMods.Paths;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

namespace NexusMods.CLI;

/// <summary>
/// Extension class for <see cref="IServiceCollection"/>
/// </summary>
public static class Services
{
    // ReSharper disable once InconsistentNaming
    /// <summary>
    /// Adds the CLI services to the <see cref="IServiceCollection"/>
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddCLI(this IServiceCollection services)
    {
        services.AddOptionParser<AbsolutePath, AbsolutePathParser>()
                .AddOptionParser<IGame, GameParser>()
                .AddOptionParser<Loadout.Model, LoadoutParser>()
                .AddOptionParser<Uri>(u => (new Uri(u), null))
                .AddOptionParser<Version>(v => (Version.Parse(v), null))
                .AddOptionParser<string>(s => (s, null))
                .AddOptionParser<ITool, ToolParser>();

        // Protocol Handlers
        services.AddSingleton<IIpcProtocolHandler, NxmIpcProtocolHandler>();
        services.AddSingleton<IDownloadProtocolHandler, NxmDownloadProtocolHandler>();

        services.AddProtocolVerbs();
        return services;
    }

}
