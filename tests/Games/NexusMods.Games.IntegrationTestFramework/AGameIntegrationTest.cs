using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.Serialization;
using NexusMods.Backend;
using NexusMods.DataModel;
using NexusMods.Games.CreationEngine;
using NexusMods.Games.FileHashes;
using NexusMods.Paths;
using NexusMods.Sdk.Settings;

namespace NexusMods.Games.IntegrationTestFramework;

public abstract class AGameIntegrationTest
{
    public const string GameImagesEnvVarName = "NMA_INTEGRATION_BASE_PATH";
    
    private readonly IHost _hosting;
    protected readonly Type GameType;

#region Imported Services
    public IFileSystem FileSystem { get; }
    public IGameRegistry GameRegistry { get; set; }
    public GameInstallation GameInstallation { get; set; }
    public ILoadoutSynchronizer Synchronizer { get; set; }
    
#endregion

    private record FauxLocator(GameLocatorResult LocatorResult) : IGameLocator
    { 
        public IEnumerable<GameLocatorResult> Find(ILocatableGame game, bool forceRefreshCache = false)
        {
            if (game is not ISteamGame steamGame) 
                yield break;
            if (LocatorResult.Metadata is not SteamLocatorResultMetadata steamMetadata) 
                yield break;
            
            if (!steamGame.SteamIds.Contains(steamMetadata.AppId))
                yield break;
            
            yield return LocatorResult;
        }
    }

    protected AGameIntegrationTest(Type gameType, GameLocatorResult locatorResult)
    {
        GameType = gameType;
        var locatorResult1 = locatorResult;

        var basePathEnv = Environment.GetEnvironmentVariable(GameImagesEnvVarName);
        if (basePathEnv is null)
            Assert.Fail($"{GameImagesEnvVarName} environment variable is not set, please set this to the path to the game images");
        
        var basePath = NexusMods.Paths.FileSystem.Shared.FromUnsanitizedFullPath(basePathEnv!);
        
        var gameArchives = GetArchives(locatorResult1).ToList();
        List<AbsolutePath> missingGameImages = [];
        foreach (var (src, mount) in gameArchives)
        {
            var absPath = basePath / src;
            if (!absPath.FileExists)
            {
                missingGameImages.Add(absPath);
            }
        }
        
        if (missingGameImages.Count > 0)
            ThrowMissingGameImagesError(missingGameImages);
        
        
        var overlays = gameArchives.Select(x => new NxReadOnlyFilesystem( basePath / x.Src, x.Mount)).ToArray();
        
        FileSystem = new ReadOnlySourcesFileSystem(locatorResult.GameFileSystem, overlays);
        
        // Remap the file system properties in the locator result to the new file system
        locatorResult = locatorResult with
        {
            GameFileSystem = FileSystem,
            Path = FileSystem.FromUnsanitizedFullPath(locatorResult.Path.ToString()),
        };
        
        if (!FileSystem.DirectoryExists(locatorResult.Path) || !FileSystem.EnumerateFiles(locatorResult.Path).Any())
            Assert.Fail($"The game archive is empty something was configured incorrectly with this test, please check the archive");
        
        _hosting = new HostBuilder()
            .ConfigureServices(s =>
            {
                s.AddSingleton<IFileSystem>(_ => FileSystem)
                 .AddSettingsManager()
                 .AddCreationEngine()
                 .AddDataModel()
                 .AddLibraryModels()
                 .AddOSInterop()
                 .AddFileHashes()
                 .AddHttpClient()
                 .AddJobMonitor()
                 .AddLoadoutAbstractions()
                 .AddSerializationAbstractions()
                 .AddSingleton<IGameLocator>(_ => new FauxLocator(locatorResult))
                 .OverrideSettingsForTests<DataModelSettings>(t =>
                     {
                         t.UseInMemoryDataModel = true;
                         return t;
                     }
                 );
            })
            .Build();
    }

    private void ThrowMissingGameImagesError(List<AbsolutePath> missingGameImages)
    {
        Console.WriteLine($"Missing game images (for {GetType().Name}: ");
        foreach (var missingGameImage in missingGameImages)
            Console.WriteLine("* " + missingGameImage);
        
        Assert.Fail($"Missing game images (for {GetType().Name}");
    }

    private IEnumerable<(RelativePath Src, AbsolutePath Mount)> GetArchives(GameLocatorResult locatorResult)
    {
        if (locatorResult.Store == GameStore.Steam)
        {
            foreach (var locatorId in locatorResult.Metadata.ToLocatorIds())
            {
                yield return (RelativePath.FromUnsanitizedInput("Steam/"  + locatorId + ".nx"), locatorResult.Path);
            }
            yield break;
        }
        throw new NotImplementedException();
    }

    [Before(Test)]
    public async Task Startup()
    {
        await _hosting.StartAsync();
        GameRegistry = _hosting.Services.GetRequiredService<IGameRegistry>();
        GameInstallation = GameRegistry.Installations.Values
            .Single(g => g.Game.GetType() == GameType);
        Synchronizer = GameInstallation.GetGame().Synchronizer;
    }


    public async Task<Loadout.ReadOnly> CreateLoadout()
    {
        return await Synchronizer.CreateLoadout(GameInstallation, "Test Loadout");
    }
}
