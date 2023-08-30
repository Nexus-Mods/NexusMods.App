using System.IO.Compression;
using System.Text;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Common;
using NexusMods.DataModel;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.ArchiveMetaData;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.Games.TestFramework.Downloader;
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
    protected readonly IArchiveManager ArchiveManager;
    protected readonly IArchiveInstaller ArchiveInstaller;
    protected readonly IDownloadRegistry DownloadRegistry;
    protected readonly LoadoutManager LoadoutManager;
    protected readonly LoadoutRegistry LoadoutRegistry;
    protected readonly LoadoutSynchronizer LoadoutSynchronizer;
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
        ArchiveManager = serviceProvider.GetRequiredService<IArchiveManager>();
        ArchiveInstaller = serviceProvider.GetRequiredService<IArchiveInstaller>();
        DownloadRegistry = serviceProvider.GetRequiredService<IDownloadRegistry>();
        TemporaryFileManager = serviceProvider.GetRequiredService<TemporaryFileManager>();
        LoadoutManager = serviceProvider.GetRequiredService<LoadoutManager>();
        LoadoutRegistry = serviceProvider.GetRequiredService<LoadoutRegistry>();
        LoadoutSynchronizer = serviceProvider.GetRequiredService<LoadoutSynchronizer>();
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

        await DownloadRegistry.RegisterDownload(file.Path, new NexusModsArchiveMetadata
        {
            GameDomain = gameDomain,
            ModId = modId,
            FileId = fileId,
            Quality = Quality.High
        });

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
    public async Task<DownloadId> DownloadAndCacheMod(GameDomain gameDomain, ModId modId, FileId fileId, Hash hash)
    {
        var metaDatas = DownloadRegistry.GetAll()
            .FirstOrDefault(g => g.MetaData is NexusModsArchiveMetadata na
                        && na.GameDomain == gameDomain && na.ModId == modId && na.FileId == fileId);

        if (metaDatas != null)
            return metaDatas.DownloadId;

        var (file, downloadHash) = await DownloadMod(gameDomain, modId, fileId);
        downloadHash.Should().Be(hash);

        var id = await DownloadRegistry.RegisterDownload(file, new NexusModsArchiveMetadata
            { GameDomain = gameDomain, ModId = modId, FileId = fileId, Quality = Quality.High });

        return id;
    }

    /// <summary>
    /// Installs the mods from the archive into the loadout.
    /// </summary>
    /// <param name="loadout"></param>
    /// <param name="hash"></param>
    /// <param name="defaultModName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected async Task<Mod[]> InstallModsFromArchiveIntoLoadout(
        LoadoutMarker loadout,
        DownloadId downloadId,
        string? defaultModName = null,
        CancellationToken cancellationToken = default)
    {
        var modIds = await ArchiveInstaller.AddMods(loadout.Value.LoadoutId, downloadId, defaultModName, cancellationToken);
        return modIds.Select(id => loadout.Value.Mods[id]).ToArray();
    }


    /// <summary>
    /// Installs a single mod from the archive into the loadout. This calls
    /// <see cref="InstallModsFromArchiveIntoLoadout(NexusMods.DataModel.Loadouts.Markers.LoadoutMarker,NexusMods.Hashing.xxHash64.Hash,string?,System.Threading.CancellationToken)"/> and asserts only one mod
    /// exists in the archive.
    /// </summary>
    /// <param name="loadout"></param>
    /// <param name="hash"></param>
    /// <param name="defaultModName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected async Task<Mod> InstallModFromArchiveIntoLoadout(
        LoadoutMarker loadout,
        DownloadId downloadId,
        string? defaultModName = null,
        CancellationToken cancellationToken = default)
    {
        var mods = await InstallModsFromArchiveIntoLoadout(
            loadout, downloadId,
            defaultModName,
            cancellationToken);

        mods.Length.Should().BeGreaterOrEqualTo(1);
        return mods.First();
    }

    /// <summary>
    /// Variant of <see cref="InstallModFromArchiveIntoLoadout(NexusMods.DataModel.Loadouts.Markers.LoadoutMarker,NexusMods.Hashing.xxHash64.Hash,string?,System.Threading.CancellationToken)"/> that takes a file path instead of a hash.
    /// </summary>
    /// <param name="loadout"></param>
    /// <param name="path"></param>
    /// <param name="defaultModName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected async Task<Mod> InstallModFromArchiveIntoLoadout(
        LoadoutMarker loadout,
        AbsolutePath path,
        string? defaultModName = null,
        CancellationToken cancellationToken = default)
    {
        var downloadId = await DownloadRegistry.RegisterDownload(path, new FilePathMetadata
            { OriginalName = path.FileName, Quality = Quality.Low }, cancellationToken);

        var mods = await InstallModsFromArchiveIntoLoadout(
            loadout,
            downloadId,
            defaultModName,
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

    /// <summary>
    /// Helper method to create create a apply plan and apply it.
    /// </summary>
    /// <param name="loadout"></param>
    protected async Task Apply(Loadout loadout)
    {
        var plan = await LoadoutSynchronizer.MakeApplySteps(loadout);
        await LoadoutSynchronizer.Apply(plan);
    }
}
