using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Sdk.Collections;
using StrawberryShake;

namespace NexusMods.Networking.NexusWebApi.Errors;

file static class Helper
{
    internal static readonly IReadOnlyDictionary<ErrorCode, IGraphQlError> NoErrors = FrozenDictionary<ErrorCode, IGraphQlError>.Empty;

    internal static IReadOnlyDictionary<ErrorCode, IGraphQlError> ToErrors<TError>(TError error)
        where TError : IGraphQlError<TError>
    {
        return new SingleValueDictionary<ErrorCode, IGraphQlError>(new KeyValuePair<ErrorCode, IGraphQlError>(TError.Code, error));
    }
}

/// <summary>
/// Represents a GraphQl result.
/// </summary>
[PublicAPI]
public class GraphQlResult<TData> : IGraphQlResult<TData>
    where TData : notnull
{
    private readonly Optional<TData> _data;

    /// <inheritdoc/>
    public IReadOnlyDictionary<ErrorCode, IGraphQlError> Errors { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    public GraphQlResult(TData data)
    {
        _data = data;
        Errors = Helper.NoErrors;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    public GraphQlResult(IReadOnlyDictionary<ErrorCode, IGraphQlError> errors)
    {
        _data = Optional<TData>.None;
        Errors = errors;
    }

    /// <inheritdoc/>
    public bool HasData => _data.HasValue;

    /// <inheritdoc/>
    public bool HasErrors => Errors.Count > 0;

    /// <inheritdoc/>
    public TData AssertHasData()
    {
        if (!HasData) throw new InvalidOperationException();
        return _data.Value;
    }

    /// <inheritdoc/>
    public bool TryGetData([NotNullWhen(true)] out TData? data)
    {
        if (!_data.HasValue)
        {
            data = default(TData);
            return false;
        }

        data = _data.Value;
        return true;
    }

    /// <inheritdoc/>
    public override string ToString() => $"HasData = {HasData} ({_data}) ErrorsCount = {Errors.Count}";

    /// <summary>
    /// Implicit conversion.
    /// </summary>
    public static implicit operator GraphQlResult<TData>(TData data) => new(data);
}

/// <inheritdoc cref="GraphQlResult{TData}"/>
[PublicAPI]
public class GraphQlResult<TData, TError1> : GraphQlResult<TData>, IGraphQlResult<TData, TError1>
    where TData : notnull
    where TError1 : IGraphQlError<TError1>
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public GraphQlResult(TData data) : base(data) { }

    /// <summary>
    /// Constructor.
    /// </summary>
    public GraphQlResult(IReadOnlyDictionary<ErrorCode, IGraphQlError> errors) : base(errors) { }

    /// <summary>
    /// Constructor.
    /// </summary>
    public GraphQlResult(TError1 error) : base(Helper.ToErrors(error)) { }

    /// <summary>
    /// Implicit conversion.
    /// </summary>
    public static implicit operator GraphQlResult<TData, TError1>(TData data) => new(data);
}

/// <inheritdoc cref="GraphQlResult{TData}"/>
[PublicAPI]
public class GraphQlResult<TData, TError1, TError2> : GraphQlResult<TData>, IGraphQlResult<TData, TError1, TError2>
    where TData : notnull
    where TError1 : IGraphQlError<TError1>
    where TError2 : IGraphQlError<TError2>
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public GraphQlResult(TData data) : base(data) { }

    /// <summary>
    /// Constructor.
    /// </summary>
    public GraphQlResult(IReadOnlyDictionary<ErrorCode, IGraphQlError> errors) : base(errors) { }

    /// <summary>
    /// Constructor.
    /// </summary>
    public GraphQlResult(TError1 error) : base(Helper.ToErrors(error)) { }

    /// <summary>
    /// Constructor.
    /// </summary>
    public GraphQlResult(TError2 error) : base(Helper.ToErrors(error)) { }

    /// <summary>
    /// Implicit conversion.
    /// </summary>
    public static implicit operator GraphQlResult<TData, TError1, TError2>(TData data) => new(data);
}

/// <inheritdoc cref="GraphQlResult{TData}"/>
[PublicAPI]
public class GraphQlResult<TData, TError1, TError2, TError3> : GraphQlResult<TData>, IGraphQlResult<TData, TError1, TError2, TError3>
    where TData : notnull
    where TError1 : IGraphQlError<TError1>
    where TError2 : IGraphQlError<TError2>
    where TError3 : IGraphQlError<TError3>
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public GraphQlResult(TData data) : base(data) { }

    /// <summary>
    /// Constructor.
    /// </summary>
    public GraphQlResult(IReadOnlyDictionary<ErrorCode, IGraphQlError> errors) : base(errors) { }

    /// <summary>
    /// Constructor.
    /// </summary>
    public GraphQlResult(TError1 error) : base(Helper.ToErrors(error)) { }

    /// <summary>
    /// Constructor.
    /// </summary>
    public GraphQlResult(TError2 error) : base(Helper.ToErrors(error)) { }

    /// <summary>
    /// Constructor.
    /// </summary>
    public GraphQlResult(TError3 error) : base(Helper.ToErrors(error)) { }

    /// <summary>
    /// Implicit conversion.
    /// </summary>
    public static implicit operator GraphQlResult<TData, TError1, TError2, TError3>(TData data) => new(data);
}

/// <summary>
/// Static methods
/// </summary>
[PublicAPI]
public static class GraphQlResult
{
    /// <summary>
    /// Tries to extract all errors.
    /// </summary>
    public static bool TryExtractErrors<TData, TError1>(IOperationResult operationResult, [NotNullWhen(true)] out GraphQlResult<TData, TError1>? result)
        where TData : notnull
        where TError1 : IGraphQlError<TError1>
    {
        var errors = operationResult.Errors;
        if (errors.Count == 0)
        {
            result = null;
            return false;
        }

        if (errors.Count == 1)
        {
            var error = errors[0];

            if (TError1.Matches(error) && TError1.TryParse(error, out var error1))
            {
                result = new GraphQlResult<TData, TError1>(error1);
                return true;
            }

            ThrowUnsupported(error);
        }

        var parsedErrors = new Dictionary<ErrorCode, IGraphQlError>();

        foreach (var error in errors)
        {
            if (TError1.Matches(error) && TError1.TryParse(error, out var error1))
            {
                parsedErrors[TError1.Code] = error1;
            }
            else
            {
                ThrowUnsupported(error);
            }
        }

        result = new GraphQlResult<TData, TError1>(parsedErrors);
        return true;
    }

    /// <summary>
    /// Tries to extract all errors.
    /// </summary>
    public static bool TryExtractErrors<TData, TError1, TError2>(IOperationResult operationResult, [NotNullWhen(true)] out GraphQlResult<TData, TError1, TError2>? result)
        where TData : notnull
        where TError1 : IGraphQlError<TError1>
        where TError2 : IGraphQlError<TError2>
    {
        var errors = operationResult.Errors;
        if (errors.Count == 0)
        {
            result = null;
            return false;
        }

        if (errors.Count == 1)
        {
            var error = errors[0];

            if (TError1.Matches(error) && TError1.TryParse(error, out var error1))
            {
                result = new GraphQlResult<TData, TError1, TError2>(error1);
                return true;
            }

            if (TError2.Matches(error) && TError2.TryParse(error, out var error2))
            {
                result = new GraphQlResult<TData, TError1, TError2>(error2);
                return true;
            }

            ThrowUnsupported(error);
        }

        var parsedErrors = new Dictionary<ErrorCode, IGraphQlError>();

        foreach (var error in errors)
        {
            if (TError1.Matches(error) && TError1.TryParse(error, out var error1))
            {
                parsedErrors[TError1.Code] = error1;
            }
            else if (TError2.Matches(error) && TError2.TryParse(error, out var error2))
            {
                parsedErrors[TError2.Code] = error2;
            }
            else
            {
                ThrowUnsupported(error);
            }
        }

        result = new GraphQlResult<TData, TError1, TError2>(parsedErrors);
        return true;
    }

    /// <summary>
    /// Tries to extract all errors.
    /// </summary>
    public static bool TryExtractErrors<TData, TError1, TError2, TError3>(IOperationResult operationResult, [NotNullWhen(true)] out GraphQlResult<TData, TError1, TError2, TError3>? result)
        where TData : notnull
        where TError1 : IGraphQlError<TError1>
        where TError2 : IGraphQlError<TError2>
        where TError3 : IGraphQlError<TError3>
    {
        var errors = operationResult.Errors;
        if (errors.Count == 0)
        {
            result = null;
            return false;
        }

        if (errors.Count == 1)
        {
            var error = errors[0];

            if (TError1.Matches(error) && TError1.TryParse(error, out var error1))
            {
                result = new GraphQlResult<TData, TError1, TError2, TError3>(error1);
                return true;
            }

            if (TError2.Matches(error) && TError2.TryParse(error, out var error2))
            {
                result = new GraphQlResult<TData, TError1, TError2, TError3>(error2);
                return true;
            }

            if (TError3.Matches(error) && TError3.TryParse(error, out var error3))
            {
                result = new GraphQlResult<TData, TError1, TError2, TError3>(error3);
                return true;
            }

            ThrowUnsupported(error);
        }

        var parsedErrors = new Dictionary<ErrorCode, IGraphQlError>();

        foreach (var error in errors)
        {
            if (TError1.Matches(error) && TError1.TryParse(error, out var error1))
            {
                parsedErrors[TError1.Code] = error1;
            }
            else if (TError2.Matches(error) && TError2.TryParse(error, out var error2))
            {
                parsedErrors[TError2.Code] = error2;
            }
            else if (TError3.Matches(error) && TError3.TryParse(error, out var error3))
            {
                parsedErrors[TError3.Code] = error3;
            }
            else
            {
                ThrowUnsupported(error);
            }
        }

        result = new GraphQlResult<TData, TError1, TError2, TError3>(parsedErrors);
        return true;
    }

    [DoesNotReturn]
    private static void ThrowUnsupported(IClientError error)
    {
        Debugger.Break();
        throw new NotSupportedException($"Unknown error: `{error.Message}` Code={error.Code} Exception={error.Exception}");
    }
}
