using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Networking.NexusWebApi.Errors;

namespace NexusMods.Networking.NexusWebApi;

internal partial class GraphQlClient
{
    public async ValueTask<GraphQlResult<(ICollection, ICollectionRevision), Invalid>> CreateCollection(
        CollectionPayload payload,
        PresignedUploadUrl presignedUploadUrl,
        CancellationToken cancellationToken)
    {
        var operationResult = await _client.CreateCollection.ExecuteAsync(
            payload: payload,
            storageUUID: presignedUploadUrl.UUID,
            cancellationToken: cancellationToken
        );

        return operationResult.Transform(
            out GraphQlResult<(ICollection, ICollectionRevision), Invalid> _,
            static result => result.CreateCollection,
            create => RequiresSuccess(create, static create => create.Success, static create => (create.Collection, create.Revision))
        );
    }

    public async ValueTask<GraphQlResult<(ICollection, ICollectionRevision), Invalid, NotFound>> CreateOrUpdateRevision(
        CollectionId collectionId,
        CollectionPayload payload,
        PresignedUploadUrl presignedUploadUrl,
        CancellationToken cancellationToken)
    {
        var operationResult = await _client.CreateOrUpdateRevision.ExecuteAsync(
            payload: payload,
            collectionId: (int)collectionId.Value,
            storageUUID: presignedUploadUrl.UUID,
            cancellationToken: cancellationToken
        );

        return operationResult.Transform(
            out GraphQlResult<(ICollection, ICollectionRevision), Invalid, NotFound> _,
            static result => result.CreateOrUpdateRevision,
            createOrUpdate => RequiresSuccess(createOrUpdate, static createOrUpdate => createOrUpdate.Success, static createOrUpdate => (createOrUpdate.Collection, createOrUpdate.Revision))
        );
    }

    public async ValueTask<GraphQlResult<ICollection, NotFound>> RenameCollection(
        CollectionId collectionId,
        string newName,
        CancellationToken cancellationToken)
    {
        var operationResult = await _client.RenameCollection.ExecuteAsync(
            collectionId: (int)collectionId.Value,
            newName: newName,
            cancellationToken: cancellationToken
        );

        return operationResult.Transform(
            out GraphQlResult<ICollection, NotFound> _,
            static result => result.EditCollection,
            edit => RequiresSuccess(edit, static edit => edit.Success, static edit => edit.Collection)
        );
    }

    public async ValueTask<GraphQlResult<ICollection, NotFound>> AddRequiredCollectionMetadata(
        CollectionId collectionId,
        ICategory category,
        string summary,
        string description,
        CancellationToken cancellationToken)
    {
        var operationResult = await _client.AddRequiredCollectionMetadata.ExecuteAsync(
            collectionId: (int)collectionId.Value,
            categoryId: category.Id.ToString(),
            summary: summary,
            description: description,
            cancellationToken
        );

        return operationResult.Transform(
            out GraphQlResult<ICollection, NotFound> _,
            static result => result.EditCollection,
            edit => RequiresSuccess(edit, static edit => edit.Success, static edit => edit.Collection)
        );
    }

    public async ValueTask<GraphQlResult<NoData, NotFound>> AddTileImageToCollection(
        CollectionId collectionId,
        PresignedUploadUrl presignedUploadUrl,
        string mimeType,
        CancellationToken cancellationToken)
    {
        var operationResult = await _client.AddTileImageToCollection.ExecuteAsync(
            collectionId: collectionId.ToString(),
            image: new UploadImageInput
            {
                Id = presignedUploadUrl.UUID,
                ContentType = mimeType,
            },
            cancellationToken: cancellationToken
        );

        return operationResult.Transform(out GraphQlResult<NoData, NotFound> _, static result => result.AddTileImageToCollection, static _ => new NoData());
    }
    
    public async ValueTask<GraphQlResult<NoData, NotFound, Invalid>> PublishRevision(
        RevisionId revisionId,
        CancellationToken cancellationToken)
    {
        var operationResult = await _client.PublishRevision.ExecuteAsync(
            revisionId: revisionId.ToString(),
            cancellationToken: cancellationToken
        );

        return operationResult.Transform(
            out GraphQlResult<NoData, NotFound, Invalid> _,
            static result => result.PublishRevision,
            publish => RequiresSuccess(publish, static publish => publish.Success, _ => new NoData())
        );
    }

    public ValueTask<GraphQlResult<NoData, NotFound>> ChangeCollectionStatus(
        CollectionId collectionId,
        Abstractions.NexusModsLibrary.Models.CollectionStatus newStatus,
        CancellationToken cancellationToken)
    {
        if (newStatus is not Abstractions.NexusModsLibrary.Models.CollectionStatus.Listed and not Abstractions.NexusModsLibrary.Models.CollectionStatus.Unlisted)
            throw new ArgumentException($"Collection status can only be changed to {CollectionStatus.Listed} or {CollectionStatus.Unlisted} and not {newStatus}", nameof(newStatus));

        if (newStatus is Abstractions.NexusModsLibrary.Models.CollectionStatus.Listed) return ChangeCollectionStatusToListed(collectionId, cancellationToken);

        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        Debug.Assert(newStatus is Abstractions.NexusModsLibrary.Models.CollectionStatus.Unlisted);

        return ChangeCollectionStatusToUnlisted(collectionId, cancellationToken);
    }

    private async ValueTask<GraphQlResult<NoData, NotFound>> ChangeCollectionStatusToListed(CollectionId collectionId, CancellationToken cancellationToken)
    {
        var operationResult = await _client.ChangeCollectionStatusToListed.ExecuteAsync(
            collectionId: (int)collectionId.Value,
            cancellationToken: cancellationToken
        );

        return operationResult.Transform(
            out GraphQlResult<NoData,NotFound> _,
            static result => result.ListCollection,
            list => RequiresSuccess(list, static list => list.Success, _ => new NoData())
        );
    }

    private async ValueTask<GraphQlResult<NoData, NotFound>> ChangeCollectionStatusToUnlisted(CollectionId collectionId, CancellationToken cancellationToken)
    {
        var operationResult = await _client.ChangeCollectionStatusToUnlisted.ExecuteAsync(
            collectionId: collectionId.ToString(),
            cancellationToken: cancellationToken
        );

        return operationResult.Transform(
            out GraphQlResult<NoData,NotFound> _,
            static result => result.UnlistCollection,
            unlist => RequiresSuccess(unlist, static unlist => unlist.Success, _ => new NoData())
        );
    }

    private TOutput RequiresSuccess<TInput, TOutput>(
        TInput input,
        Func<TInput, bool> successSelector,
        Func<TInput, TOutput> outputSelector,
        [CallerMemberName] string callerMemberName = "")
    {
        var isSuccess = successSelector(input);

        // NOTE(erri120): If you somehow ended up here, that means the API returned no GraphQL errors. However, for some unknown reason,
        // there is this extra "success" boolean on some API results for mutations. I don't know why this exists or what this represents.
        // If you landed here, please let me know and open an issue on GitHub.
        Debug.Assert(isSuccess, "expect this to always be true");
        if (!isSuccess) _logger.LogWarning("API returned no errors but the result doesn't indicate success on `{Type}` for `{Caller}`", typeof(TInput), callerMemberName);

        return outputSelector(input);
    }
}
