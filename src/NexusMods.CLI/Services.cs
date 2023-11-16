using Microsoft.Extensions.DependencyInjection;
using NexusMods.CLI.OptionParsers;
using NexusMods.Common;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.DataModel.RateLimiting;
using NexusMods.FileExtractor.Extractors;
using NexusMods.Paths;
using NexusMods.CLI.Types;
using NexusMods.CLI.Types.DownloadHandlers;
using NexusMods.CLI.Types.IpcHandlers;
using NexusMods.Common.ProtocolRegistration;
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
                .AddOptionParser<LoadoutMarker, LoadoutMarkerParser>()
                .AddOptionParser<Version>(v => (Version.Parse(v), null))
                .AddOptionParser<Loadout, LoadoutParser>()
                .AddOptionParser<ITool, ToolParser>();

        OSInformation.Shared.SwitchPlatform(
            ref services,
#pragma warning disable CA1416
            onWindows: (ref IServiceCollection value) => value.AddSingleton<IProtocolRegistration, ProtocolRegistrationWindows>(),
            onLinux: (ref IServiceCollection value) => value.AddSingleton<IProtocolRegistration, ProtocolRegistrationLinux>()
#pragma warning restore CA1416
        );

        // Protocol Handlers
        services.AddSingleton<IIpcProtocolHandler, NxmIpcProtocolHandler>();
        services.AddSingleton<IDownloadProtocolHandler, NxmDownloadProtocolHandler>();

        services.AddProtocolVerbs();
        services.AddAllSingleton<IResource, IResource<IExtractor, Size>>(_ => new Resource<IExtractor, Size>("File Extraction"));
        return services;
    }

}
