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
using NexusMods.DataModel.GameRegistry;
using NexusMods.DataModel.Synchronizer;
using NexusMods.Games.CreationEngine;
using NexusMods.Games.FileHashes;
using NexusMods.Jobs;
using NexusMods.Paths;
using NexusMods.Sdk;
using NexusMods.Sdk.Settings;

namespace NexusMods.Games.IntegrationTestFramework;

public abstract class AGameIntegrationTest<TGame> 
   where TGame : IGame
{
    private readonly GameInfo _installedGame;
    private readonly IHost _hosting;
    private readonly IFileSystem _fileSystem;
    private readonly GameLocatorResult _locatorResult;

#region Imported Services
    public IFileSystem FileSystem { get; set; }
    public IGameRegistry GameRegistry { get; set; }
    public GameInstallation GameInstallation { get; set; }
    public ILoadoutSynchronizer Synchronizer { get; set; }
    
#endregion


    public record GameInfo(GameStore Store, string[] LocatorIds, Type GameType);

    private record FauxLocator(AGameIntegrationTest<TGame> IntegrationTest) : IGameLocator
    { 
        public IEnumerable<GameLocatorResult> Find(ILocatableGame game, bool forceRefreshCache = false)
        {
            return IntegrationTest.Locators;
        }
    }

    protected AGameIntegrationTest()
    {
        // Set the base filesystem
        FileSystem = new InMemoryFileSystem();

        var basePathEnv = Environment.GetEnvironmentVariable("NMA_INTEGRATION_BASE_PATH");
        if (basePathEnv is null)
            Assert.Fail("NMA_INTEGRATION_BASE_PATH environment variable is not set, please set this to the path to the game images");
        
        var basePath = NexusMods.Paths.FileSystem.Shared.FromUnsanitizedFullPath(basePathEnv!);
        
        var gameArchives = Locators.SelectMany(GetArchives).ToList();
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
        
        _fileSystem = new ReadOnlySourcesFileSystem(new InMemoryFileSystem(), overlays);
        
        _hosting = new HostBuilder()
            .ConfigureServices(s =>
            {
                s.AddSingleton<IFileSystem>(_ => _fileSystem)
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
                 .AddSingleton<IGameLocator>(_ => new FauxLocator(this))
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
        FileSystem = _hosting.Services.GetRequiredService<IFileSystem>();
        await _hosting.StartAsync();
        GameRegistry = _hosting.Services.GetRequiredService<IGameRegistry>();
        GameInstallation = GameRegistry.Installations.Values
            .Single(g => g.Game is TGame);
        Synchronizer = GameInstallation.GetGame().Synchronizer;
    }


    public async Task<Loadout.ReadOnly> CreateLoadout()
    {
        return await Synchronizer.CreateLoadout(GameInstallation, "Test Loadout");
    }
    
    protected abstract IEnumerable<GameLocatorResult> Locators { get; }

}
