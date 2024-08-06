using System.IO.Compression;
using System.Text;
using DynamicData;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.HttpDownloader;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.DataModel;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions;
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
    protected readonly IHttpDownloader HttpDownloader;
    
    protected ILoadoutSynchronizer Synchronizer => GameInstallation.GetGame().Synchronizer;
    
    private readonly ILogger<AGameTest<TGame>> _logger;
    
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
        HttpDownloader = serviceProvider.GetRequiredService<IHttpDownloader>();

        _logger = serviceProvider.GetRequiredService<ILogger<AGameTest<TGame>>>();
        if (GameInstallation.Locator is UniversalStubbedGameLocator<TGame> universal)
        {
            _logger.LogInformation("Resetting game files for {Game}", Game.Name);
            ResetGameFolders();
        }
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
    public LoadoutFileId AddFile(ITransaction tx, LoadoutId loadoutId, LoadoutItemGroupId groupId, GamePath path)
    {
        return AddFile(tx, loadoutId, groupId, path, out _, out _);
    }
    
    /// <summary>
    /// Creates a file in the loadout for the given mod.
    /// The file will be named with the given path, the hash will be the hash
    /// of the name, and the size will be the length of the name. 
    /// </summary>
    public LoadoutFileId AddFile(ITransaction tx, LoadoutId loadoutId, LoadoutItemGroupId groupId, GamePath path, out Hash hash, out Size size)
    {
        hash = path.Path.ToString().XxHash64AsUtf8();
        size = Size.FromLong(path.Path.ToString().Length);
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
            TargetPath = path,
        },
        Hash = hash,
        Size = size,
    };

    /// <summary>
    /// Creates a file in the loadout for the given mod.
    /// The file will be named with the given path, the hash will be the hash
    /// of the name, and the size will be the length of the name. 
    /// </summary>
    public async Task<(AbsolutePath archivePath, List<Hash> hashes)> AddModAsync(ITransaction tx, IEnumerable<RelativePath> paths, LoadoutId loadoutId, string modName)
    {
        var records = new List<ArchivedFileEntry>();
        var hashes = new List<Hash>();
        var modGroup = AddEmptyGroup(tx, loadoutId, modName);
        foreach (var path in paths)
        {
            var data = Encoding.UTF8.GetBytes(path);
            var hash = path.Path.XxHash64AsUtf8();
            var size = Size.FromLong(path.Path.Length);
            
            // Create the LoadoutFile in DB
            var file = AddFileInternal(tx, loadoutId, modGroup, new GamePath(LocationId.Game, path), hash, size);
            
            // Create the file to backup.
            records.Add(new ArchivedFileEntry(
                new MemoryStreamFactory(path, new MemoryStream(data)),
                hash,
                size
            ));
            
            hashes.Add(hash);
        }

        await FileStore.BackupFiles(records);
        return (GetArchivePath(records[0].Hash), hashes);
    }
    
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
    }

    /// <summary>
    /// Creates a new loadout and returns the <see cref="Loadout.ReadOnly"/> of it.
    /// </summary>
    protected async Task<Loadout.ReadOnly> CreateLoadout(bool indexGameFiles = true)
    {
        return await GameInstallation.GetGame().Synchronizer.CreateLoadout(GameInstallation, Guid.NewGuid().ToString());
    }
    
    /// <summary>
    /// Deletes a loadout with a given ID.
    /// </summary>
    protected Task DeleteLoadoutAsync(LoadoutId loadoutId) => GameInstallation.GetGame().Synchronizer.DeleteLoadout(GameInstallation, loadoutId);

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
