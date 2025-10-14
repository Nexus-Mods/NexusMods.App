using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.Serialization;
using NexusMods.Backend;
using NexusMods.DataModel;
using NexusMods.FileExtractor;
using NexusMods.Games.CreationEngine;
using NexusMods.Games.FileHashes;
using NexusMods.Library;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Paths;
using NexusMods.Sdk.Settings;

namespace NexusMods.Games.IntegrationTestFramework;

[Property("IntegrationTest", "True")]
public abstract class AGameIntegrationTest
{
    public const string GameImagesEnvVarName = "NMA_INTEGRATION_BASE_PATH";
    
    private readonly IHost _hosting;
    protected readonly Type GameType;



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
        
        FileSystem = new ReadOnlySourcesFileSystem(new InMemoryFileSystem(), overlays);
        
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
                 .AddSingleton<TemporaryFileManager>()
                 .AddFileExtractors()
                 .AddSettingsManager()
                 .AddCreationEngine()
                 .AddDataModel()
                 .AddLibraryModels()
                 .AddLibrary()
                 .AddNexusWebApi()
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
        ServiceProvider = _hosting.Services;
        Connection = ServiceProvider.GetRequiredService<IConnection>();
        GameRegistry = _hosting.Services.GetRequiredService<IGameRegistry>();
        GameInstallation = GameRegistry.Installations.Values
            .Single(g => g.Game.GetType() == GameType);
        Game = GameInstallation.Game;
        Synchronizer = GameInstallation.GetGame().Synchronizer;
        LoadoutManager = _hosting.Services.GetRequiredService<ILoadoutManager>();
        LibraryService = ServiceProvider.GetRequiredService<ILibraryService>();
        NexusModsLibrary = ServiceProvider.GetRequiredService<NexusModsLibrary>();
        TemporaryFileManager = ServiceProvider.GetRequiredService<TemporaryFileManager>();
    }



#region Imported Services
    public ILocatableGame Game { get; set; }

    protected IFileSystem FileSystem { get; }
    protected IGameRegistry GameRegistry { get; set; }
    protected GameInstallation GameInstallation { get; set; }
    protected ILoadoutSynchronizer Synchronizer { get; set; }
    
    protected ILoadoutManager LoadoutManager { get; set; }

    protected IConnection Connection { get; set; }

    protected IServiceProvider ServiceProvider { get; set; }
    
    public ILibraryService LibraryService { get; set; }

    public NexusModsLibrary NexusModsLibrary { get; set; }
    
    public TemporaryFileManager TemporaryFileManager { get; set; }
    
#endregion

    /// <summary>
    /// Formats the tuples into a markdown table and runs verify on the resulting data
    /// </summary>
    protected SettingsTask VerifyTable<T>(IEnumerable<T> table, string? name = null)
        where T : ITuple
    {
        List<List<string>> rows = new();
        Dictionary<int, int> cellWidths = new();

        foreach (var row in table)
        {
            var rowCells = new List<string>();
            rows.Add(rowCells);
            for (var cellIdx = 0; cellIdx < row.Length; cellIdx++)
            {
                var cellData = row[cellIdx]?.ToString() ?? string.Empty;
                ref var existingWidth = ref CollectionsMarshal.GetValueRefOrAddDefault(cellWidths, cellIdx, out var exists);
                existingWidth = Math.Max(existingWidth, cellData.Length);
                rowCells.Add(cellData);
            }
        }

        var sb = new StringBuilder();
        foreach (var row in rows)
        {
            sb.Append("| ");
            for (var i = 0; i < row.Count; i++)
            {
                sb.Append(row[i].PadRight(cellWidths[i]));
                sb.Append(" | ");
            }
            sb.AppendLine();
        }
        var task = Verify(sb.ToString(), extension: "md");
        if (name is not null)
            task.UseParameters("NAME", name);
        return task;
    }
    

    public async Task<Loadout.ReadOnly> CreateLoadout()
    {
        return await LoadoutManager.CreateLoadout(GameInstallation, "Test Loadout");
    }
}
