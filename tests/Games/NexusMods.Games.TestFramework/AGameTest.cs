using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using DynamicData;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.GC;
using NexusMods.Abstractions.HttpDownloader;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.DataModel;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Paths;
using NexusMods.StandardGameLocators.TestHelpers;

namespace NexusMods.Games.TestFramework;

[PublicAPI]
public abstract class AGameTest<TGame> where TGame : AGame
{
    protected readonly IServiceProvider ServiceProvider;
    protected readonly TGame Game;
    protected readonly GameInstallation GameInstallation;

    protected readonly IFileSystem FileSystem;
    protected readonly TemporaryFileManager TemporaryFileManager;
    protected readonly IFileStore FileStore;
    protected readonly IGameRegistry GameRegistry;

    protected readonly IConnection Connection;

    protected readonly NexusApiClient NexusNexusApiClient;
    
    protected ILoadoutSynchronizer Synchronizer => GameInstallation.GetGame().Synchronizer;
    
    private readonly ILogger<AGameTest<TGame>> _logger;

    private bool _gameFilesWritten = false;
    
    public IDiagnosticManager DiagnosticManager { get; set; }


    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="serviceProvider"></param>
    protected AGameTest(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        
        GameRegistry = serviceProvider.GetRequiredService<IGameRegistry>();
        
        GameInstallation = GameRegistry.Installations.Values.First(g => g.Game is TGame);
        Game = (TGame)GameInstallation.Game;

        FileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        FileStore = serviceProvider.GetRequiredService<IFileStore>();
        TemporaryFileManager = serviceProvider.GetRequiredService<TemporaryFileManager>();
        Connection = serviceProvider.GetRequiredService<IConnection>();

        DiagnosticManager = serviceProvider.GetRequiredService<IDiagnosticManager>();

        NexusNexusApiClient = serviceProvider.GetRequiredService<NexusApiClient>();

        _logger = serviceProvider.GetRequiredService<ILogger<AGameTest<TGame>>>();
        if (GameInstallation.Locator is UniversalStubbedGameLocator<TGame> universal)
        {
            _logger.LogInformation("Resetting game files for {Game}", Game.Name);
            ResetGameFolders();
        }
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
    protected async Task<Loadout.ReadOnly> CreateLoadout(bool indexGameFiles = true)
    {
        if (!_gameFilesWritten)
        {
            await GenerateGameFiles();
            _gameFilesWritten = true;
        }
        return await GameInstallation.GetGame().Synchronizer.CreateLoadout(GameInstallation, Guid.NewGuid().ToString());
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
}
