using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using StrawberryShake;

namespace NexusMods.Networking.NexusWebApi.Errors;

/// <summary>
/// Represents a typed GraphQl error.
/// </summary>
[PublicAPI]
public interface IGraphQlError
{
    /// <summary>
    /// Gets the error code.
    /// </summary>
    ErrorCode Code { get; }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    string Message { get; }
}

/// <summary>
/// Represents a typed GraphQl error.
/// </summary>
[PublicAPI]
public interface IGraphQlError<TSelf> : IGraphQlError
    where TSelf : IGraphQlError<TSelf>
{
    /// <inheritdoc cref="IGraphQlError.Code"/>
    new static abstract ErrorCode Code { get; }

    ErrorCode IGraphQlError.Code => TSelf.Code;

    /// <summary>
    /// Whether the provided <see cref="IClientError"/> matches <typeparamref name="TSelf"/>.
    /// </summary>
    static virtual bool Matches(IClientError clientError) => clientError.Code is not null && TSelf.Code.Equals(clientError.Code);

    /// <summary>
    /// Tries to parse out the data from the <see cref="IClientError"/>.
    /// </summary>
    static abstract bool TryParse(IClientError clientError, [NotNullWhen(true)] out TSelf? error);
}
