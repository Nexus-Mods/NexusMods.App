using BitFaster.Caching;
using BitFaster.Caching.Lru;
using JetBrains.Annotations;

namespace NexusMods.Abstractions.Resources.Caching;

/// <summary>
/// Cache for scoped pipeline results.
/// </summary>
/// <remarks>
/// Prefer this over <see cref="ResourceCache{TResourceIdentifier,TKey,TData}"/> if <typeparamref name="TData"/>
/// implements <see cref="IDisposable"/>. See the docs for more details: https://github.com/bitfaster/BitFaster.Caching/wiki/IDisposable-and-Scoped-values#scoped
/// </remarks>
/// <seealso cref="InMemoryStore{TResourceIdentifierIn,TResourceIdentifierOut,TKey,TData}"/>
/// <seealso cref="ResourceCache{TResourceIdentifier,TKey,TData}"/>
[PublicAPI]
public sealed class ScopedResourceCache<TResourceIdentifier, TKey, TData> : IResourceLoader<TResourceIdentifier, Lifetime<TData>>
    where TResourceIdentifier : notnull
    where TData : IDisposable
    where TKey : notnull
{
    private readonly Func<TResourceIdentifier, TKey> _keyGenerator;
    private readonly IResourceLoader<TResourceIdentifier, TData> _inner;
    private readonly IScopedAsyncCache<TKey, TData> _cache;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ScopedResourceCache(
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
            .AsScopedCache()
            .Build();
    }

    public async ValueTask<Resource<Lifetime<TData>>> LoadResourceAsync(TResourceIdentifier resourceIdentifier, CancellationToken cancellationToken)
    {
        var key = _keyGenerator(resourceIdentifier);
        var lifetime = await _cache.ScopedGetOrAddAsync(key, static (key, state) => AddAsync(state.Item1, state.Item2, state.Item3), (this, resourceIdentifier, cancellationToken));

        return new Resource<Lifetime<TData>>
        {
            Data = lifetime,
        };
    }

    private static async Task<Scoped<TData>> AddAsync(
        ScopedResourceCache<TResourceIdentifier, TKey, TData> self,
        TResourceIdentifier resourceIdentifier,
        CancellationToken cancellationToken)
    {
        var resource = await self._inner.LoadResourceAsync(resourceIdentifier, cancellationToken);
        return new Scoped<TData>(resource.Data);
    }
}

[PublicAPI]
public static partial class ExtensionsMethods
{
    public static IResourceLoader<TResourceIdentifier, Lifetime<TData>> UseScopedCache<TResourceIdentifier, TKey, TData>(
        this IResourceLoader<TResourceIdentifier, TData> inner,
        Func<TResourceIdentifier, TKey> keyGenerator,
        IEqualityComparer<TKey> keyComparer,
        ICapacityPartition capacityPartition)
        where TResourceIdentifier : notnull
        where TData : IDisposable
        where TKey : notnull
    {
        return inner.Then(
            state: (keyGenerator, keyComparer, capacityPartition),
            factory: static (input, inner) => new ScopedResourceCache<TResourceIdentifier, TKey, TData>(
                keyGenerator: input.keyGenerator,
                keyComparer: input.keyComparer,
                capacityPartition: input.capacityPartition,
                inner: inner
            )
        );
    }
}
