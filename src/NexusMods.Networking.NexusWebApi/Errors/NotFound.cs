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
    public string Message { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    public NotFound(string message)
    {
        Message = message;
    }

    /// <inheritdoc/>
    public static ErrorCode Code { get; } = ErrorCode.From("NOT_FOUND");

    /// <inheritdoc/>
    public static bool TryParse(IClientError clientError, [NotNullWhen(true)] out NotFound? error)
    {
        Debug.Assert(Code.Equals(clientError.Code));
        error = new NotFound(clientError.Message);
        return true;
    }
}
