using System.Text.Json;
using DynamicData.Kernel;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.DTOs;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Abstractions.NexusWebApi.Types.V2.Uid;
using NexusMods.Abstractions.Telemetry;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Networking.NexusWebApi.Extensions;
using NexusMods.Paths;
using NexusMods.Sdk;
using NexusMods.Sdk.FileStore;

namespace NexusMods.Networking.NexusWebApi;

/// <summary>
/// Methods for connecting the Nexus Mods API with the Library.
/// </summary>
[PublicAPI]
public partial class NexusModsLibrary
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnection _connection;
    private readonly INexusApiClient _apiClient;
    private readonly IGraphQlClient _graphQlClient;
    private readonly HttpClient _httpClient;
    private readonly TemporaryFileManager _temporaryFileManager;
    private readonly IFileStore _fileStore;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly IGameDomainToGameIdMappingCache _mappingCache;

    /// <summary>
    /// Constructor.
    /// </summary>
    public NexusModsLibrary(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<NexusModsLibrary>>();
        _httpClient = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient();
        _connection = serviceProvider.GetRequiredService<IConnection>();
        _apiClient = serviceProvider.GetRequiredService<INexusApiClient>();
        _temporaryFileManager = serviceProvider.GetRequiredService<TemporaryFileManager>();
        _fileStore = serviceProvider.GetRequiredService<IFileStore>();
        _jsonSerializerOptions = serviceProvider.GetRequiredService<JsonSerializerOptions>();
        _mappingCache = serviceProvider.GetRequiredService<IGameDomainToGameIdMappingCache>();
        _graphQlClient = serviceProvider.GetRequiredService<IGraphQlClient>();
    }

    public async Task<NexusModsModPageMetadata.ReadOnly> GetOrAddModPage(
        ModId modId,
        GameId gameId,
        CancellationToken cancellationToken = default)
    {
        var uid = new UidForMod
        {
            GameId = gameId,
            ModId = modId,
        };
        var modPageEntities = NexusModsModPageMetadata.FindByUid(_connection.Db, uid);
        if (modPageEntities.TryGetFirst(x => x.Uid.GameId == gameId, out var modPage)) return modPage;

        using var tx = _connection.BeginTransaction();

        var modResult = await _graphQlClient.QueryMod(uid.ModId, uid.GameId, cancellationToken);
        // TODO: handle errors
        var mod = modResult.AssertHasData();

        var modEntityId = mod.Resolve(_connection.Db, tx, setFilesTimestamp: true);
        await ResolveAllFilesInModPage(uid, tx, modEntityId, cancellationToken);

        var txResults = await tx.Commit();
        return NexusModsModPageMetadata.Load(txResults.Db, txResults[modEntityId]);
    }

    private async Task ResolveAllFilesInModPage(UidForMod uid, ITransaction tx, EntityId modPageId, CancellationToken cancellationToken)
    {
        // Note(sewer):
        // Make sure to also fetch all files on the mod page.
        // The update code refreshes file info only on changes of the mod page.
        // If our initial mod page item does not contain info on all the files,
        // then updates are not visible unless an actual change is made to the
        // mod page, this is somewhat undesireable.
        var result = await _graphQlClient.QueryModFiles(uid.ModId, uid.GameId, cancellationToken: cancellationToken);

        // TODO: handle errors
        var modFiles = result.AssertHasData();

        foreach (var modFile in modFiles)
        {
            modFile.Resolve(_connection.Db, tx, modPageId);
        }
    }

    public async Task<NexusModsFileMetadata.ReadOnly> GetOrAddFile(
        FileId fileId,
        NexusModsModPageMetadata.ReadOnly modPage,
        CancellationToken cancellationToken = default)
    {
        var uid = new UidForFile(fileId, modPage.Uid.GameId);
        var fileEntities = NexusModsFileMetadata.FindByUid(_connection.Db, uid);
        if (fileEntities.TryGetFirst(x => x.ModPageId == modPage, out var file))
            return file;

        var modFileResult = await _graphQlClient.QueryModFile(fileId, modPage.Uid.GameId, cancellationToken);
        // TODO: handle errors
        var modFile = modFileResult.AssertHasData();

        using var tx = _connection.BeginTransaction();

        var size = Size.FromLong(long.Parse(modFile.SizeInBytes ?? "0"));
        var newFile = new NexusModsFileMetadata.New(tx)
        {
            Name = modFile.Name,
            Version = modFile.Version,
            ModPageId = modPage,
            Uid = uid,
            UploadedAt = DateTimeOffset.FromUnixTimeSeconds(modFile.Date).UtcDateTime,
            Size = size,
        };

        var txResults = await tx.Commit();
        return txResults.Remap(newFile);
    }

    public async Task<Uri> GetDownloadUri(
        NexusModsFileMetadata.ReadOnly file,
        Optional<(NXMKey, DateTime)> nxmData,
        CancellationToken cancellationToken = default)
    {
        Abstractions.NexusWebApi.DTOs.Response<DownloadLink[]> links;

        if (nxmData.HasValue)
        {
            // NOTE(erri120): the key and expiration date are required for free users to be able to download anything
            var (key, expirationDate) = nxmData.Value;
            links = await _apiClient.DownloadLinksAsync(
                file.ModPage.GameDomain.ToString(),
                file.ModPage.Uid.ModId,
                file.Uid.FileId,
                key: key,
                expireTime: expirationDate,
                token: cancellationToken
            );
        }
        else
        {
            // NOTE(erri120): premium-only API
            links = await _apiClient.DownloadLinksAsync(
                file.ModPage.GameDomain.ToString(),
                file.ModPage.Uid.ModId,
                file.Uid.FileId,
                token: cancellationToken
            );
        }

        // NOTE(erri120): The first download link is the preferred download location as
        // set by the user in their settings. By default, this will be the CDN, which
        // is going to be the fastest location 99% of the time.
        return links.Data.First().Uri;
    }

    /// <summary>
    /// Checks whether the file has already been downloaded.
    /// </summary>
    public async ValueTask<bool> IsAlreadyDownloaded(NXMModUrl url, CancellationToken cancellationToken)
    {
        var gameId = _mappingCache[GameDomain.From(url.Game)];

        var modPage = await GetOrAddModPage(url.ModId, gameId, cancellationToken);
        var file = await GetOrAddFile(url.FileId, modPage, cancellationToken);

        var libraryItems = NexusModsLibraryItem.FindByFileMetadata(file.Db, file);
        return libraryItems.Count != 0;
    }

    /// <summary>
    /// Parse a NXM URL and create a download job from the data
    /// </summary>
    public async Task<IJobTask<NexusModsDownloadJob, AbsolutePath>> CreateDownloadJob(
        AbsolutePath destination,
        NXMModUrl url,
        CancellationToken cancellationToken)
    {
        var nxmData = url.Key is not null && url.ExpireTime is not null ? (url.Key.Value, url.ExpireTime.Value) : Optional.None<(NXMKey, DateTime)>();
        var gameId = _mappingCache[GameDomain.From(url.Game)];
        return await CreateDownloadJob(destination, gameId, url.ModId, url.FileId, nxmData, cancellationToken);
    }

    /// <summary>
    /// Given a mod ID, file ID, and game domain, create a download job
    /// </summary>
    public async Task<IJobTask<NexusModsDownloadJob, AbsolutePath>> CreateDownloadJob(
        AbsolutePath destination,
        GameId gameId,
        ModId modId,
        FileId fileId,
        Optional<(NXMKey, DateTime)> nxmData = default,
        CancellationToken cancellationToken = default)
    {
        var modPage = await GetOrAddModPage(modId, gameId, cancellationToken);
        var file = await GetOrAddFile(fileId, modPage, cancellationToken);

        var uri = await GetDownloadUri(file, nxmData, cancellationToken: cancellationToken);

        var domain = _mappingCache[gameId];
        var httpJob = HttpDownloadJob.Create(_serviceProvider, uri, NexusModsUrlBuilder.GetModUri(domain, modId), destination);
        var nexusJob = NexusModsDownloadJob.Create(_serviceProvider, httpJob, file);

        return nexusJob;
    }

    public async Task<IJobTask<NexusModsDownloadJob, AbsolutePath>> CreateDownloadJob(
        AbsolutePath destination,
        NexusModsFileMetadata.ReadOnly fileMetadata,
        CancellationToken cancellationToken = default)
    {
        var uri = await GetDownloadUri(fileMetadata, Optional<(NXMKey, DateTime)>.None, cancellationToken: cancellationToken);

        var domain = _mappingCache[fileMetadata.Uid.GameId];
        var httpJob = HttpDownloadJob.Create(_serviceProvider, uri, NexusModsUrlBuilder.GetModUri(domain, fileMetadata.ModPage.Uid.ModId), destination);
        var nexusJob = NexusModsDownloadJob.Create(_serviceProvider, httpJob, fileMetadata);

        return nexusJob;
    }

    /// <summary>
    /// Create a job that downloads a collection
    /// </summary>
    /// <param name="destination">Download location</param>
    /// <param name="slug">The collection slug</param>
    /// <param name="revision">The revision of the collection download</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public IJobTask<NexusModsCollectionDownloadJob, AbsolutePath> CreateCollectionDownloadJob(
        AbsolutePath destination,
        CollectionSlug slug,
        RevisionNumber revision,
        CancellationToken cancellationToken)
    {
        return NexusModsCollectionDownloadJob.Create(_serviceProvider, slug, revision, destination);
    }
}
