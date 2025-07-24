using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Networking.NexusWebApi.Errors;

namespace NexusMods.Networking.NexusWebApi;

public partial interface IGraphQlClient
{
    /// <summary>
    /// Creates a new collection on Nexus Mods. Requires the collection archive to be uploaded to
    /// <paramref name="presignedUploadUrl"/>.
    /// </summary>
    ValueTask<GraphQlResult<(ICollection, ICollectionRevision), Invalid>> CreateCollection(
        CollectionPayload payload,
        PresignedUploadUrl presignedUploadUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new draft revision or updates an existing draft revision. Requires the collection archive to be
    /// uploaded to <paramref name="presignedUploadUrl"/>.
    /// </summary>
    /// <remarks>
    /// If the collection has no draft revision, this API call will create a new draft revision. If the collection
    /// has an existing draft revision, this API call will update that revision. There can only be zero or one draft revisions
    /// on a collection at all times.
    /// </remarks>
    ValueTask<GraphQlResult<(ICollection, ICollectionRevision), Invalid, NotFound>> CreateOrUpdateRevision(
        CollectionId collectionId,
        CollectionPayload payload,
        PresignedUploadUrl presignedUploadUrl, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Edits the collection name.
    /// </summary>
    ValueTask<GraphQlResult<ICollection, NotFound>> RenameCollection(
        CollectionId collectionId,
        string newName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds the collection metadata required for publishing.
    /// </summary>
    ValueTask<GraphQlResult<ICollection, NotFound>> AddRequiredCollectionMetadata(
        CollectionId collectionId,
        ICategory category,
        string summary,
        string description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a tile image to the collection.
    /// </summary>
    ValueTask<GraphQlResult<NoData, NotFound>> AddTileImageToCollection(
        CollectionId collectionId,
        PresignedUploadUrl presignedUploadUrl,
        string mimeType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes the revision.
    /// </summary>
    ValueTask<GraphQlResult<NoData, NotFound, Invalid>> PublishRevision(
        RevisionId revisionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the collection status.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="newStatus"/> isn't <see cref="Abstractions.NexusModsLibrary.Models.CollectionStatus.Listed"/> or <see cref="Abstractions.NexusModsLibrary.Models.CollectionStatus.Unlisted"/>.</exception>
    ValueTask<GraphQlResult<NoData, NotFound>> ChangeCollectionStatus(
        CollectionId collectionId,
        Abstractions.NexusModsLibrary.Models.CollectionStatus newStatus,
        CancellationToken cancellationToken = default);
}
