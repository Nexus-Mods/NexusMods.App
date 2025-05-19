using JetBrains.Annotations;

namespace NexusMods.Sdk.Resources;

/// <summary>
/// Changes the identifier.
/// </summary>
[PublicAPI]
public sealed class ChangeIdentifier<TOldResourceIdentifier, TNewResourceIdentifier, TData> : IResourceLoader<TOldResourceIdentifier, TData>
    where TData : notnull
    where TOldResourceIdentifier : notnull
    where TNewResourceIdentifier : notnull
{
    private readonly Func<TOldResourceIdentifier, TNewResourceIdentifier> _transform;
    private readonly IResourceLoader<TNewResourceIdentifier, TData> _inner;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ChangeIdentifier(
        Func<TOldResourceIdentifier, TNewResourceIdentifier> transform,
        IResourceLoader<TNewResourceIdentifier, TData> inner)
    {
        _transform = transform;
        _inner = inner;
    }

    /// <inheritdoc/>
    public ValueTask<Resource<TData>> LoadResourceAsync(TOldResourceIdentifier resourceIdentifier, CancellationToken cancellationToken)
    {
        var newIdentifier = _transform(resourceIdentifier);
        return _inner.LoadResourceAsync(newIdentifier, cancellationToken);
    }
}

public static partial class Extensions
{
    /// <summary>
    /// Change the identifier.
    /// </summary>
    public static IResourceLoader<TOldResourceIdentifier, TData> ChangeIdentifier<TOldResourceIdentifier, TNewResourceIdentifier, TData>(
        this IResourceLoader<TNewResourceIdentifier, TData> inner,
        Func<TOldResourceIdentifier, TNewResourceIdentifier> transform)
        where TData : notnull
        where TOldResourceIdentifier : notnull
        where TNewResourceIdentifier : notnull
    {
        return inner.Then(
            state: transform,
            factory: static (input, inner) => new ChangeIdentifier<TOldResourceIdentifier,TNewResourceIdentifier,TData>(
                transform: input,
                inner: inner
            )
        );
    }
}
