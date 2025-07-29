using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using StrawberryShake;

namespace NexusMods.Networking.NexusWebApi.Errors;

/// <summary>
/// Error returned when an entity was queried that doesn't exist. <see cref="Message"/> contains details about what entity wasn't found.
/// </summary>
[PublicAPI]
public record NotFound : IGraphQlError<NotFound>
{
    /// <inheritdoc/>
    public required string Message { get; init; }

    /// <inheritdoc/>
    public static ErrorCode Code { get; } = ErrorCode.From("NOT_FOUND");

    /// <inheritdoc/>
    public static bool TryParse(IClientError clientError, [NotNullWhen(true)] out NotFound? error)
    {
        Debug.Assert(Code.Equals(clientError.Code));
        error = new NotFound
        {
            Message = clientError.Message,
        };
        return true;
    }
}
