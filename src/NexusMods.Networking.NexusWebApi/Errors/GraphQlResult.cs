using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Sdk;
using StrawberryShake;

namespace NexusMods.Networking.NexusWebApi.Errors;

file static class Helper
{
    internal static KeyValuePair<ErrorCode, IGraphQlError>[] ToErrors(IGraphQlError error)
    {
        return [new KeyValuePair<ErrorCode, IGraphQlError>(error.Code, error)];
    }
}

/// <summary>
/// Represents no data.
/// </summary>
public record NoData;

/// <summary>
/// Represents a GraphQl result.
/// </summary>
[PublicAPI]
public class GraphQlResult<TData> : IGraphQlResult<TData>
    where TData : notnull
{
    private readonly Optional<TData> _data;

    /// <inheritdoc/>
    public KeyValuePair<ErrorCode, IGraphQlError>[] Errors { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    public GraphQlResult(TData data)
    {
        _data = data;
        Errors = [];
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    public GraphQlResult(KeyValuePair<ErrorCode, IGraphQlError>[] errors)
    {
        _data = Optional<TData>.None;
        Errors = errors;
    }

    /// <inheritdoc/>
    public bool HasData => _data.HasValue;

    /// <inheritdoc/>
    public bool HasErrors => Errors.Length > 0;

    /// <inheritdoc/>
    public TData AssertHasData()
    {
        Debug.Assert(HasData, "Result should have data when this method is called, use TryGetData instead if you can't guarantee it");
        if (!HasData) throw new InvalidOperationException($"Expected the result to contain data but it has {Errors.Length} errors instead");
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

    /// <summary>
    /// Tries to get a specific error.
    /// </summary>
    public bool TryGetError<TError>([NotNullWhen(true)] out TError? error) where TError : IGraphQlError<TError>
    {
        if (!Errors.TryGetFirst(kv => kv.Key.Equals(TError.Code), out var tmp))
        {
            error = default(TError);
            return false;
        }

        if (tmp is not TError errorInstance)
            throw new NotSupportedException($"Error with code `{TError.Code}` is of type `{tmp.GetType()}` but expected `{typeof(TError)}`");

        error = errorInstance;
        return true;
    }

    /// <inheritdoc/>
    public override string ToString() => $"HasData = {HasData} ({_data}) ErrorsCount = {Errors.Length}";

    /// <summary>
    /// Implicit conversion.
    /// </summary>
    public static implicit operator GraphQlResult<TData>(TData data) => new(data);

    /// <summary>
    /// Implicit conversion.
    /// </summary>
    public static implicit operator GraphQlResult<TData>(KeyValuePair<ErrorCode, IGraphQlError>[] errors) => new(errors);
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
    public GraphQlResult(KeyValuePair<ErrorCode, IGraphQlError>[] errors) : base(errors) { }

    /// <summary>
    /// Constructor.
    /// </summary>
    public GraphQlResult(TError1 error) : base(Helper.ToErrors(error)) { }

    /// <summary>
    /// Maps data from <typeparamref name="TData"/> to <typeparamref name="TOther"/> using provided delegate.
    /// </summary>
    public GraphQlResult<TOther, TError1> Map<TOther>(Func<TData, TOther> func)
        where TOther : notnull
    {
        if (!HasData) return new GraphQlResult<TOther, TError1>(Errors);
        var data = func(AssertHasData());
        return new GraphQlResult<TOther, TError1>(data);
    }

    /// <inheritdoc cref="IGraphQlResult.TryGetError{TError}"/>>
    public bool TryGetError([NotNullWhen(true)] out TError1? error) => TryGetError<TError1>(out error);

    /// <summary>
    /// Implicit conversion.
    /// </summary>
    public static implicit operator GraphQlResult<TData, TError1>(TData data) => new(data);

    /// <summary>
    /// Implicit conversion.
    /// </summary>
    public static implicit operator GraphQlResult<TData, TError1>(KeyValuePair<ErrorCode, IGraphQlError>[] errors) => new(errors);
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
    public GraphQlResult(KeyValuePair<ErrorCode, IGraphQlError>[] errors) : base(errors) { }

    /// <summary>
    /// Constructor.
    /// </summary>
    public GraphQlResult(TError1 error) : base(Helper.ToErrors(error)) { }

    /// <summary>
    /// Constructor.
    /// </summary>
    public GraphQlResult(TError2 error) : base(Helper.ToErrors(error)) { }

    /// <summary>
    /// Maps data from <typeparamref name="TData"/> to <typeparamref name="TOther"/> using provided delegate.
    /// </summary>
    public GraphQlResult<TOther, TError1, TError2> Map<TOther>(Func<TData, TOther> func)
        where TOther : notnull
    {
        if (!HasData) return new GraphQlResult<TOther, TError1, TError2>(Errors);
        var data = func(AssertHasData());
        return new GraphQlResult<TOther, TError1, TError2>(data);
    }

    /// <inheritdoc cref="IGraphQlResult.TryGetError{TError}"/>>
    public bool TryGetError([NotNullWhen(true)] out TError1? error) => TryGetError<TError1>(out error);

    /// <inheritdoc cref="IGraphQlResult.TryGetError{TError}"/>>
    public bool TryGetError([NotNullWhen(true)] out TError2? error) => TryGetError<TError2>(out error);

    /// <summary>
    /// Implicit conversion.
    /// </summary>
    public static implicit operator GraphQlResult<TData, TError1, TError2>(TData data) => new(data);

    /// <summary>
    /// Implicit conversion.
    /// </summary>
    public static implicit operator GraphQlResult<TData, TError1, TError2>(KeyValuePair<ErrorCode, IGraphQlError>[] errors) => new(errors);
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
    public GraphQlResult(KeyValuePair<ErrorCode, IGraphQlError>[] errors) : base(errors) { }

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
    /// Maps data from <typeparamref name="TData"/> to <typeparamref name="TOther"/> using provided delegate.
    /// </summary>
    public GraphQlResult<TOther, TError1, TError2, TError3> Map<TOther>(Func<TData, TOther> func)
        where TOther : notnull
    {
        if (!HasData) return new GraphQlResult<TOther, TError1, TError2, TError3>(Errors);
        var data = func(AssertHasData());
        return new GraphQlResult<TOther, TError1, TError2, TError3>(data);
    }

    /// <inheritdoc cref="IGraphQlResult.TryGetError{TError}"/>>
    public bool TryGetError([NotNullWhen(true)] out TError1? error) => TryGetError<TError1>(out error);

    /// <inheritdoc cref="IGraphQlResult.TryGetError{TError}"/>>
    public bool TryGetError([NotNullWhen(true)] out TError2? error) => TryGetError<TError2>(out error);

    /// <inheritdoc cref="IGraphQlResult.TryGetError{TError}"/>>
    public bool TryGetError([NotNullWhen(true)] out TError3? error) => TryGetError<TError3>(out error);

    /// <summary>
    /// Implicit conversion.
    /// </summary>
    public static implicit operator GraphQlResult<TData, TError1, TError2, TError3>(TData data) => new(data);

    /// <summary>
    /// Implicit conversion.
    /// </summary>
    public static implicit operator GraphQlResult<TData, TError1, TError2, TError3>(KeyValuePair<ErrorCode, IGraphQlError>[] errors) => new(errors);
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
    public static bool TryExtractErrors<TOperationData, TData>(
        this IOperationResult<TOperationData> operationResult,
        [NotNullWhen(true)] out GraphQlResult<TData>? result,
        [NotNullWhen(false)] out TOperationData? operationData)
        where TOperationData : class
        where TData : notnull
    {
        var errors = operationResult.Errors;
        if (errors.Count == 0)
        {
            result = null;
            operationData = AssertOperationData(operationResult);
            return false;
        }

        operationData = null;
        if (errors.Count == 1)
        {
            var error = errors[0];
            var unknownError = CreateUnknownError(error);
            result = new GraphQlResult<TData>(Helper.ToErrors(unknownError));
            return true;
        }

        var parsedErrors = new KeyValuePair<ErrorCode, IGraphQlError>[errors.Count];
        for (var i = 0; i < errors.Count; i++)
        {
            var error = errors[i];
            var unknownError = CreateUnknownError(error);
            parsedErrors[i] = new KeyValuePair<ErrorCode, IGraphQlError>(unknownError.Code, unknownError);
        }

        result = new GraphQlResult<TData>(parsedErrors);
        return true;
    }

    /// <summary>
    /// Transforms the operation result to a result.
    /// </summary>
    public static GraphQlResult<TOutput> Transform<TOperationData, TQueryData, TOutput>(
        this IOperationResult<TOperationData> operationResult,
        Func<TOperationData, TQueryData?> selectorFunc,
        Func<TQueryData, TOutput> transformerFunc)
        where TOperationData : class
        where TOutput : notnull
    {
        if (operationResult.TryExtractErrors(out GraphQlResult<TOutput>? resultWithErrors, out var operationData))
            return resultWithErrors;

        var queryData = selectorFunc(operationData);
        Debug.Assert(queryData is not null);
        if (queryData is null) throw new InvalidOperationException("Expected query data to be non-null");

        return transformerFunc(queryData);
    }

    /// <summary>
    /// Tries to extract all errors.
    /// </summary>
    public static bool TryExtractErrors<TOperationData, TData, TError1>(
        this IOperationResult<TOperationData> operationResult,
        [NotNullWhen(true)] out GraphQlResult<TData, TError1>? result,
        [NotNullWhen(false)] out TOperationData? operationData)
        where TOperationData : class
        where TData : notnull
        where TError1 : IGraphQlError<TError1>
    {
        var errors = operationResult.Errors;
        if (errors.Count == 0)
        {
            result = null;
            operationData = AssertOperationData(operationResult);
            return false;
        }

        operationData = null;
        if (errors.Count == 1)
        {
            var error = errors[0];

            if (TError1.Matches(error) && TError1.TryParse(error, out var error1))
            {
                result = new GraphQlResult<TData, TError1>(error1);
                return true;
            }

            var unknownError = CreateUnknownError(error);
            result = new GraphQlResult<TData, TError1>(Helper.ToErrors(unknownError));
            return true;
        }

        var parsedErrors = new KeyValuePair<ErrorCode, IGraphQlError>[errors.Count];
        for (var i = 0; i < errors.Count; i++)
        {
            var error = errors[i];
            if (TError1.Matches(error) && TError1.TryParse(error, out var error1))
            {
                parsedErrors[i] = new KeyValuePair<ErrorCode, IGraphQlError>(TError1.Code, error1);
            }
            else
            {
                var unknownError = CreateUnknownError(error);
                parsedErrors[i] = new KeyValuePair<ErrorCode, IGraphQlError>(unknownError.Code, unknownError);
            }
        }

        result = new GraphQlResult<TData, TError1>(parsedErrors);
        return true;
    }

    /// <summary>
    /// Transforms the operation result to a result.
    /// </summary>
    public static GraphQlResult<TOutput, TError1> Transform<TOperationData, TQueryData, TOutput, TError1>(
        this IOperationResult<TOperationData> operationResult,
        out GraphQlResult<TOutput, TError1> result,
        Func<TOperationData, TQueryData?> selectorFunc,
        Func<TQueryData, TOutput> transformerFunc)
        where TOperationData : class
        where TOutput : notnull
        where TError1 : IGraphQlError<TError1>
    {
        if (operationResult.TryExtractErrors(out GraphQlResult<TOutput, TError1>? resultWithErrors, out var operationData))
        {
            result = resultWithErrors;
            return result;
        }

        var queryData = selectorFunc(operationData);
        Debug.Assert(queryData is not null);
        if (queryData is null) throw new InvalidOperationException("Expected query data to be non-null");

        result = transformerFunc(queryData);
        return result;
    }

    /// <summary>
    /// Tries to extract all errors.
    /// </summary>
    public static bool TryExtractErrors<TOperationData, TData, TError1, TError2>(
        this IOperationResult<TOperationData> operationResult,
        [NotNullWhen(true)] out GraphQlResult<TData, TError1, TError2>? result,
        [NotNullWhen(false)] out TOperationData? operationData)
        where TOperationData : class
        where TData : notnull
        where TError1 : IGraphQlError<TError1>
        where TError2 : IGraphQlError<TError2>
    {
        var errors = operationResult.Errors;
        if (errors.Count == 0)
        {
            result = null;
            operationData = AssertOperationData(operationResult);
            return false;
        }

        operationData = null;
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

            var unknownError = CreateUnknownError(error);
            result = new GraphQlResult<TData, TError1, TError2>(Helper.ToErrors(unknownError));
            return true;
        }

        var parsedErrors = new KeyValuePair<ErrorCode, IGraphQlError>[errors.Count];
        for (var i = 0; i < errors.Count; i++)
        {
            var error = errors[i];
            if (TError1.Matches(error) && TError1.TryParse(error, out var error1))
            {
                parsedErrors[i] = new KeyValuePair<ErrorCode, IGraphQlError>(TError1.Code, error1);
            }
            else if (TError2.Matches(error) && TError2.TryParse(error, out var error2))
            {
                parsedErrors[i] = new KeyValuePair<ErrorCode, IGraphQlError>(TError2.Code, error2);
            }
            else
            {
                var unknownError = CreateUnknownError(error);
                parsedErrors[i] = new KeyValuePair<ErrorCode, IGraphQlError>(unknownError.Code, unknownError);
            }
        }

        result = new GraphQlResult<TData, TError1, TError2>(parsedErrors);
        return true;
    }

    /// <summary>
    /// Transforms the operation result to a result.
    /// </summary>
    public static GraphQlResult<TOutput, TError1, TError2> Transform<TOperationData, TQueryData, TOutput, TError1, TError2>(
        this IOperationResult<TOperationData> operationResult,
        out GraphQlResult<TOutput, TError1, TError2> result,
        Func<TOperationData, TQueryData?> selectorFunc,
        Func<TQueryData, TOutput> transformerFunc)
        where TOperationData : class
        where TOutput : notnull
        where TError1 : IGraphQlError<TError1>
        where TError2 : IGraphQlError<TError2>
    {
        if (operationResult.TryExtractErrors(out GraphQlResult<TOutput, TError1, TError2>? resultWithErrors, out var operationData))
        {
            result = resultWithErrors;
            return result;
        }

        var queryData = selectorFunc(operationData);
        Debug.Assert(queryData is not null);
        if (queryData is null) throw new InvalidOperationException("Expected query data to be non-null");

        result = transformerFunc(queryData);
        return result;
    }

    /// <summary>
    /// Tries to extract all errors.
    /// </summary>
    public static bool TryExtractErrors<TOperationData, TData, TError1, TError2, TError3>(
        this IOperationResult<TOperationData> operationResult,
        [NotNullWhen(true)] out GraphQlResult<TData, TError1, TError2, TError3>? result,
        [NotNullWhen(false)] out TOperationData? operationData)
        where TOperationData : class
        where TData : notnull
        where TError1 : IGraphQlError<TError1>
        where TError2 : IGraphQlError<TError2>
        where TError3 : IGraphQlError<TError3>
    {
        var errors = operationResult.Errors;
        if (errors.Count == 0)
        {
            result = null;
            operationData = AssertOperationData(operationResult);
            return false;
        }

        operationData = null;
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

            var unknownError = CreateUnknownError(error);
            result = new GraphQlResult<TData, TError1, TError2, TError3>(Helper.ToErrors(unknownError));
            return true;
        }

        var parsedErrors = new KeyValuePair<ErrorCode, IGraphQlError>[errors.Count];
        for (var i = 0; i < errors.Count; i++)
        {
            var error = errors[i];
            if (TError1.Matches(error) && TError1.TryParse(error, out var error1))
            {
                parsedErrors[i] = new KeyValuePair<ErrorCode, IGraphQlError>(TError1.Code, error1);
            }
            else if (TError2.Matches(error) && TError2.TryParse(error, out var error2))
            {
                parsedErrors[i] = new KeyValuePair<ErrorCode, IGraphQlError>(TError2.Code, error2);
            }
            else if (TError3.Matches(error) && TError3.TryParse(error, out var error3))
            {
                parsedErrors[i] = new KeyValuePair<ErrorCode, IGraphQlError>(TError3.Code, error3);
            }
            else
            {
                var unknownError = CreateUnknownError(error);
                parsedErrors[i] = new KeyValuePair<ErrorCode, IGraphQlError>(unknownError.Code, unknownError);
            }
        }

        result = new GraphQlResult<TData, TError1, TError2, TError3>(parsedErrors);
        return true;
    }

    /// <summary>
    /// Transforms the operation result to a result.
    /// </summary>
    public static GraphQlResult<TOutput, TError1, TError2, TError3> Transform<TOperationData, TQueryData, TOutput, TError1, TError2, TError3>(
        this IOperationResult<TOperationData> operationResult,
        out GraphQlResult<TOutput, TError1, TError2, TError3> result,
        Func<TOperationData, TQueryData?> selectorFunc,
        Func<TQueryData, TOutput> transformerFunc)
        where TOperationData : class
        where TOutput : notnull
        where TError1 : IGraphQlError<TError1>
        where TError2 : IGraphQlError<TError2>
        where TError3 : IGraphQlError<TError3>
    {
        if (operationResult.TryExtractErrors(out GraphQlResult<TOutput, TError1, TError2, TError3>? resultWithErrors, out var operationData))
        {
            result = resultWithErrors;
            return result;
        }

        var queryData = selectorFunc(operationData);
        Debug.Assert(queryData is not null);
        if (queryData is null) throw new InvalidOperationException("Expected query data to be non-null");

        result = transformerFunc(queryData);
        return result;
    }

    private static TOperationData AssertOperationData<TOperationData>(IOperationResult<TOperationData> operationResult)
        where TOperationData : class
    {
        var operationData = operationResult.Data;
        if (operationData is null) throw new InvalidOperationException("Expected result to contain data but found null");
        return operationData;
    }

    private static UnknownError CreateUnknownError(IClientError error)
    {
        // NOTE(erri120): If you landed here, that means some GraphQl call returned an error
        // that the app doesn't know about. Ideally, the error should be turned into a concrete
        // type if possible.
        if (Debugger.IsAttached) Debugger.Break();

        return new UnknownError
        {
            Code = error.Code is null ? UnknownError.DefaultCode : ErrorCode.From(error.Code),
            Message = error.Message,
            ClientError = error,
        };
    }
}
