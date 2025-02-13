using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using DynamicData.Kernel;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.FileExtractor;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.GC;
using NexusMods.Abstractions.GuidedInstallers;
using NexusMods.Abstractions.HttpDownloader;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.Serialization;
using NexusMods.App.BuildInfo;
using NexusMods.DataModel;
using NexusMods.Games.FOMOD;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Paths;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit.Abstractions;
using Xunit.DependencyInjection;

namespace NexusMods.Games.TestFramework;

[PublicAPI]
public abstract class AIsolatedGameTest<TTest, TGame> : IAsyncLifetime where TGame : AGame
{
    protected readonly ILogger Logger;
    protected readonly IServiceProvider ServiceProvider;
    protected TGame Game;
    protected GameInstallation GameInstallation;

    protected readonly IFileSystem FileSystem;
    protected readonly TemporaryFileManager TemporaryFileManager;
    protected readonly ILibraryService LibraryService;
    protected readonly IFileStore FileStore;
    protected readonly IFileExtractor FileExtractor;
    protected readonly IGameRegistry GameRegistry;
    protected readonly NexusModsLibrary NexusModsLibrary;


    protected readonly IConnection Connection;

    protected readonly NexusApiClient NexusNexusApiClient;
    protected ILoadoutSynchronizer Synchronizer => GameInstallation.GetGame().Synchronizer;
    
    private bool _gameFilesWritten = false;
    private readonly IHost _host;
    private readonly ITestOutputHelper _helper;
    protected readonly ConfigOptionsRecord ConfigOptions;

    public IDiagnosticManager DiagnosticManager { get; set; }


    private class Accessor : ITestOutputHelperAccessor
    {
        public ITestOutputHelper? Output { get; set; }
    }
    
    /// <summary>
    /// Constructor.
    /// </summary>
    protected AIsolatedGameTest(ITestOutputHelper helper)
    {
        ConfigOptions = new ConfigOptionsRecord();
        _helper = helper;
        _host = new HostBuilder()
            .ConfigureServices(s => AddServices(s))
            .Build();
        
        ServiceProvider = _host.Services;

        GameRegistry = ServiceProvider.GetRequiredService<IGameRegistry>();
        


        FileSystem = ServiceProvider.GetRequiredService<IFileSystem>();
        FileStore = ServiceProvider.GetRequiredService<IFileStore>();
        FileExtractor = ServiceProvider.GetRequiredService<IFileExtractor>();
        TemporaryFileManager = ServiceProvider.GetRequiredService<TemporaryFileManager>();
        Connection = ServiceProvider.GetRequiredService<IConnection>();

        DiagnosticManager = ServiceProvider.GetRequiredService<IDiagnosticManager>();

        NexusNexusApiClient = ServiceProvider.GetRequiredService<NexusApiClient>();
        Logger = ServiceProvider.GetRequiredService<ILogger<TTest>>();
        LibraryService = ServiceProvider.GetRequiredService<ILibraryService>();
        NexusModsLibrary = ServiceProvider.GetRequiredService<NexusModsLibrary>();
    }
    
    public async Task<LibraryArchive.ReadOnly> RegisterLocalArchive(AbsolutePath file)
    {
        var libraryFile = await LibraryService.AddLocalFile(file);
        if (!libraryFile.AsLibraryFile().TryGetAsLibraryArchive(out var archive))
            throw new InvalidOperationException("The library file should be an archive.");
        return archive;
    }
    
    public record ConfigOptionsRecord
    {
        public bool RegisterNullGuidedInstaller { get; set; } = true;
    }
    
    protected virtual IServiceCollection AddServices(IServiceCollection services)
    {
        if (ConfigOptions.RegisterNullGuidedInstaller)
            services.AddSingleton<IGuidedInstaller, NullGuidedInstaller>();
        
        return services
            .AddDefaultServicesForTesting()
            .AddFomod()
            .AddLogging(builder => builder.AddXUnit())
            .AddGames()
            .AddLoadoutAbstractions()
            .AddSingleton<ITestOutputHelperAccessor>(_ => new Accessor { Output = _helper })
            .Validate();
    }

    /// <summary>
    /// Override this method to generate the game files for the tests in this class. 
    /// </summary>
    protected virtual Task GenerateGameFiles()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Adds an empty mod to the loadout in the given transaction.
    /// </summary>
    protected LoadoutItemGroupId AddEmptyGroup(ITransaction tx, LoadoutId loadoutId, string name)
    {
        var mod = new LoadoutItemGroup.New(tx, out var id)
        {
            IsGroup = true,
            LoadoutItem = new LoadoutItem.New(tx, id)
            {
                LoadoutId = loadoutId,
                Name = name,
                
            },
        };
        return mod.Id;
    }
    
    /// <summary>
    /// Creates a file in the loadout for the given mod.
    /// The file will be named with the given path, the hash will be the hash
    /// of the name, and the size will be the length of the name. 
    /// </summary>
    public LoadoutFileId AddFile(ITransaction tx, LoadoutId loadoutId, LoadoutItemGroupId groupId, GamePath path, string? content = null)
    {
        return AddFile(tx, loadoutId, groupId, path, content, out _, out _);
    }
    
    /// <summary>
    /// Creates a file in the loadout for the given mod.
    /// The file will be named with the given path, the hash will be the hash
    /// of the name, and the size will be the length of the name. 
    /// </summary>
    public LoadoutFileId AddFile(ITransaction tx, LoadoutId loadoutId, LoadoutItemGroupId groupId, GamePath path, string? content, out Hash hash, out Size size)
    {
        content ??= path.Path.ToString();
        var contentArray = Encoding.UTF8.GetBytes(content);

        hash = contentArray.xxHash3();
        size = Size.FromLong(contentArray.Length);
        return AddFileInternal(tx, loadoutId, groupId, path, hash, size).Id;
    }
    private static LoadoutFile.New AddFileInternal(ITransaction tx, LoadoutId loadoutId, LoadoutItemGroupId groupId, GamePath path, Hash hash, Size size) => new(tx, out var id)
    {
        LoadoutItemWithTargetPath = new LoadoutItemWithTargetPath.New(tx, id)
        {
            LoadoutItem = new LoadoutItem.New(tx, id)
            {
                LoadoutId = loadoutId,
                ParentId = groupId,
                Name = path.Path,
            },
            TargetPath = path.ToGamePathParentTuple(loadoutId),
        },
        Hash = hash,
        Size = size,
    };

    /// <summary>
    /// Creates a <see cref="LibraryArchive"/> to simulate an item in the library.
    /// This item can be used in conjunction with <see cref="AddModAsync"/> to simulate
    /// adding a mod from the library.
    /// </summary>
    /// <param name="conn">The connection with the DataStore</param>
    /// <param name="fileName">Name of the archive.</param>
    public async Task<LibraryArchive.ReadOnly> CreateLibraryArchive(IConnection conn, string fileName)
    {
        using var tx = conn.BeginTransaction();
        var libraryFile = CreateLibraryFile(fileName, tx, out var entityId);
        
        // Note(sewer): These are the same entity, just an API caveat that 2 objects are needed.
        var archive = new LibraryArchive.New(tx, entityId)
        {
            LibraryFile = libraryFile,
            IsArchive = true,
        };

        var result = await tx.Commit();
        return result.Remap(archive);
    }

    /// <summary>
    /// Creates a file in the loadout for the given mod.
    /// The file will be named with the given path, the hash will be the hash
    /// of the name, and the size will be the length of the name. 
    /// </summary>
    /// <param name="tx">The transaction.</param>
    /// <param name="paths">The names of the file paths to add. The file contents will be the UTF-8 of these paths.</param>
    /// <param name="loadoutId">The loadout to add the mod to.</param>
    /// <param name="modName">Name of the mod in the loadout.</param>
    /// <param name="libraryArchive">
    ///     The library item created with <see cref="CreateLibraryArchive"/> to attach the mod files to.
    ///     Specifying this parameter will add DB entries to simulate this being the original archive which
    ///     the files have come from.
    /// </param>
    public async Task<(AbsolutePath archivePath, List<Hash> hashes)> AddModAsync(ITransaction tx, IEnumerable<RelativePath> paths, LoadoutId loadoutId, string modName, LibraryArchive.ReadOnly? libraryArchive = null)
    {
        var records = new List<ArchivedFileEntry>();
        var hashes = new List<Hash>();
        var modGroup = AddEmptyGroup(tx, loadoutId, modName);
        foreach (var path in paths)
        {
            var data = Encoding.UTF8.GetBytes(path);
            var hash = data.xxHash3();
            var size = Size.FromLong(path.Path.Length);
            
            // Create the LoadoutFile in DB
            AddFileInternal(tx, loadoutId, modGroup, new GamePath(LocationId.Game, path), hash, size);
            
            // Create the file to backup.
            hashes.Add(hash);
            if (!await FileStore.HaveFile(hash))
            {
                records.Add(new ArchivedFileEntry(
                    new MemoryStreamFactory(path, new MemoryStream(data)),
                    hash,
                    size
                ));
            }

            if (libraryArchive == null)
                continue;

            var libraryArchiveFileEntry = CreateLibraryFile(path.Path, tx, out _);
            _ = new LibraryArchiveFileEntry.New(tx, libraryArchiveFileEntry.Id)
            {
                Path = path,
                ParentId = libraryArchive,
                LibraryFile = libraryArchiveFileEntry,
            };
        }

        if (records.Count > 0)
            await FileStore.BackupFiles(records);

        return (GetArchivePath(hashes.First()), hashes);
    }

    private static LibraryFile.New CreateLibraryFile(string fileName, ITransaction tx, out EntityId entityId) => new(tx, out entityId)
    {
        FileName = fileName,
        Hash = fileName.xxHash3AsUtf8(),
        Size = Size.FromLong(fileName.Length),
        LibraryItem = new LibraryItem.New(tx, entityId)
        {
            Name = fileName,
        },
    };
    
    /// <summary>
    /// Resets the game folders to a clean state.
    /// </summary>
    private void ResetGameFolders()
    {
        var register = GameInstallation.LocationsRegister;
        var oldLocations = register.GetTopLevelLocations().ToArray();
        var newLocations = new Dictionary<LocationId, AbsolutePath>();
        foreach (var (k, _) in oldLocations)
        {
            newLocations[k] = TemporaryFileManager.CreateFolder().Path;
        }
        register.Reset(newLocations);
        _gameFilesWritten = false;
    }

    /// <summary>
    /// Creates a new loadout and returns the <see cref="Loadout.ReadOnly"/> of it.
    /// </summary>
    protected async Task<Loadout.ReadOnly> CreateLoadout()
    {
        return await GameInstallation
            .GetGame()
            .Synchronizer
            .CreateLoadout(GameInstallation, Guid.NewGuid().ToString());
    }

    /// <summary>
    /// Deletes a loadout with a given ID.
    /// </summary>
    protected Task DeleteLoadoutAsync(LoadoutId loadoutId, GarbageCollectorRunMode gcRunMode = GarbageCollectorRunMode.DoNotRun) => GameInstallation.GetGame().Synchronizer.DeleteLoadout(loadoutId, gcRunMode);

    /// <summary>
    /// Reloads the entity from the database.
    /// </summary>
    protected T Refresh<T>(T entity) where T : IReadOnlyModel<T>
        => T.Create(Connection.Db, entity.Id);
    
    /// <summary>
    /// Reloads the entity from the database.
    /// </summary>
    protected void Refresh<T>(ref T entity) where T : IReadOnlyModel<T>
        => entity = T.Create(Connection.Db, entity.Id);

    /// <summary>
    /// Creates a ZIP archive using <see cref="ZipArchive"/> and returns the
    /// <see cref="TemporaryPath"/> to it.
    /// </summary>
    /// <param name="filesToZip"></param>
    /// <returns></returns>
    protected async Task<TemporaryPath> CreateTestArchive(IDictionary<RelativePath, byte[]> filesToZip)
    {
        var file = TemporaryFileManager.CreateFile();

        await using var stream = file.Path.Create();

        // Don't put this in Create mode, because for some reason it will create broken Zips that are not prefixed
        // with the ZIP magic number. Not sure why and I can't reproduce it in a simple test case, but if you open
        // in create mode all your zip archives will be prefixed with 0x0000FFFF04034B50 instead of 0x04034B50.
        // See https://github.com/dotnet/runtime/blob/23886f158cf925e13c72e661b9891df704806746/src/libraries/System.IO.Compression/src/System/IO/Compression/ZipArchiveEntry.cs#L949-L956
        // for where this bug occurs
        using var zipArchive = new ZipArchive(stream, ZipArchiveMode.Update);

        foreach (var kv in filesToZip)
        {
            var (path, contents) = kv;

            var entry = zipArchive.CreateEntry(path.Path, CompressionLevel.Fastest);
            await using var entryStream = entry.Open();
            await using var ms = new MemoryStream(contents);
            await ms.CopyToAsync(entryStream);
            await entryStream.FlushAsync();
        }

        await stream.FlushAsync();
        return file;
    }

    protected async Task<TemporaryPath> CreateTestFile(string fileName, byte[] contents)
    {
        var folder = TemporaryFileManager.CreateFolder();
        var path = folder.Path.Combine(fileName);
        var file = new TemporaryPath(FileSystem, path);

        await path.WriteAllBytesAsync(contents);
        return file;
    }

    protected Task<TemporaryPath> CreateTestFile(string fileName, string contents, Encoding? encoding = null)
        => CreateTestFile(fileName, (encoding ?? Encoding.UTF8).GetBytes(contents));

    protected async Task<TemporaryPath> CreateTestFile(byte[] contents, Extension? extension)
    {
        var file = TemporaryFileManager.CreateFile(extension);
        await file.Path.WriteAllBytesAsync(contents);
        return file;
    }

    protected Task<TemporaryPath> CreateTestFile(string contents, Extension? extension, Encoding? encoding = null)
        => CreateTestFile((encoding ?? Encoding.UTF8).GetBytes(contents), extension);
    
    private AbsolutePath GetArchivePath(Hash hash)
    {
        if (FileStore is not NxFileStore store)
            throw new NotSupportedException("GetArchivePath is not currently supported in stubbed file stores.");

        store.TryGetLocation(Connection.Db, hash, null, out var archivePath, out _).Should().BeTrue("Archive should exist");
        return archivePath;
    }
    
    /// <summary>
    /// Prints the disk states and the loadout states to the string builder, for verify purposes.
    /// </summary>
    protected void LogDiskState(StringBuilder sb, string sectionName, string comments = "", Loadout.ReadOnly[]? loadouts = null)
    {
        Logger.LogInformation("Logging State {SectionName}", sectionName);
        
        var metadata = GameInstallation.GetMetadata(Connection);
        sb.AppendLine($"{sectionName}:");
        if (!string.IsNullOrEmpty(comments))
            sb.AppendLine(comments);
        
        Section("### Initial State", metadata.InitialDiskStateTransaction);
        if (metadata.Contains(GameInstallMetadata.LastSyncedLoadoutTransaction)) 
            Section("### Last Synced State", metadata.LastSyncedLoadoutTransaction);
        Section("### Current State", metadata.LastScannedDiskStateTransaction);
        if (loadouts is not null)
        {
            foreach (var loadout in loadouts)
            {
                if (!loadout.Items.Any())
                    continue;

                var files = loadout.Items.OfTypeLoadoutItemWithTargetPath().ToArray();
                
                sb.AppendLine($"### Loadout {loadout.ShortName} - ({files.Length})");
                sb.AppendLine("| Path | Hash | Size | Disabled | Deleted |");
                sb.AppendLine("| --- | --- | --- | --- | --- |");
                foreach (var entry in files.OrderBy(f => f.TargetPath))
                {
                    var disabled = entry.AsLoadoutItem().GetThisAndParents().Any(p => p.IsDisabled) ? "Disabled" : " ";
                    var deleted = entry.TryGetAsDeletedFile(out _) ? "Deleted" : " ";

                    var hash = "";
                    var size = "";
                    if (entry.TryGetAsLoadoutFile(out var loadoutFile))
                    {
                        hash = loadoutFile.Hash.ToString();
                        size = loadoutFile.Size.ToString();
                    }
                    
                    sb.AppendLine($"| {FmtPath(entry.TargetPath)} | {hash} | {size} | {disabled} | {deleted} |");
                }
            }
        }
        
        sb.AppendLine("\n\n");

        
        
        void Section(string sectionName, Transaction.ReadOnly asOf)
        {
            var entries = metadata.DiskStateAsOf(asOf);
            sb.AppendLine($"{sectionName} - ({entries.Count})");
            sb.AppendLine("| Path | Hash | Size |");
            sb.AppendLine("| --- | --- | --- |");
            foreach (var entry in entries.OrderBy(e=> e.Path)) 
                sb.AppendLine($"| {FmtPath(entry.Path)} | {entry.Hash} | {entry.Size} |");
        }
        
        static string FmtPath((EntityId entityId, LocationId locationId, RelativePath relativePath) targetPath)
        {
            return $"{{{targetPath.locationId}, {targetPath.relativePath}}}";
        }
    }

    public async Task InitializeAsync()
    {
        await _host.StartAsync();
        GameInstallation = GameRegistry.Installations.Values.First(g => g.Game is TGame);
        Game = (TGame)GameInstallation.Game;
        
        if (GameInstallation.Locator is UniversalStubbedGameLocator<TGame> universal)
        {
            Logger.LogInformation("Resetting game files for {Game}", Game.Name);
            ResetGameFolders();
        }
    }

    public async Task DisposeAsync()
    {
        await _host.StopAsync();
        _host.Dispose();
    }
}
