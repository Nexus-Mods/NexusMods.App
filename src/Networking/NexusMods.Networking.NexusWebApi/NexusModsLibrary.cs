using DynamicData.Kernel;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.DTOs;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Extensions.BCL;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Paths;
using User = NexusMods.Abstractions.NexusModsLibrary.Models.User;

namespace NexusMods.Networking.NexusWebApi;

[PublicAPI]
public class NexusModsLibrary
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnection _connection;
    private readonly INexusApiClient _apiClient;
    private readonly NexusGraphQLClient _gqlClient;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Constructor.
    /// </summary>
    public NexusModsLibrary(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _httpClient = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient();
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
    public async Task<Collection.ReadOnly> GetOrAddCollectionMetadata(CollectionSlug slug, bool referesh = false, CancellationToken token = default)
    {
        if (!referesh)
        {
            var collections = Collection.FindBySlug(_connection.Db, slug);
            if (collections.TryGetFirst(x => x.Slug == slug, out var collection))
                return collection;
        }

        var info = await _gqlClient.CollectionInfo.ExecuteAsync(slug.Value, true, token);

        var collectionInfo = info.Data!.Collection;
        var collectionTileImage = _httpClient.GetByteArrayAsync(new Uri(collectionInfo.TileImage!.ThumbnailUrl), token);
        var avatarImage = _httpClient.GetByteArrayAsync(new Uri(collectionInfo.User.Avatar), token);
        
        using var tx = _connection.BeginTransaction();
        var db = _connection.Db;
        var collectionResolver = GraphQLResolver.Create(db, tx, Collection.Slug, slug);
        collectionResolver.Add(Collection.Name, collectionInfo.Name);
        collectionResolver.Add(Collection.Summary, collectionInfo.Summary);
        collectionResolver.Add(Collection.Endorsements, (ulong)collectionInfo.Endorsements);
        collectionResolver.Add(Collection.TileImage, await collectionTileImage);

        // Remap the user info
        var userResolver = GraphQLResolver.Create(db, tx, User.NexusId, (ulong)collectionInfo.User.MemberId);
        userResolver.Add(User.Name, collectionInfo.User.Name);
        userResolver.Add(User.Avatar, new Uri(collectionInfo.User.Avatar));
        userResolver.Add(User.AvatarImage, await avatarImage);
        
        collectionResolver.Add(Collection.User, userResolver.Id);
        
        // Remap the revisions
        foreach (var revision in collectionInfo.Revisions)
        {
            var revisionResolver = GraphQLResolver.Create(db, tx, CollectionRevision.RevisionId, RevisionId.From((ulong)revision.Id));
            revisionResolver.Add(CollectionRevision.RevisionId, RevisionId.From((ulong)revision.Id));
            revisionResolver.Add(CollectionRevision.RevisionNumber, RevisionNumber.From((ulong)revision.RevisionNumber));
            revisionResolver.Add(CollectionRevision.CollectionId, collectionResolver.Id);
            revisionResolver.Add(CollectionRevision.Downloads, (ulong)revision.TotalDownloads);
            revisionResolver.Add(CollectionRevision.TotalSize, Size.From(ulong.Parse(revision.TotalSize)));
            revisionResolver.Add(CollectionRevision.OverallRating, float.Parse(revision.OverallRating ?? "0.0"));
            revisionResolver.Add(CollectionRevision.TotalRatings, (ulong)(revision.OverallRatingCount ?? 0));
            revisionResolver.Add(CollectionRevision.ModCount, (ulong)revision.ModCount);
        }

        foreach (var tag in collectionInfo.Tags)
        {
            var categoryResolver = GraphQLResolver.Create(db, tx, CollectionTag.NexusId, ulong.Parse(tag.Id));
            categoryResolver.Add(CollectionTag.Name, tag.Name);
            collectionResolver.Add(Collection.Tags, categoryResolver.Id);
        }
        
        var txResults = await tx.Commit();

        return Collection.Load(txResults.Db, txResults[collectionResolver.Id]);
    }
    
    /// <summary>
    /// Get or add a collection metadata
    /// </summary>
    public async Task<CollectionRevision.ReadOnly> GetOrAddCollectionRevision(CollectionSlug slug, RevisionNumber revisionNumber, CancellationToken token)
    {
        var collection = await GetOrAddCollectionMetadata(slug, false, token);
        if (collection.Revisions.TryGetFirst(r => r.RevisionNumber == revisionNumber, out var revision)) 
            return revision;
        
        collection = await GetOrAddCollectionMetadata(slug, true, token);
        return collection.Revisions.First(r => r.RevisionNumber == revisionNumber);
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

    /// <summary>
    /// Parse a NXM URL and create a download job from the data
    /// </summary>
    public async Task<IJobTask<NexusModsDownloadJob, AbsolutePath>> CreateDownloadJob(
        AbsolutePath destination,
        NXMModUrl url,
        CancellationToken cancellationToken)
    {
        var nxmData = url.Key is not null && url.ExpireTime is not null ? (url.Key.Value, url.ExpireTime.Value) : Optional.None<(NXMKey, DateTime)>();
        return await CreateDownloadJob(destination, GameDomain.From(url.Game), url.ModId, url.FileId, nxmData, cancellationToken);
    }
    
    /// <summary>
    /// Given a mod ID, file ID, and game domain, create a download job
    /// </summary>
    public async Task<IJobTask<NexusModsDownloadJob, AbsolutePath>> CreateDownloadJob(
        AbsolutePath destination,
        GameDomain gameDomain,
        ModId modId,
        FileId fileId,
        Optional<(NXMKey, DateTime)> nxmData = default,
        CancellationToken cancellationToken = default)
    {
        var modPage = await GetOrAddModPage(modId, gameDomain, cancellationToken);
        var file = await GetOrAddFile(fileId, modPage, gameDomain, cancellationToken);
        
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
