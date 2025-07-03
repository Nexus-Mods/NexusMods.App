using JetBrains.Annotations;

namespace NexusMods.Networking.NexusWebApi.Errors;

/// <summary>
/// Catch-all type for all unknown not explicitly typed errors.
/// </summary>
[PublicAPI]
public record UnknownError : IGraphQlError
{
    internal static readonly ErrorCode DefaultCode = ErrorCode.From("NMA_UNKNOWN_ERROR");

    /// <inheritdoc/>
    public required ErrorCode Code { get; init; }

    /// <inheritdoc/>
    public required string Message { get; init; }

    /// <summary>
    /// Direct error.
    /// </summary>
    public required StrawberryShake.IClientError ClientError { get; init; }
}
