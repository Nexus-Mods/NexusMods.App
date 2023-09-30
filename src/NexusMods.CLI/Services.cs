using Microsoft.Extensions.DependencyInjection;
using NexusMods.CLI.OptionParsers;
using NexusMods.CLI.Verbs;
using NexusMods.Common;
using NexusMods.DataModel;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.DataModel.RateLimiting;
using NexusMods.FileExtractor.Extractors;
using NexusMods.Paths;
using NexusMods.Abstractions.CLI;
using NexusMods.CLI.Types;
using NexusMods.CLI.Types.DownloadHandlers;
using NexusMods.CLI.Types.IpcHandlers;
using NexusMods.Common.GuidedInstaller;
using NexusMods.Common.ProtocolRegistration;

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
        services.AddScoped<CommandLineConfigurator>();
        services.AddSingleton<IOptionParser<AbsolutePath>, AbsolutePathParser>();
        services.AddSingleton<IOptionParser<IGame>, GameParser>();
        services.AddSingleton<IOptionParser<LoadoutMarker>, LoadoutMarkerParser>();
        services.AddSingleton<IOptionParser<Version>, VersionParser>();
        services.AddSingleton<IOptionParser<Loadout>, LoadoutParser>();
        services.AddSingleton<IOptionParser<ITool>, ToolParser>();
        services.AddAllSingleton<IGuidedInstaller, CliGuidedInstaller>();

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

        services
            .AddVerb<AddGame>()
            .AddVerb<AnalyzeArchive>()
            .AddVerb<Apply>()
            .AddVerb<AssociateNxm>()
            .AddVerb<ChangeTracking>()
            .AddVerb<DownloadAndInstallMod>()
            .AddVerb<DownloadLinks>()
            .AddVerb<DownloadUri>()
            .AddVerb<ExportLoadout>()
            .AddVerb<ExtractArchive>()
            .AddVerb<FlattenList>()
            .AddVerb<HashFolder>()
            .AddVerb<InstallMod>()
            .AddVerb<ListGames>()
            .AddVerb<ListHistory>()
            .AddVerb<ListManagedGames>()
            .AddVerb<ListModContents>()
            .AddVerb<ListMods>()
            .AddVerb<ListTools>()
            .AddVerb<ManageGame>()
            .AddVerb<NexusGames>()
            .AddVerb<NexusLogin>()
            .AddVerb<NexusLogout>()
            .AddVerb<ProtocolInvoke>()
            .AddVerb<Rename>()
            .AddVerb<RunTool>()
            .AddVerb<SetNexusAPIKey>();

        services.AddAllSingleton<IResource, IResource<IExtractor, Size>>(_ => new Resource<IExtractor, Size>("File Extraction"));
        return services;
    }

}
