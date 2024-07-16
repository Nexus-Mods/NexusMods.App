using System.IO.Compression;
using System.Text;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.FileStore.Downloads;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.HttpDownloader;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Networking.NexusWebApi.Extensions;
using NexusMods.Paths;
using NexusMods.StandardGameLocators.TestHelpers;
using File = NexusMods.Abstractions.Loadouts.Files.File;
using FileId = NexusMods.Abstractions.NexusWebApi.Types.FileId;
using ModId = NexusMods.Abstractions.NexusWebApi.Types.ModId;

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
    protected readonly IArchiveInstaller ArchiveInstaller;
    protected readonly IFileOriginRegistry FileOriginRegistry;
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
        ArchiveInstaller = serviceProvider.GetRequiredService<IArchiveInstaller>();
        FileOriginRegistry = serviceProvider.GetRequiredService<IFileOriginRegistry>();
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
    protected NexusMods.Abstractions.Loadouts.Mods.ModId AddEmptyMod(ITransaction tx, LoadoutId loadoutId, string name)
    {
        var mod = new Mod.New(tx)
        {
            LoadoutId = loadoutId,
            Name = name,
            Enabled = true,
            Status = ModStatus.Installed,
            Revision = 0,
            Category = ModCategory.Mod,
        };
        return mod.Id;
    }
    
    /// <summary>
    /// Creates a file in the loadout for the given mod. The file will be named with the given path, the hash will be the hash
    /// of the name, and the size will be the length of the name. 
    /// </summary>
    public StoredFileId AddFile(ITransaction tx, LoadoutId loadoutId, NexusMods.Abstractions.Loadouts.Mods.ModId modId, GamePath path)
    {
        var file = new StoredFile.New(tx, out var id)
        {
            File = new File.New(tx, id)
            {
                LoadoutId = loadoutId,
                ModId = modId,
                To = path,
            },
            Hash = path.ToString().XxHash64AsUtf8(),
            Size = Size.FromLong(path.ToString().Length),
        };
        return file.Id;
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
    /// Creates a new loadout and returns the <see cref="LoadoutMarker"/> of it.
    /// </summary>
    /// <returns></returns>
    protected async Task<Loadout.ReadOnly> CreateLoadout(bool indexGameFiles = true)
    {
        return await GameInstallation.GetGame().Synchronizer.CreateLoadout(GameInstallation, Guid.NewGuid().ToString());
    }

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
    /// Downloads a mod and returns the <see cref="TemporaryPath"/> and <see cref="Hash"/> of it.
    /// </summary>
    /// <param name="gameDomain"></param>
    /// <param name="modId"></param>
    /// <param name="fileId"></param>
    /// <returns></returns>
    protected async Task<DownloadId> DownloadMod(GameDomain gameDomain, ModId modId, FileId fileId)
    {
        var links = await NexusNexusApiClient.DownloadLinksAsync(gameDomain, modId, fileId);
        var file = TemporaryFileManager.CreateFile();

        await HttpDownloader.DownloadAsync(
            links.Data.Select(u => new HttpRequestMessage(HttpMethod.Get, u.Uri)).ToArray(),
            file
        );

        var id = await FileOriginRegistry.RegisterDownload(file.Path, fileId.ToString());

        return id;
    }

    /// <summary>
    /// Downloads a mod and caches it in the <see cref="FileStore"/> so future
    /// requests for the same file will be served from the cache. Compares the
    /// hash of the downloaded file with the expected hash and throws an exception
    /// if they don't match.
    /// </summary>
    /// <param name="gameDomain"></param>
    /// <param name="modId"></param>
    /// <param name="fileId"></param>
    /// <param name="hash"></param>
    /// <returns></returns>
    public async Task<DownloadId> DownloadAndCacheMod(GameDomain gameDomain, ModId modId, FileId fileId, Hash hash)
    {
        var db = Connection.Db;
        var metaDatas = NexusModsArchiveMetadata.FindByFileId(db, fileId)
            .Where(ent => ent.ModId == modId)
            .FirstOrDefault(ent => ent.GameId == gameDomain);

        if (metaDatas.IsValid())
            return DownloadId.From(metaDatas.Id);

        var id = await DownloadMod(gameDomain, modId, fileId);

        return id;
    }

    /// <summary>
    /// Installs the mods from the archive into the loadout.
    /// </summary>
    protected async Task<Mod.ReadOnly[]> InstallModsStoredFileIntoLoadout(
        Loadout.ReadOnly loadout,
        DownloadId downloadId,
        CancellationToken cancellationToken = default)
    {
        var modIds = await ArchiveInstaller.AddMods(LoadoutId.From(loadout.Id), downloadId, token: cancellationToken);
        var db = Connection.Db;
        return modIds.Select(id => Mod.Load(db, id)).ToArray();
    }


    /// <summary>
    /// Installs a single mod from the archive into the loadout. This calls
    /// <see cref="InstallModsStoredFileIntoLoadout(LoadoutMarker,NexusMods.Hashing.xxHash64.Hash,string?,System.Threading.CancellationToken)"/> and asserts only one mod
    /// exists in the archive.
    /// </summary>
    protected async Task<Mod.ReadOnly> InstallModStoredFileIntoLoadout(
        Loadout.ReadOnly loadout,
        DownloadId downloadId,
        string? defaultModName = null,
        CancellationToken cancellationToken = default)
    {
        var mods = await InstallModsStoredFileIntoLoadout(
            loadout, downloadId,
            cancellationToken);

        mods.Length.Should().BeGreaterOrEqualTo(1);
        // Sort the mods so we have consistent results
        return mods.OrderBy(m => m.Name).First();
    }

    /// <summary>
    /// Variant of <see cref="InstallModStoredFileIntoLoadout(LoadoutMarker,NexusMods.Hashing.xxHash64.Hash,string?,System.Threading.CancellationToken)"/> that takes a file path instead of a hash.
    /// </summary>
    protected async Task<Mod.ReadOnly> InstallModStoredFileIntoLoadout(
        Loadout.ReadOnly loadout,
        AbsolutePath path,
        string? defaultModName = null,
        CancellationToken cancellationToken = default)
    {
        var downloadId = await FileOriginRegistry.RegisterDownload(path, defaultModName ?? "Unknown", cancellationToken);

        var mods = await InstallModsStoredFileIntoLoadout(
            loadout,
            downloadId,
            cancellationToken
        );

        mods.Should().ContainSingle();
        return mods.First();
    }

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
}
