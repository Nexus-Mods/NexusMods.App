using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using StrawberryShake;

namespace NexusMods.Networking.NexusWebApi.Errors;

/// <summary>
/// Error returned when the queried collection was discarded.
/// </summary>
[PublicAPI]
public record CollectionDiscarded : IGraphQlError<CollectionDiscarded>
{
    /// <inheritdoc/>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the name of the collection that was queried.
    /// </summary>
    public string? CollectionName { get; private set; }

    /// <inheritdoc/>
    public static ErrorCode Code { get; } = ErrorCode.From("COLLECTION_DISCARDED");

    /// <inheritdoc/>
    public static bool TryParse(IClientError clientError, [NotNullWhen(true)] out CollectionDiscarded? error)
    {
        Debug.Assert(Code.Equals(clientError.Code));
        error = new CollectionDiscarded
        {
            Message = clientError.Message,
        };

        var extensions = clientError.Extensions;
        if (extensions is null) return true;

        if (extensions.TryGetValue("title", out var oTitle) && oTitle is string title)
        {
            error.CollectionName = title;
        }

        return true;
    }
}
