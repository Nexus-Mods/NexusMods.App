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
using System.Runtime.InteropServices;
using NexusMods.CLI.Types;
using NexusMods.CLI.Types.DownloadHandlers;
using NexusMods.CLI.Types.IpcHandlers;
using NexusMods.Common.ProtocolRegistration;
using NexusMods.Common.UserInput;

namespace NexusMods.CLI;

public static class Services
{
    // ReSharper disable once InconsistentNaming
    public static IServiceCollection AddCLI(this IServiceCollection services)
    {
        services.AddScoped<Configurator>();
        services.AddScoped<CommandLineConfigurator>();
        services.AddSingleton<IOptionParser<AbsolutePath>, AbsolutePathParser>();
        services.AddSingleton<IOptionParser<IGame>, GameParser>();
        services.AddSingleton<IOptionParser<LoadoutMarker>, LoadoutMarkerParser>();
        services.AddSingleton<IOptionParser<Version>, VersionParser>();
        services.AddSingleton<IOptionParser<Loadout>, LoadoutParser>();
        services.AddSingleton<IOptionParser<ITool>, ToolParser>();
        services.AddSingleton<IOptionSelector, CliOptionSelector>();
        services.AddSingleton<TemporaryFileManager>();

        OSInformation.Shared.SwitchPlatform(
            ref services,
            onWindows: (ref IServiceCollection value) => value.AddSingleton<IProtocolRegistration, ProtocolRegistrationWindows>(),
            onLinux: (ref IServiceCollection value) => value.AddSingleton<IProtocolRegistration, ProtocolRegistrationLinux>()
        );

        // Protocol Handlers
        services.AddSingleton<IIpcProtocolHandler, NxmIpcProtocolHandler>();
        services.AddSingleton<IDownloadProtocolHandler, NxmDownloadProtocolHandler>();
        
        services.AddVerb<AnalyzeArchive>()
            .AddVerb<Apply>()
            .AddVerb<ChangeTracking>()
            .AddVerb<ExtractArchive>()
            .AddVerb<ExportLoadout>()
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
            .AddVerb<ProtocolInvoke>()
            .AddVerb<Rename>()
            .AddVerb<RunTool>()
            .AddVerb<DownloadUri>()
            .AddVerb<AssociateNxm>()
            .AddVerb<DownloadAndInstallMod>()
            .AddVerb<SetNexusAPIKey>()
            .AddVerb<NexusApiVerify>()
            .AddVerb<NexusGames>()
            .AddVerb<DownloadLinks>()
            .AddVerb<NexusLogin>()
            .AddVerb<NexusLogout>();

        services.AddAllSingleton<IResource, IResource<IExtractor, Size>>(_ => new Resource<IExtractor, Size>("File Extraction"));
        services.AddAllSingleton<IResource, IResource<ArchiveAnalyzer, Size>>(_ => new Resource<ArchiveAnalyzer, Size>("File Analysis"));
        return services;
    }

}
