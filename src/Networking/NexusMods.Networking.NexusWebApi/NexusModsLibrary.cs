using DynamicData.Kernel;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.DTOs;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Extensions.BCL;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Paths;

namespace NexusMods.Networking.NexusWebApi;

[PublicAPI]
public class NexusModsLibrary
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnection _connection;
    private readonly INexusApiClient _apiClient;
    private readonly NexusGraphQLClient _gqlClient;
    
    /// <summary>
    /// Constructor.
    /// </summary>
    public NexusModsLibrary(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _connection = serviceProvider.GetRequiredService<IConnection>();
        _apiClient = serviceProvider.GetRequiredService<INexusApiClient>();
        _gqlClient = serviceProvider.GetRequiredService<NexusGraphQLClient>();
    }

    public async Task<NexusModsModPageMetadata.ReadOnly> GetOrAddModPage(
        ModId modId,
        GameDomain gameDomain,
        CancellationToken cancellationToken = default)
    {
        var modPageEntities = NexusModsModPageMetadata.FindByModId(_connection.Db, modId);
        if (modPageEntities.TryGetFirst(x => x.GameDomain == gameDomain, out var modPage)) return modPage;

        using var tx = _connection.BeginTransaction();

        var modInfo = await _apiClient.ModInfoAsync(gameDomain.ToString(), modId, cancellationToken);

        var newModPage = new NexusModsModPageMetadata.New(tx)
        {
            Name = modInfo.Data.Name,
            ModId = modId,
            GameDomain = gameDomain,
        };

        if (Uri.TryCreate(modInfo.Data.PictureUrl, UriKind.Absolute, out var fullSizedPictureUri))
        {
            newModPage.FullSizedPictureUri = fullSizedPictureUri;

            var thumbnailUrl = modInfo.Data.PictureUrl.Replace("/images/", "/images/thumbnails/", StringComparison.OrdinalIgnoreCase);
            if (Uri.TryCreate(thumbnailUrl, UriKind.Absolute, out var thumbnailUri))
            {
                newModPage.ThumbnailUri = thumbnailUri;
            }
        }

        var txResults = await tx.Commit();
        return txResults.Remap(newModPage);
    }

    /// <summary>
    /// Get or add a collection metadata
    /// </summary>
    public async Task<NexusModsCollectionMetadata.ReadOnly> GetOrAddCollectionMetadata(CollectionSlug slug, CancellationToken token)
    {
        var collections = NexusModsCollectionMetadata.FindBySlug(_connection.Db, slug);
        if (collections.TryGetFirst(x => x.Slug == slug, out var collection)) 
            return collection;

        var info = await _gqlClient.CollectionInfo.ExecuteAsync(slug.Value, true, token);
        
        using var tx = _connection.BeginTransaction();
        var newCollection = new NexusModsCollectionMetadata.New(tx)
        {
            Slug = slug,
            Name = info.Data!.Collection.Name,
        };

        foreach (var revision in info.Data!.Collection.Revisions)
        {
            _ = new NexusModsCollectionRevision.New(tx)
            {
                CollectionId = newCollection,
                RevisionId = RevisionId.From((ulong)revision.Id),
                RevisionNumber = RevisionNumber.From((ulong)revision.RevisionNumber),
            };
        }
        
        var txResults = await tx.Commit();

        return txResults.Remap(newCollection);
    }
    
    /// <summary>
    /// Get or add a collection metadata
    /// </summary>
    public async Task<NexusModsCollectionRevision.ReadOnly> GetOrAddCollectionRevision(CollectionSlug slug, RevisionNumber revisionNumber, CancellationToken token)
    {
        var collections = await GetOrAddCollectionMetadata(slug, token);
        if (collections.Revisions.TryGetFirst(r => r.RevisionNumber == revisionNumber, out var revision)) 
            return revision;
        
        var revisionInfo = await _gqlClient.CollectionRevisionInfo.ExecuteAsync(slug.Value, (int)revisionNumber.Value, true, token);
        
        using var tx = _connection.BeginTransaction();
        var newRevision = new NexusModsCollectionRevision.New(tx)
        {
            CollectionId = collections,
            RevisionNumber = revisionNumber,
            RevisionId = RevisionId.From((ulong)revisionInfo.Data!.CollectionRevision.Id),
        };
        
        var txResults = await tx.Commit();
        
        return txResults.Remap(newRevision);
    }

    public async Task<NexusModsFileMetadata.ReadOnly> GetOrAddFile(
        FileId fileId,
        NexusModsModPageMetadata.ReadOnly modPage,
        GameDomain gameDomain,
        CancellationToken cancellationToken = default)
    {
        var fileEntities = NexusModsFileMetadata.FindByFileId(_connection.Db, fileId);
        if (fileEntities.TryGetFirst(x => x.ModPageId == modPage, out var file)) return file;

        using var tx = _connection.BeginTransaction();

        var filesResponse = await _apiClient.ModFilesAsync(gameDomain.ToString(), modPage.ModId, cancellationToken);
        var files = filesResponse.Data.Files;

        if (!files.TryGetFirst(x => x.FileId == fileId, out var fileInfo))
            throw new NotImplementedException();

        var newFile = new NexusModsFileMetadata.New(tx)
        {
            Name = fileInfo.Name,
            Version = fileInfo.Version,
            FileId = fileId,
            ModPageId = modPage,
        };

        var txResults = await tx.Commit();
        return txResults.Remap(newFile);
    }

    public async Task<Uri> GetDownloadUri(
        NexusModsFileMetadata.ReadOnly file,
        Optional<(NXMKey, DateTime)> nxmData,
        CancellationToken cancellationToken = default)
    {
        Response<DownloadLink[]> links;

        if (nxmData.HasValue)
        {
            // NOTE(erri120): the key and expiration date are required for free users to be able to download anything
            var (key, expirationDate) = nxmData.Value;
            links = await _apiClient.DownloadLinksAsync(
                file.ModPage.GameDomain.ToString(),
                file.ModPage.ModId,
                file.FileId,
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
                file.ModPage.ModId,
                file.FileId,
                token: cancellationToken
            );
        }

        // NOTE(erri120): The first download link is the preferred download location as
        // set by the user in their settings. By default, this will be the CDN, which
        // is going to be the fastest location 99% of the time.
        return links.Data.First().Uri;
    }

    public async Task<IJobTask<NexusModsDownloadJob, AbsolutePath>> CreateDownloadJob(
        AbsolutePath destination,
        NXMModUrl url,
        CancellationToken cancellationToken)
    {
        var modPage = await GetOrAddModPage(url.ModId, GameDomain.From(url.Game), cancellationToken);
        var file = await GetOrAddFile(url.FileId, modPage, GameDomain.From(url.Game), cancellationToken);

        var nxmData = url.Key is not null && url.ExpireTime is not null ? (url.Key.Value, url.ExpireTime.Value) : Optional.None<(NXMKey, DateTime)>();
        var uri = await GetDownloadUri(file, nxmData, cancellationToken: cancellationToken);
        
        var httpJob = HttpDownloadJob.Create(_serviceProvider, uri, modPage.GetUri(), destination);
        var nexusJob = NexusModsDownloadJob.Create(_serviceProvider, httpJob, file);

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
