using Microsoft.Extensions.DependencyInjection;
using NexusMods.CLI.OptionParsers;
using NexusMods.CLI.Verbs;
using NexusMods.Common;
using NexusMods.DataModel;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.DataModel.RateLimiting;
using NexusMods.FileExtractor;
using NexusMods.FileExtractor.Extractors;
using NexusMods.Paths;

namespace NexusMods.CLI;

public static class Services
{
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
        services.AddSingleton<TemporaryFileManager>();

        services.AddVerb<AnalyzeArchive>(AnalyzeArchive.Definition)
            .AddVerb<Apply>(Apply.Definition)
            .AddVerb<ChangeTracking>(ChangeTracking.Definition)
            .AddVerb<ExtractArchive>(ExtractArchive.Definition)
            .AddVerb<FlattenList>(FlattenList.Definition)
            .AddVerb<HashFolder>(HashFolder.Definition)
            .AddVerb<InstallMod>(InstallMod.Definition)
            .AddVerb<ListGames>(ListGames.Definition)
            .AddVerb<ListHistory>(ListHistory.Definition)
            .AddVerb<ListManagedGames>(ListManagedGames.Definition)
            .AddVerb<ListModContents>(ListModContents.Definition)
            .AddVerb<ListMods>(ListMods.Definition)
            .AddVerb<ListTools>(ListTools.Definition)
            .AddVerb<ManageGame>(ManageGame.Definition)
            .AddVerb<Rename>(Rename.Definition)
            .AddVerb<RunTool>(RunTool.Definition);
        
        services.AddAllSingleton<IResource, IResource<IExtractor, Size>>(s => new Resource<IExtractor, Size>("File Extraction"));
        services.AddAllSingleton<IResource, IResource<FileContentsCache, Size>>(s => new Resource<FileContentsCache, Size>("File Analysis"));
        services.AddFileExtractors();
        services.AddDataModel();
        return services;
    }
    
}