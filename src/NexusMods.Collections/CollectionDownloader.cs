using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Collections;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.CrossPlatform.Process;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Paths;

namespace NexusMods.Collections;

/// <summary>
/// Methods for collection downloads.
/// </summary>
public class CollectionDownloader
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly ILoginManager _loginManager;
    private readonly TemporaryFileManager _temporaryFileManager;
    private readonly NexusModsLibrary _nexusModsLibrary;
    private readonly ILibraryService _libraryService;
    private readonly IOSInterop _osInterop;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Constructor.
    /// </summary>
    public CollectionDownloader(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<CollectionDownloader>>();
        _loginManager = serviceProvider.GetRequiredService<ILoginManager>();
        _temporaryFileManager = serviceProvider.GetRequiredService<TemporaryFileManager>();
        _nexusModsLibrary = serviceProvider.GetRequiredService<NexusModsLibrary>();
        _libraryService = serviceProvider.GetRequiredService<ILibraryService>();
        _osInterop = serviceProvider.GetRequiredService<IOSInterop>();
        _httpClient = serviceProvider.GetRequiredService<HttpClient>();
    }

    private async ValueTask<bool> CanDirectDownload(CollectionDownloadExternal.ReadOnly download, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Testing if `{Uri}` can be downloaded directly", download.Uri);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, download.Uri);
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken: cancellationToken);
            if (!response.IsSuccessStatusCode) return false;

            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (contentType is null || !contentType.StartsWith("application/"))
            {
                _logger.LogInformation("Download at `{Uri}` can't be downloaded automatically because Content-Type `{ContentType}` doesn't indicate a binary download", download.Uri, contentType);
                return false;
            }

            if (!response.Content.Headers.ContentLength.HasValue)
            {
                _logger.LogInformation("Download at `{Uri}` can't be downloaded automatically because the response doesn't have a Content-Length", download.Uri);
                return false;
            }

            var size = Size.FromLong(response.Content.Headers.ContentLength.Value);
            if (size != download.Size)
            {
                _logger.LogWarning("Download at `{Uri}` can't be downloaded automatically because the Content-Length `{ContentLength}` doesn't match the expected size `{ExpectedSize}`", download.Uri, size, download.Size);
                return false;
            }

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while checking if `{Uri}` can be downloaded directly", download.Uri);
            return false;
        }
    }
    private async ValueTask Download(CollectionDownloadExternal.ReadOnly download, bool onlyDirectDownloads, CancellationToken cancellationToken)
    {
        if (await CanDirectDownload(download, cancellationToken))
        {
            _logger.LogInformation("Downloading external file at `{Uri}` directly", download.Uri);
            var job = ExternalDownloadJob.Create(_serviceProvider, download.Uri, download.Md5, download.AsCollectionDownload().Name);
            await _libraryService.AddDownload(job);
        }
        else
        {
            if (onlyDirectDownloads) return;

            _logger.LogInformation("Unable to direct download `{Uri}`, using browse as a fallback", download.Uri);
            await _osInterop.OpenUrl(download.Uri, logOutput: false, fireAndForget: true, cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Downloads an external file or opens the browser if the file can't be downloaded automatically.
    /// </summary>
    public ValueTask Download(CollectionDownloadExternal.ReadOnly download, CancellationToken cancellationToken)
    {
        return Download(download, onlyDirectDownloads: false, cancellationToken);
    }

    /// <summary>
    /// Checks whether the item was already downloaded.
    /// </summary>
    public static bool IsDownloaded(CollectionDownloadExternal.ReadOnly download, IDb? db = null)
    {
        db ??= download.Db;
        var directDownloadDatoms = db.Datoms(DirectDownloadLibraryFile.Md5, download.Md5);
        if (directDownloadDatoms.Count > 0) return true;

        var locallyAddedDatoms = db.Datoms(LocalFile.Md5, download.Md5);
        if (locallyAddedDatoms.Count > 0) return true;

        return false;
    }

    /// <summary>
    /// Checks whether the item was already downloaded.
    /// </summary>
    public static bool IsDownloaded(CollectionDownloadNexusMods.ReadOnly download, IDb? db = null)
    {
        db ??= download.Db;
        var datoms = db.Datoms(NexusModsLibraryItem.FileMetadata, download.FileMetadata);
        return datoms.Count > 0;
    }

    /// <summary>
    /// Downloads a file from nexus mods for premium users or opens the download page in the browser.
    /// </summary>
    public async ValueTask Download(CollectionDownloadNexusMods.ReadOnly download, CancellationToken cancellationToken)
    {
        if (_loginManager.IsPremium)
        {
            await using var tempPath = _temporaryFileManager.CreateFile();
            var job = await _nexusModsLibrary.CreateDownloadJob(tempPath, download.FileMetadata, cancellationToken: cancellationToken);
            await _libraryService.AddDownload(job);
        }
        else
        {
            await _osInterop.OpenUrl(download.FileMetadata.GetUri(), logOutput: false, fireAndForget: true, cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Downloads everything in the revision.
    /// </summary>
    public async ValueTask DownloadAll(
        CollectionRevisionMetadata.ReadOnly revisionMetadata,
        bool onlyRequired,
        IDb? db = null,
        int maxDegreeOfParallelism = -1,
        CancellationToken cancellationToken = default)
    {
        var downloads = revisionMetadata.Downloads.ToArray();

        await Parallel.ForAsync(fromInclusive: 0, toExclusive: downloads.Length, parallelOptions: new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = maxDegreeOfParallelism == -1 ? Environment.ProcessorCount : maxDegreeOfParallelism,
        }, body: async (index, token) =>
        {
            var download = downloads[index];

            if (download.IsOptional && onlyRequired) return;

            if (download.TryGetAsCollectionDownloadNexusMods(out var nexusModsDownload))
            {
                if (IsDownloaded(nexusModsDownload, db)) return;
                await Download(nexusModsDownload, token);
            } else if (download.TryGetAsCollectionDownloadExternal(out var externalDownload))
            {
                if (IsDownloaded(externalDownload, db)) return;
                await Download(externalDownload, onlyDirectDownloads: true, token);
            }
        });
    }
}
