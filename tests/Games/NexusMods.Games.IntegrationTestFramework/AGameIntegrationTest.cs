using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using FomodInstaller.Interface;
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
using NexusMods.Collections;
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
public abstract class AGameIntegrationTest : IDisposable
{
    public const string GameImagesEnvVarName = "NMA_INTEGRATION_BASE_PATH";
    
    private readonly IHost _hosting;
    protected readonly Type GameType;
    private readonly IFileSystem _baseFileSystem;
    private readonly TemporaryFileManager _baseTempFileManager;
    private readonly TemporaryPath _baseFolder;
    private readonly IFileSystem _mappedFileSystem;


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
        _baseFileSystem = NexusMods.Paths.FileSystem.Shared;
        _baseTempFileManager = new TemporaryFileManager(_baseFileSystem);
        _baseFolder = _baseTempFileManager.CreateFolder();

        Dictionary<AbsolutePath, AbsolutePath> pathMappings = new();
        foreach (var known in new[]
                 {
                     KnownPath.ProgramFilesDirectory, 
                     KnownPath.ApplicationDataDirectory, 
                     KnownPath.MyDocumentsDirectory, 
                     KnownPath.HomeDirectory,
                     KnownPath.TempDirectory,
                 })
        {
            var actualPath = _baseFileSystem.GetKnownPath(known);
            var mappedPath = _baseFolder.Path / known.ToString();
            mappedPath.CreateDirectory();
            pathMappings.Add(actualPath, mappedPath);
        }

        _mappedFileSystem = _baseFileSystem
            .CreateOverlayFileSystem(pathMappings, []);
        
        GameType = gameType;
        var locatorResult1 = locatorResult;

        var basePathEnv = Environment.GetEnvironmentVariable(GameImagesEnvVarName);
        if (basePathEnv is null)
            Assert.Fail($"{GameImagesEnvVarName} environment variable is not set, please set this to the path to the game images");
        
        var basePath = NexusMods.Paths.FileSystem.Shared.FromUnsanitizedFullPath(basePathEnv!);
        
        var gameArchives = GetArchives(locatorResult1, _mappedFileSystem).ToList();
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
        
        
        var overlays = gameArchives
            .Select(x => new NxReadOnlyFilesystem( basePath / x.Src, x.Mount)).ToArray();
        
        FileSystem = new ReadOnlySourcesFileSystem(_mappedFileSystem, overlays)
            .CreateOverlayFileSystem(pathMappings, []);
        
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
                 .AddNexusModsCollections()
                 .AddSingleton<ICoreDelegates, MockDelegates>()
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

    private IEnumerable<(RelativePath Src, AbsolutePath Mount)> GetArchives(GameLocatorResult locatorResult, IFileSystem fileSystem)
    {
        if (locatorResult.Store == GameStore.Steam)
        {
            foreach (var locatorId in locatorResult.Metadata.ToLocatorIds())
            {
                yield return (RelativePath.FromUnsanitizedInput("Steam/"  + locatorId + ".nx"), fileSystem.Map(locatorResult.Path));
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
    protected string Table<T>(IEnumerable<T> table)
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

        return sb.ToString();
    }
    

    public async Task<Loadout.ReadOnly> CreateLoadout()
    {
        return await LoadoutManager.CreateLoadout(GameInstallation, "Test Loadout");
    }

    public void Dispose()
    {
        _hosting.Dispose();
        _baseTempFileManager.Dispose();
    }
}
