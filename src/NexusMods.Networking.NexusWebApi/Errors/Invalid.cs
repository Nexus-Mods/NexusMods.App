using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using StrawberryShake;

namespace NexusMods.Networking.NexusWebApi.Errors;

/// <summary>
/// Error returned for mutations where some input values were invalid or missing.
/// </summary>
[PublicAPI]
public record Invalid : IGraphQlError<Invalid>
{
    /// <inheritdoc/>
    public required string Message { get; init; }

    /// <inheritdoc/>
    public static ErrorCode Code { get; } = ErrorCode.From("INVALID");

    /// <inheritdoc/>
    public static bool TryParse(IClientError clientError, [NotNullWhen(true)] out Invalid? error)
    {
        Debug.Assert(Code.Equals(clientError.Code));
        error = new Invalid
        {
            Message = clientError.Message,
        };
        return true;
    }
}
