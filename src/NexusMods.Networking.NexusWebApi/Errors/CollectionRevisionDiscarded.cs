using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using StrawberryShake;

namespace NexusMods.Networking.NexusWebApi.Errors;

/// <summary>
/// Error returned when the queried collection revision was discarded.
/// </summary>
[PublicAPI]
public record CollectionRevisionDiscarded : IGraphQlError<CollectionRevisionDiscarded>
{
    /// <inheritdoc/>
    public required string Message { get; init; }

    /// <inheritdoc/>
    public static ErrorCode Code { get; } = ErrorCode.From("COLLECTION_REVISION_DISCARDED");

    /// <inheritdoc/>
    public static bool TryParse(IClientError clientError, [NotNullWhen(true)] out CollectionRevisionDiscarded? error)
    {
        Debug.Assert(Code.Equals(clientError.Code));
        error = new CollectionRevisionDiscarded
        {
            Message = clientError.Message,
        };

        var extensions = clientError.Extensions;
        if (extensions is null) return true;

        if (extensions.TryGetValue("revision_number", out var oRevisionNumber))
        {
            // TODO: figure out the type
            Debugger.Break();
        }

        return true;
    }
}
