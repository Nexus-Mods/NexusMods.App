using BitFaster.Caching;
using BitFaster.Caching.Lru;
using NexusMods.Abstractions.Resources;

namespace NexusMods.DataModel;

public class ScopedResourceCache<TResourceIdentifier, TKey, TData> : IResourceLoader<TResourceIdentifier, Lifetime<TData>>
    where TResourceIdentifier : notnull
    where TData : IDisposable
    where TKey : notnull
{
    private readonly Func<TResourceIdentifier, TKey> _keyGenerator;
    private readonly IResourceLoader<TResourceIdentifier, TData> _inner;
    private readonly IScopedAsyncCache<TKey, TData> _cache;

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

public class ResourceCache<TResourceIdentifier, TKey, TData> : IResourceLoader<TResourceIdentifier, TData>
    where TResourceIdentifier : notnull
    where TData : IDisposable
    where TKey : notnull
{
    private readonly Func<TResourceIdentifier, TKey> _keyGenerator;
    private readonly IResourceLoader<TResourceIdentifier, TData> _inner;
    private readonly IAsyncCache<TKey, TData> _cache;

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

    public static IResourceLoader<TResourceIdentifier, TData> UseCache<TResourceIdentifier, TKey, TData>(
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
            factory: static (input, inner) => new ResourceCache<TResourceIdentifier, TKey, TData>(
                keyGenerator: input.keyGenerator,
                keyComparer: input.keyComparer,
                capacityPartition: input.capacityPartition,
                inner: inner
            )
        );
    }
}
