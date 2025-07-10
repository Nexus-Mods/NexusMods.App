using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Networking.NexusWebApi.Errors;

namespace NexusMods.Networking.NexusWebApi;

public partial class GraphQlClient
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

    /// <summary>
    /// Queries all categories, including global categories, for a game.
    /// </summary>
    public async ValueTask<GraphQlResult<ICategory[], NotFound>> QueryGameCategories(
        GameId gameId,
        CancellationToken cancellationToken)
    {
        var operationResult = await _client.QueryGameCategories.ExecuteAsync(
            gameId: (int)gameId.Value,
            cancellationToken: cancellationToken
        );

        if (operationResult.TryExtractErrors(out GraphQlResult<ICategory[], NotFound>? resultWithErrors, out var operationData))
            return resultWithErrors;

        Debug.Assert(operationData?.Categories is not null);
        return operationData.Categories.ToArray();
    }

    /// <summary>
    /// Queries all global categories.
    /// </summary>
    public async ValueTask<GraphQlResult<ICategory[]>> QueryGlobalCategories(
        CancellationToken cancellationToken)
    {
        var operationResult = await _client.QueryGlobalCategories.ExecuteAsync(
            cancellationToken: cancellationToken
        );

        if (operationResult.TryExtractErrors(out GraphQlResult<ICategory[]>? resultWithErrors, out var operationData))
            return resultWithErrors;

        Debug.Assert(operationData?.Categories is not null);
        return operationData.Categories.ToArray();
    }

    /// <summary>
    /// Requests a presigned upload URL for collection revisions.
    /// </summary>
    public async ValueTask<GraphQlResult<PresignedUploadUrl>> RequestCollectionRevisionUploadUrl(CancellationToken cancellationToken)
    {
        var operationResult = await _client.RequestCollectionRevisionUploadUrl.ExecuteAsync(
            cancellationToken: cancellationToken
        );

        if (operationResult.TryExtractErrors(out GraphQlResult<PresignedUploadUrl>? resultWithErrors, out var operationData))
            return resultWithErrors;

        Debug.Assert(operationData.CollectionRevisionUploadUrl is not null);
        return PresignedUploadUrl.FromApi(operationData.CollectionRevisionUploadUrl!);
    }

    /// <summary>
    /// Requests a presigned upload URL for media files.
    /// </summary>
    public async ValueTask<GraphQlResult<PresignedUploadUrl>> RequestMedaiUploadUrl(
        string mimeType,
        CancellationToken cancellationToken)
    {
        var operationResult = await _client.RequestMediaUploadUrl.ExecuteAsync(
            mimeType: mimeType,
            cancellationToken: cancellationToken
        );

        if (operationResult.TryExtractErrors(out GraphQlResult<PresignedUploadUrl>? resultWithErrors, out var operationData))
            return resultWithErrors;

        Debug.Assert(operationData.RequestMediaUploadUrl is not null);
        return PresignedUploadUrl.FromApi(operationData.RequestMediaUploadUrl!);
    }
}

