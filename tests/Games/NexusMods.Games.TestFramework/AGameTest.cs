using System.IO.Compression;
using System.Text;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.Hashing.xxHash64;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Networking.NexusWebApi.NMA.Extensions;
using NexusMods.Networking.NexusWebApi.Types;
using NexusMods.Paths;
using ModId = NexusMods.Networking.NexusWebApi.Types.ModId;

namespace NexusMods.Games.TestFramework;

[PublicAPI]
public abstract class AGameTest<TGame> where TGame : AGame
{
    protected readonly IServiceProvider ServiceProvider;
    protected readonly TGame Game;
    protected readonly GameInstallation GameInstallation;

    protected readonly IFileSystem FileSystem;
    protected readonly TemporaryFileManager TemporaryFileManager;
    protected readonly ArchiveManager ArchiveManager;
    protected readonly LoadoutManager LoadoutManager;
    protected readonly LoadoutRegistry LoadoutRegistry;
    protected readonly FileContentsCache FileContentsCache;
    protected readonly IDataStore DataStore;

    protected readonly Client NexusClient;
    protected readonly IHttpDownloader HttpDownloader;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="serviceProvider"></param>
    protected AGameTest(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;

        Game = serviceProvider.FindImplementationInContainer<TGame, IGame>();

        var gameInstallations = Game.Installations.ToArray();
        gameInstallations.Should().NotBeEmpty("because the game has to be installed");

        GameInstallation = gameInstallations.First();
        GameInstallation.Game.Should().BeOfType<TGame>("because the game installation should be for the game we're testing");

        FileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        ArchiveManager = serviceProvider.GetRequiredService<ArchiveManager>();
        TemporaryFileManager = serviceProvider.GetRequiredService<TemporaryFileManager>();
        LoadoutManager = serviceProvider.GetRequiredService<LoadoutManager>();
        LoadoutRegistry = serviceProvider.GetRequiredService<LoadoutRegistry>();
        FileContentsCache = serviceProvider.GetRequiredService<FileContentsCache>();
        DataStore = serviceProvider.GetRequiredService<IDataStore>();

        NexusClient = serviceProvider.GetRequiredService<Client>();
        HttpDownloader = serviceProvider.GetRequiredService<IHttpDownloader>();
    }

    /// <summary>
    /// Creates a new loadout and returns the <see cref="LoadoutMarker"/> of it.
    /// </summary>
    /// <returns></returns>
    protected async Task<LoadoutMarker> CreateLoadout(bool indexGameFiles = true)
    {
        var loadout = await LoadoutManager.ManageGameAsync(GameInstallation, Guid.NewGuid().ToString("N"), indexGameFiles: indexGameFiles);
        return loadout;
    }

    /// <summary>
    /// Downloads a mod and returns the <see cref="TemporaryPath"/> and <see cref="Hash"/> of it.
    /// </summary>
    /// <param name="gameDomain"></param>
    /// <param name="modId"></param>
    /// <param name="fileId"></param>
    /// <returns></returns>
    protected async Task<(TemporaryPath file, Hash downloadHash)> DownloadMod(GameDomain gameDomain, ModId modId, FileId fileId)
    {
        var links = await NexusClient.DownloadLinksAsync(gameDomain, modId, fileId);
        var file = TemporaryFileManager.CreateFile();

        var downloadHash = await HttpDownloader.DownloadAsync(
            links.Data.Select(u => new HttpRequestMessage(HttpMethod.Get, u.Uri)).ToArray(),
            file
        );

        return (file, downloadHash);
    }
    
    /// <summary>
    /// Downloads a mod and caches it in the <see cref="ArchiveManager"/> so future
    /// requests for the same file will be served from the cache. Compares the
    /// hash of the downloaded file with the expected hash and throws an exception
    /// if they don't match.
    /// </summary>
    /// <param name="gameDomain"></param>
    /// <param name="modId"></param>
    /// <param name="fileId"></param>
    /// <param name="hash"></param>
    /// <returns></returns>
    public async Task<AbsolutePath> DownloadAndCacheMod(GameDomain gameDomain, ModId modId, FileId fileId, Hash hash)
    {
        if (ArchiveManager.TryGetPathFor(hash, out var path))
            return path;

        var (file, downloadHash) = await DownloadMod(gameDomain, modId, fileId);
        downloadHash.Should().Be(hash);
        
        await ArchiveManager.ArchiveFileAsync(file.Path);
        return file.Path;
    }

    /// <summary>
    /// Installs the mods from the archive into the loadout.
    /// </summary>
    /// <param name="loadout"></param>
    /// <param name="archivePath"></param>
    /// <param name="defaultModName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected async Task<Mod[]> InstallModsFromArchiveIntoLoadout(
        LoadoutMarker loadout,
        AbsolutePath archivePath,
        string? defaultModName = null,
        CancellationToken cancellationToken = default)
    {
        var modIds = await loadout.InstallModsFromArchiveAsync(archivePath, default, cancellationToken);
        return modIds.Select(id => loadout.Value.Mods[id]).ToArray();
    }

    /// <summary>
    /// Installs a single mod from the archive into the loadout. This calls
    /// <see cref="InstallModsFromArchiveIntoLoadout"/> and asserts only one mod
    /// exists in the archive.
    /// </summary>
    /// <param name="loadout"></param>
    /// <param name="archivePath"></param>
    /// <param name="defaultModName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected async Task<Mod> InstallModFromArchiveIntoLoadout(
        LoadoutMarker loadout,
        AbsolutePath archivePath,
        string? defaultModName = null,
        CancellationToken cancellationToken = default)
    {
        var mods = await InstallModsFromArchiveIntoLoadout(
            loadout,
            archivePath,
            defaultModName,
            cancellationToken);

        mods.Should().ContainSingle();
        return mods.First();
    }

    /// <summary>
    /// Analyzes a file as an archive using the <see cref="NexusMods.DataModel.FileContentsCache"/>.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">The provided file is not an archive.</exception>
    protected async Task<AnalyzedArchive> AnalyzeArchive(AbsolutePath path)
    {
        var analyzedFile = await FileContentsCache.AnalyzeFileAsync(path);
        if (analyzedFile is AnalyzedArchive analyzedArchive)
            return analyzedArchive;
        throw new ArgumentException($"File at {path} is not an archive!", nameof(path));
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

        await using var stream = FileSystem.OpenFile(file.Path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
        using var zipArchive = new ZipArchive(stream, ZipArchiveMode.Create);
        
        foreach (var kv in filesToZip)
        {
            var (path, contents) = kv;

            var entry = zipArchive.CreateEntry(path.Path, CompressionLevel.Fastest);
            await using var entryStream = entry.Open();
            await using var ms = new MemoryStream(contents);
            await ms.CopyToAsync(entryStream);
        }

        return file;
    }

    protected async Task<TemporaryPath> CreateTestFile(string fileName, byte[] contents)
    {
        var folder = TemporaryFileManager.CreateFolder();
        var path = folder.Path.CombineUnchecked(fileName);
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
