using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Games;
using NexusMods.Games.TestFramework.Stubs;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Paths;

namespace NexusMods.Games.TestFramework;

/// <summary>
/// Utility for downloading mods from various remote sources for test purposes.
/// </summary>
[PublicAPI]
public class TestModDownloader
{
    private readonly ILogger<TestModDownloader> _logger;
    private readonly IHttpDownloader _httpDownloader;
    private readonly Client _nexusClient;

    public TestModDownloader(IHttpDownloader httpDownloader, ILogger<TestModDownloader> logger, Client nexusClient)
    {
        _httpDownloader = httpDownloader;
        _logger = logger;
        _nexusClient = nexusClient;
    }

    /// <summary>
    /// Downloads this mod asynchronously to a temporary path.
    /// </summary>
    /// <param name="modFolder">Folder containing the manifest.</param>
    /// <param name="targetFs">FileSystem to write to.</param>
    public async Task<DownloadedItem> DownloadFromManifestAsync(AbsolutePath modFolder, IFileSystem targetFs, CancellationToken token = default)
    {
        var manifestFile = modFolder.CombineUnchecked("manifest.json");
        var meta = await RemoteModMetadataBase.DeserializeFromAsync(manifestFile, FileSystem.Shared);
        return await DownloadAsync(meta, targetFs, token);
    }

    /// <summary>
    /// Downloads this mod asynchronously to a temporary path.
    /// </summary>
    /// <param name="meta">The metadata for which to download the mod for.</param>
    /// <param name="targetFs">FileSystem to write to.</param>
    /// <param name="token">Allows you to cancel the operation.</param>
    /// <returns>Downloaded item inside the provided FileSystem.</returns>
    public async Task<DownloadedItem> DownloadAsync(RemoteModMetadataBase meta, IFileSystem targetFs, CancellationToken token = default)
    {
        var manager = new TemporaryFileManager(targetFs);
        var tempFolder = manager.CreateFolder();
        await DownloadAsync(meta, targetFs, tempFolder.Path, token);
        return new DownloadedItem(manager, tempFolder, targetFs);
    }

    /// <summary>
    /// Downloads this mod asynchronously to a given path.
    /// </summary>
    /// <param name="meta">The metadata for which to download the mod for.</param>
    /// <param name="targetFs">FileSystem to write to.</param>
    /// <param name="folderPath">Folder to download files to.</param>
    /// <param name="token">Allows you to cancel the operation.</param>
    public async Task DownloadAsync(RemoteModMetadataBase meta, IFileSystem targetFs, AbsolutePath folderPath, CancellationToken token = default)
    {
        switch (meta.Source)
        {
            case RemoteModSource.NexusMods:
                await DownloadNexusModAsync((NexusModMetadata)meta, folderPath, targetFs, token);
                break;
            case RemoteModSource.RealFileSystem:
                await DownloadFileSystemModAsync((FileSystemModMetadata)meta, folderPath, targetFs, token);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(meta.Source));
        }
    }

    /// <summary>
    /// Downloads a mod from NexusMods to a given directory.
    /// </summary>
    /// <param name="nexusMod">Mod stored remotely on the Nexus.</param>
    /// <param name="folderPath">The directory to download to.</param>
    /// <param name="fileSystem">The FileSystem to write resulting data to.</param>
    /// <param name="token">Allows you to cancel the operation.</param>
    private async Task DownloadNexusModAsync(NexusModMetadata nexusMod,
        AbsolutePath folderPath, [PublicAPI] IFileSystem fileSystem, CancellationToken token = default)
    {
        // TODO: Nexus Web API needs To Support IFileSystem at some point.
        _logger.LogInformation("Downloading {ModId} {FileId} {Hash}", nexusMod.ModId, nexusMod.FileId, nexusMod.Hash);
        var uris = await _nexusClient.DownloadLinks(GameDomain.Cyberpunk2077, nexusMod.ModId, nexusMod.FileId, token);
        var downloadUris = uris.Data.Select(u => new HttpRequestMessage(HttpMethod.Get, u.Uri)).ToArray();
        var downloadHash = await _httpDownloader.DownloadAsync(downloadUris, folderPath, token: token);
        if (downloadHash != nexusMod.Hash)
            throw new Exception($"Hash of downloaded file from Nexus {nexusMod.ModId}, {nexusMod.FileId} does not match expected hash.");
    }

    /// <summary>
    /// Downloads a mod from the filesystem.
    /// </summary>
    /// <param name="fsModMetadata">Metadata of the given path.</param>
    /// <param name="folderPath">Path to the output folder.</param>
    /// <param name="fileSystem">The FileSystem to write resulting data to.</param>
    /// <param name="token">Allows you to cancel the operation.</param>
    private async Task DownloadFileSystemModAsync(
        FileSystemModMetadata fsModMetadata, AbsolutePath folderPath,
        IFileSystem fileSystem, CancellationToken token = default)
    {
        // TODO: CombineChecked this once FileSystem is separated from AbsolutePath/RelativePath.
        var sourcePath = fsModMetadata.JsonPath.Parent.CombineUnchecked(fsModMetadata.FilePath);
        _logger.LogInformation("Downloading {Type}, From: {Output}", RemoteModSource.RealFileSystem, sourcePath);
        var data = await FileSystem.Shared.ReadAllBytesAsync(sourcePath, token);
        await fileSystem.WriteAllBytesAsync(folderPath, data, token);
    }
}
