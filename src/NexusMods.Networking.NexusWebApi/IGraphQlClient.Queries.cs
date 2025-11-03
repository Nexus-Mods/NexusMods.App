using JetBrains.Annotations;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Networking.NexusWebApi.Errors;
using NexusMods.Sdk.NexusModsApi;

namespace NexusMods.Networking.NexusWebApi;

[PublicAPI]
public partial interface IGraphQlClient
{
    /// <summary>
    /// Queries all categories, including global categories, for a game.
    /// </summary>
    ValueTask<GraphQlResult<ICategory[], NotFound>> QueryGameCategories(
        NexusModsGameId nexusModsGameId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries all global categories.
    /// </summary>
    ValueTask<GraphQlResult<ICategory[]>> QueryGlobalCategories(CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests a presigned upload URL for collection revisions.
    /// </summary>
    ValueTask<GraphQlResult<PresignedUploadUrl>> RequestCollectionRevisionUploadUrl(CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests a presigned upload URL for media files.
    /// </summary>
    ValueTask<GraphQlResult<PresignedUploadUrl>> RequestMediaUploadUrl(
        string mimeType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries the domain of a game by id.
    /// </summary>
    ValueTask<GraphQlResult<GameDomain, NotFound>> QueryGameDomain(
        NexusModsGameId nexusModsGameId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries the id of a game by domain.
    /// </summary>
    ValueTask<GraphQlResult<NexusModsGameId, NotFound>> QueryGameId(
        GameDomain domain,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries a mod.
    /// </summary>
    ValueTask<GraphQlResult<IMod, NotFound>> QueryMod(
        ModId modId,
        NexusModsGameId nexusModsGameId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries a single file.
    /// </summary>
    ValueTask<GraphQlResult<IModFile, NotFound>> QueryModFile(
        FileId fileId,
        NexusModsGameId nexusModsGameId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries all files of a mod.
    /// </summary>
    ValueTask<GraphQlResult<IModFile[], NotFound>> QueryModFiles(
        ModId modId,
        NexusModsGameId nexusModsGameId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries the ID of a collection by slug.
    /// </summary>
    ValueTask<GraphQlResult<CollectionId, NotFound>> QueryCollectionId(
        CollectionSlug slug,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Queries a collection revision.
    /// </summary>
    ValueTask<GraphQlResult<ICollectionRevision, NotFound>> QueryCollectionRevision(
        CollectionSlug collectionSlug,
        RevisionNumber revisionNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries all revisions of a collection.
    /// </summary>
    ValueTask<GraphQlResult<(RevisionNumber Number, RevisionStatus Status)[], NotFound>> QueryCollectionRevisionNumbers(
        CollectionSlug collectionSlug,
        GameDomain gameDomain,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries the download link for a revision.
    /// </summary>
    ValueTask<GraphQlResult<string, NotFound>> QueryCollectionRevisionDownloadLink(
        CollectionSlug collectionSlug,
        RevisionNumber revisionNumber,
        CancellationToken cancellationToken = default);
}
