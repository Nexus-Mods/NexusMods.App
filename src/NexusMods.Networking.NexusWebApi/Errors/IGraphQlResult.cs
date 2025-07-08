using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace NexusMods.Networking.NexusWebApi.Errors;

/// <summary>
/// Represents a GraphQl result.
/// </summary>
[PublicAPI]
public interface IGraphQlResult
{
    /// <summary>
    /// Gets whether the query returned data.
    /// </summary>
    bool HasData { get; }

    /// <summary>
    /// Gets whether the query returned errors.
    /// </summary>
    bool HasErrors { get; }

    /// <summary>
    /// Gets all errors produced by the query.
    /// </summary>
    IReadOnlyDictionary<ErrorCode, IGraphQlError> Errors { get; }

    /// <summary>
    /// Tries to get a specific error.
    /// </summary>
    bool TryGetError<TError>([NotNullWhen(true)] out TError? error) where TError : IGraphQlError<TError>
    {
        if (!Errors.TryGetValue(TError.Code, out var tmp))
        {
            error = default(TError);
            return false;
        }

        if (tmp is not TError errorInstance)
            throw new NotSupportedException($"Error with code `{TError.Code}` is of type `{tmp.GetType()}` but expected `{typeof(TError)}`");

        error = errorInstance;
        return true;
    }
}

/// <summary>
/// Represents a GraphQl result.
/// </summary>
[PublicAPI]
public interface IGraphQlResult<TData> : IGraphQlResult
    where TData : notnull
{
    /// <summary>
    /// Asserts that the result has and returns it.
    /// </summary>
    /// <remarks>
    /// Should only be used if you called <see cref="IGraphQlResult.HasData"/> beforehand to guarantee that the result has data.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when the result doesn't have data.</exception>
    TData AssertHasData();

    /// <summary>
    /// Tries to get the data.
    /// </summary>
    bool TryGetData([NotNullWhen(true)] out TData? data);
}

/// <inheritdoc cref="IGraphQlResult{TData}"/>
[PublicAPI]
public interface IGraphQlResult<TData, TError1> : IGraphQlResult<TData>
    where TData : notnull
    where TError1 : IGraphQlError<TError1>
{
    /// <inheritdoc cref="IGraphQlResult.TryGetError{TError}"/>>
    bool TryGetError([NotNullWhen(true)] out TError1? error) => TryGetError<TError1>(out error);
}

/// <inheritdoc cref="IGraphQlResult{TData}"/>
[PublicAPI]
public interface IGraphQlResult<TData, TError1, TError2> : IGraphQlResult<TData>
    where TData : notnull
    where TError1 : IGraphQlError<TError1>
    where TError2 : IGraphQlError<TError2>
{
    /// <inheritdoc cref="IGraphQlResult.TryGetError{TError}"/>>
    bool TryGetError([NotNullWhen(true)] out TError1? error) => TryGetError<TError1>(out error);

    /// <inheritdoc cref="IGraphQlResult.TryGetError{TError}"/>>
    bool TryGetError([NotNullWhen(true)] out TError2? error) => TryGetError<TError2>(out error);
}

/// <inheritdoc cref="IGraphQlResult{TData}"/>
[PublicAPI]
public interface IGraphQlResult<TData, TError1, TError2, TError3> : IGraphQlResult<TData>
    where TData : notnull
    where TError1 : IGraphQlError<TError1>
    where TError2 : IGraphQlError<TError2>
    where TError3 : IGraphQlError<TError3>
{
    /// <inheritdoc cref="IGraphQlResult.TryGetError{TError}"/>>
    bool TryGetError([NotNullWhen(true)] out TError1? error) => TryGetError<TError1>(out error);

    /// <inheritdoc cref="IGraphQlResult.TryGetError{TError}"/>>
    bool TryGetError([NotNullWhen(true)] out TError2? error) => TryGetError<TError2>(out error);

    /// <inheritdoc cref="IGraphQlResult.TryGetError{TError}"/>>
    bool TryGetError([NotNullWhen(true)] out TError3? error) => TryGetError<TError3>(out error);
}
