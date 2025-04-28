using BitFaster.Caching;
using BitFaster.Caching.Lru;
using JetBrains.Annotations;

namespace NexusMods.Abstractions.Resources.Caching;

/// <summary>
/// Cache for pipeline results.
/// </summary>
/// <seealso cref="InMemoryStore{TResourceIdentifierIn,TResourceIdentifierOut,TKey,TData}"/>
/// <seealso cref="ScopedResourceCache{TResourceIdentifier,TKey,TData}"/>
[PublicAPI]
public sealed class ResourceCache<TResourceIdentifier, TKey, TData> : IResourceLoader<TResourceIdentifier, TData>
    where TResourceIdentifier : notnull
    where TData : notnull
    where TKey : notnull
{
    private readonly Func<TResourceIdentifier, TKey> _keyGenerator;
    private readonly IResourceLoader<TResourceIdentifier, TData> _inner;
    private readonly IAsyncCache<TKey, TData> _cache;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ResourceCache(
        Func<TResourceIdentifier, TKey> keyGenerator,
        IEqualityComparer<TKey> keyComparer,
        ICapacityPartition capacityPartition,
        IResourceLoader<TResourceIdentifier, TData> inner)
    {
        _keyGenerator = keyGenerator;
        _inner = inner;

        _cache = new ConcurrentLruBuilder<TKey, TData>()
            .WithKeyComparer(keyComparer)
            .WithCapacity(capacityPartition)
            .AsAsyncCache()
            .Build();
    }

    /// <inheritdoc/>
    public async ValueTask<Resource<TData>> LoadResourceAsync(TResourceIdentifier resourceIdentifier, CancellationToken cancellationToken)
    {
        var key = _keyGenerator(resourceIdentifier);
        var lifetime = await _cache.GetOrAddAsync(key, static (key, state) => AddAsync(state.Item1, state.Item2, state.Item3), (this, resourceIdentifier, cancellationToken));

        return new Resource<TData>
        {
            Data = lifetime,
        };
    }

    private static async Task<TData> AddAsync(
        ResourceCache<TResourceIdentifier, TKey, TData> self,
        TResourceIdentifier resourceIdentifier,
        CancellationToken cancellationToken)
    {
        var resource = await self._inner.LoadResourceAsync(resourceIdentifier, cancellationToken);
        return resource.Data;
    }
}

public static partial class ExtensionsMethods
{
    public static IResourceLoader<TResourceIdentifier, TData> UseCache<TResourceIdentifier, TKey, TData>(
        this IResourceLoader<TResourceIdentifier, TData> inner,
        Func<TResourceIdentifier, TKey> keyGenerator,
        IEqualityComparer<TKey> keyComparer,
        ICapacityPartition capacityPartition)
        where TResourceIdentifier : notnull
        where TData : notnull
        where TKey : notnull
    {
        return inner.Then(
            state: (keyGenerator, keyComparer, capacityPartition),
            factory: static (input, inner) => new ResourceCache<TResourceIdentifier, TKey, TData>(
                keyGenerator: input.keyGenerator,
                keyComparer: input.keyComparer,
                capacityPartition: input.capacityPartition,
                inner: inner
            )
        );
    }
}
