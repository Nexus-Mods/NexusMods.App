using System.Diagnostics;
using DynamicData.Kernel;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Abstractions.NexusWebApi.Types.V2.Uid;
using NexusMods.Networking.NexusWebApi.Errors;
using NexusMods.Sdk.NexusModsApi;

namespace NexusMods.Networking.NexusWebApi;

internal partial class GraphQlClient : IGraphQlClient
{
    private readonly ILogger _logger;
    private readonly INexusGraphQLClient _client;

    public GraphQlClient(
        ILogger<GraphQlClient> logger,
        INexusGraphQLClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async ValueTask<GraphQlResult<ICategory[], NotFound>> QueryGameCategories(
        GameId gameId,
        CancellationToken cancellationToken)
    {
        var operationResult = await _client.QueryGameCategories.ExecuteAsync(
            gameId: (int)gameId.Value,
            cancellationToken: cancellationToken
        );

        return operationResult.Transform(out GraphQlResult<ICategory[], NotFound> _, static result => result.Categories, static categoryList => categoryList.ToArray<ICategory>());
    }

    public async ValueTask<GraphQlResult<ICategory[]>> QueryGlobalCategories(
        CancellationToken cancellationToken)
    {
        var operationResult = await _client.QueryGlobalCategories.ExecuteAsync(
            cancellationToken: cancellationToken
        );

        return operationResult.Transform(static result => result.Categories, static categoryList => categoryList.ToArray<ICategory>());
    }

    public async ValueTask<GraphQlResult<PresignedUploadUrl>> RequestCollectionRevisionUploadUrl(CancellationToken cancellationToken)
    {
        var operationResult = await _client.RequestCollectionRevisionUploadUrl.ExecuteAsync(
            cancellationToken: cancellationToken
        );

        return operationResult.Transform(static result => result.CollectionRevisionUploadUrl, PresignedUploadUrl.FromApi);
    }

    public async ValueTask<GraphQlResult<PresignedUploadUrl>> RequestMediaUploadUrl(
        string mimeType,
        CancellationToken cancellationToken)
    {
        var operationResult = await _client.RequestMediaUploadUrl.ExecuteAsync(
            mimeType: mimeType,
            cancellationToken: cancellationToken
        );

        return operationResult.Transform(static result => result.RequestMediaUploadUrl, PresignedUploadUrl.FromApi);
    }

    public async ValueTask<GraphQlResult<GameDomain, NotFound>> QueryGameDomain(GameId gameId, CancellationToken cancellationToken)
    {
        var operationResult = await _client.QueryGameById.ExecuteAsync(
            id: gameId.ToString(),
            cancellationToken: cancellationToken
        );

        return operationResult.Transform(out GraphQlResult<GameDomain, NotFound> _, static result => result.Game, static game => GameDomain.From(game.DomainName));
    }

    public async ValueTask<GraphQlResult<GameId, NotFound>> QueryGameId(GameDomain domain, CancellationToken cancellationToken)
    {
        var operationResult = await _client.QueryGameByDomain.ExecuteAsync(
            domain: domain.Value,
            cancellationToken: cancellationToken
        );

        return operationResult.Transform(out GraphQlResult<GameId, NotFound> _, static result => result.Game, static game => GameId.From((uint)game.Id));
    }

    public async ValueTask<GraphQlResult<IMod, NotFound>> QueryMod(ModId modId, GameId gameId, CancellationToken cancellationToken)
    {
        var operationResult = await _client.QueryMod.ExecuteAsync(
            modId: (int)modId.Value,
            gameId: (int)gameId.Value,
            cancellationToken: cancellationToken
        );

        return operationResult.Transform(out GraphQlResult<IMod, NotFound> _, static result => result, result => ExpectOne(result.LegacyMods.Nodes));
    }

    public async ValueTask<GraphQlResult<IModFile, NotFound>> QueryModFile(FileId fileId, GameId gameId, CancellationToken cancellationToken)
    {
        var uid = new UidForFile(fileId, gameId);

        var operationResult = await _client.QueryModFile.ExecuteAsync(
            uid: uid.ToV2Api(),
            cancellationToken: cancellationToken
        );

        return operationResult.Transform(out GraphQlResult<IModFile, NotFound> _, static result => result.ModFilesByUid, result => ExpectOne(result.Nodes));
    }

    public async ValueTask<GraphQlResult<IModFile[], NotFound>> QueryModFiles(ModId modId, GameId gameId, CancellationToken cancellationToken)
    {
        var operationResult = await _client.QueryModFiles.ExecuteAsync(
            modId: modId.ToString(),
            gameId: gameId.ToString(),
            cancellationToken: cancellationToken
        );

        return operationResult.Transform(out GraphQlResult<IModFile[], NotFound> _, static result => result.ModFiles, static fileList => fileList.ToArray<IModFile>());
    }

    public async ValueTask<GraphQlResult<CollectionId, NotFound>> QueryCollectionId(CollectionSlug slug, CancellationToken cancellationToken)
    {
        var operationResult = await _client.QueryCollectionId.ExecuteAsync(
            collectionSlug: slug.Value,
            cancellationToken: cancellationToken
        );

        return operationResult.Transform(out GraphQlResult<CollectionId, NotFound> _, static result => result.Collection, static collection => CollectionId.From((ulong)collection.Id));
    }
    
    public async ValueTask<GraphQlResult<ICollectionRevision, NotFound>> QueryCollectionRevision(CollectionSlug slug, RevisionNumber revisionNumber, CancellationToken cancellationToken)
    {
        var operationResult = await _client.QueryCollectionRevision.ExecuteAsync(
            collectionSlug: slug.Value,
            revisionNumber: (int)revisionNumber.Value,
            cancellationToken: cancellationToken
        );

        return operationResult.Transform(out GraphQlResult<ICollectionRevision, NotFound> _, static result => result.CollectionRevision, static revision => revision);
    }
    
    public async ValueTask<GraphQlResult<(RevisionNumber Number, RevisionStatus Status)[], NotFound>> QueryCollectionRevisionNumbers(CollectionSlug collectionSlug, GameDomain gameDomain, CancellationToken cancellationToken)
    {
        var operationResult = await _client.QueryCollectionRevisionNumbers.ExecuteAsync(
            collectionSlug: collectionSlug.Value,
            gameDomainName: gameDomain.Value,
            cancellationToken: cancellationToken
        );

        return operationResult.Transform(out GraphQlResult<(RevisionNumber, RevisionStatus)[], NotFound> _, static result => result.Collection, Transformer);
        (RevisionNumber, RevisionStatus)[] Transformer(IQueryCollectionRevisionNumbers_Collection collection)
        {
            var revisionNumbers = collection.Revisions
                .OrderByDescending(x => x.RevisionNumber)
                .Select(data =>
                {
                    var revisionNumber = RevisionNumber.From((ulong)data.RevisionNumber);
                    var status = ToStatus(data);
                    if (!status.HasValue) return Optional<(RevisionNumber, RevisionStatus)>.None;
                    return (revisionNumber, status.Value);
                })
                .Where(x => x.HasValue)
                .Select(x => x.Value)
                .ToArray();

            return revisionNumbers;
        }
    }

    public async ValueTask<GraphQlResult<string, NotFound>> QueryCollectionRevisionDownloadLink(CollectionSlug collectionSlug, RevisionNumber revisionNumber, CancellationToken cancellationToken)
    {
        var operationResult = await _client.QueryCollectionRevisionDownloadLink.ExecuteAsync(
            collectionSlug: collectionSlug.Value,
            revisionNumber: (int)revisionNumber.Value,
            cancellationToken: cancellationToken
        );

        return operationResult.Transform(out GraphQlResult<string, NotFound> _, static result => result.CollectionRevision, static revision => revision.DownloadLink);
    }

    private T ExpectOne<T>(IReadOnlyList<T> list)
    {
        Debug.Assert(list.Count != 0, "expect result to contain at least one item");

        if (list.Count == 1) return list[0];
        if (list.Count == 0) throw new InvalidOperationException("Expected result to contain at least one item");

        _logger.LogWarning("API returned {Count} items of type `{Type}`, expected a single item", list.Count, typeof(T));
        return list[0];
    }

    private Optional<RevisionStatus> ToStatus(ICollectionRevisionStatus revisionFragment)
    {
        // NOTE(erri120): no idea why the revision fragment has two strings for the status
        Debug.Assert(revisionFragment.Status.Equals(revisionFragment.RevisionStatus, StringComparison.OrdinalIgnoreCase), $"weird things happening: {revisionFragment.Status} != {revisionFragment.RevisionStatus}");

        if (revisionFragment.Status.Equals("draft", StringComparison.OrdinalIgnoreCase))
            return RevisionStatus.Draft;
        if (revisionFragment.Status.Equals("published", StringComparison.OrdinalIgnoreCase))
            return RevisionStatus.Published;
        _logger.LogWarning("Unknown revision status: `{Status}`", revisionFragment.Status);
        return Optional<RevisionStatus>.None;
    }
}
