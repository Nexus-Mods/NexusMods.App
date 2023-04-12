using System.IO.Compression;
using System.Text;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.Hashing.xxHash64;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Networking.NexusWebApi;
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
    protected readonly FileContentsCache FileContentsCache;
    protected readonly IDataStore DataStore;

    protected readonly Client NexusClient;
    protected readonly IHttpDownloader HttpDownloader;

    protected AGameTest(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        Game = serviceProvider.FindImplementationInContainer<TGame, IGame>();
        GameInstallation = Game.Installations.First();

        FileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        ArchiveManager = serviceProvider.GetRequiredService<ArchiveManager>();
        TemporaryFileManager = serviceProvider.GetRequiredService<TemporaryFileManager>();
        LoadoutManager = serviceProvider.GetRequiredService<LoadoutManager>();
        FileContentsCache = serviceProvider.GetRequiredService<FileContentsCache>();
        DataStore = serviceProvider.GetRequiredService<IDataStore>();

        NexusClient = serviceProvider.GetRequiredService<Client>();
        HttpDownloader = serviceProvider.GetRequiredService<IHttpDownloader>();
    }

    /// <summary>
    /// Creates a new loadout and returns the <see cref="LoadoutMarker"/> of it.
    /// </summary>
    /// <returns></returns>
    protected async Task<LoadoutMarker> CreateLoadout()
    {
        var loadout = await LoadoutManager.ManageGameAsync(GameInstallation, Guid.NewGuid().ToString("N"));
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
        var links = await NexusClient.DownloadLinks(gameDomain, modId, fileId);
        var file = TemporaryFileManager.CreateFile();

        var downloadHash = await HttpDownloader.DownloadAsync(
            links.Data.Select(u => new HttpRequestMessage(HttpMethod.Get, u.Uri)).ToArray(),
            file
        );

        return (file, downloadHash);
    }
    
    
    public async Task<AbsolutePath> DownloadAndCacheMod(GameDomain gameDomain, ModId modId, FileId fileId, Hash hash)
    {
        // TODO: Change HaveArchive to output file path, otherwise end users might see tests, copy this code and do it inefficiently. https://github.com/Nexus-Mods/NexusMods.App/issues/206
        if (ArchiveManager.HaveArchive(hash))
            return ArchiveManager.PathFor(hash);

        var (file, downloadHash) = await DownloadMod(gameDomain, modId, fileId);
        downloadHash.Should().Be(hash);
        return file.Path;
    }

    /// <summary>
    /// Installs a mod into the loadout and returns the <see cref="Mod"/> of it.
    /// </summary>
    /// <param name="loadout"></param>
    /// <param name="path"></param>
    /// <param name="modName"></param>
    /// <returns></returns>
    protected async Task<Mod> InstallModIntoLoadout(LoadoutMarker loadout, AbsolutePath path, string? modName = null)
    {
        var modId = await loadout.InstallModAsync(path, modName ?? Guid.NewGuid().ToString("N"));
        return loadout.Value.Mods[modId];
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
        using (var zipArchive = new ZipArchive(stream, ZipArchiveMode.Create))
        {
            foreach (var kv in filesToZip)
            {
                var (path, contents) = kv;

                var entry = zipArchive.CreateEntry(path.Path, CompressionLevel.Fastest);
                await using var entryStream = entry.Open();
                await using var ms = new MemoryStream(contents);
                await ms.CopyToAsync(entryStream);
            }
        }

        return file;
    }

    protected async Task<TemporaryPath> CreateTestFile(byte[] contents, Extension? extension)
    {
        var file = TemporaryFileManager.CreateFile(extension);
        await FileSystem.WriteAllBytesAsync(file.Path, contents);
        return file;
    }

    protected Task<TemporaryPath> CreateTestFile(string contents, Extension? extension, Encoding? encoding = null)
        => CreateTestFile((encoding ?? Encoding.UTF8).GetBytes(contents), extension);
}
