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

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            services.AddSingleton<IProtocolRegistration, ProtocolRegistrationWindows>();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            services.AddSingleton<IProtocolRegistration, ProtocolRegistrationLinux>();
        }
        else
        {
            throw new PlatformNotSupportedException("This platform does not support protocol registration");
        }

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
            .AddVerb<RunTool>();

        services.AddAllSingleton<IResource, IResource<IExtractor, Size>>(_ => new Resource<IExtractor, Size>("File Extraction"));
        services.AddAllSingleton<IResource, IResource<FileContentsCache, Size>>(_ => new Resource<FileContentsCache, Size>("File Analysis"));
        return services;
    }

}
