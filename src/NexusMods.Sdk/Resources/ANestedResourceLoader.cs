using JetBrains.Annotations;

namespace NexusMods.Sdk.Resources;

/// <summary>
/// Nested resource loader.
/// </summary>
[PublicAPI]
public abstract class ANestedResourceLoader<TResourceIdentifier, TData, TInnerData> : IResourceLoader<TResourceIdentifier, TData>
    where TResourceIdentifier : notnull
    where TData : notnull
    where TInnerData : notnull
{
    private readonly IResourceLoader<TResourceIdentifier, TInnerData> _innerLoader;

    /// <summary>
    /// Constructor.
    /// </summary>
    protected ANestedResourceLoader(IResourceLoader<TResourceIdentifier, TInnerData> innerLoader)
    {
        _innerLoader = innerLoader;
    }

    /// <inheritdoc/>
    public async ValueTask<Resource<TData>> LoadResourceAsync(TResourceIdentifier resourceIdentifier, CancellationToken cancellationToken)
    {
        var resource = await _innerLoader.LoadResourceAsync(resourceIdentifier, cancellationToken);
        return await ProcessResourceAsync(resource, resourceIdentifier, cancellationToken);
    }

    /// <summary>
    /// Process the resource from the inner loader.
    /// </summary>
    protected abstract ValueTask<Resource<TData>> ProcessResourceAsync(Resource<TInnerData> resource, TResourceIdentifier resourceIdentifier, CancellationToken cancellationToken);
}
