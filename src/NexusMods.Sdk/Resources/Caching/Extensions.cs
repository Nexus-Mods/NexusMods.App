using BitFaster.Caching;
using BitFaster.Caching.Lru;

namespace NexusMods.Sdk.Resources;

public static partial class Extensions
{
        /// <summary>
    /// Store the results in-memory.
    /// </summary>
    public static IResourceLoader<TResourceIdentifier, TData> StoreInMemory<TResourceIdentifier, TKey, TData>(
        this IResourceLoader<TResourceIdentifier, TData> inner,
        Func<TResourceIdentifier, TKey> keySelector,
        IEqualityComparer<TKey> keyComparer,
        InMemoryStore<TResourceIdentifier, TResourceIdentifier, TKey, TData>.ShouldDeleteKey? shouldDeleteKey, DynamicData.Kernel.Optional<TimeSpan> deleteDelay = default)
        where TResourceIdentifier : notnull
        where TData : notnull
        where TKey : notnull
    {
        return inner.Then(
            state: (keySelector, keyComparer, shouldDeleteKey, deleteDelay),
            factory: static (input, inner) => new InMemoryStore<TResourceIdentifier, TResourceIdentifier, TKey, TData>(
                keySelector: input.keySelector,
                resourceIdentifierSelector: static x => x,
                keyComparer: input.keyComparer,
                shouldDeleteKey: input.shouldDeleteKey,
                deleteDelay: input.deleteDelay,
                inner: inner
            )
        );
    }

    /// <summary>
    /// Store the results in-memory.
    /// </summary>
    public static IResourceLoader<TResourceIdentifier, TData> StoreInMemory<TResourceIdentifier, TKey, TData>(
        this IResourceLoader<TKey, TData> inner,
        Func<TResourceIdentifier, TKey> selector,
        IEqualityComparer<TKey> keyComparer,
        InMemoryStore<TResourceIdentifier, TKey, TKey, TData>.ShouldDeleteKey? shouldDeleteKey, DynamicData.Kernel.Optional<TimeSpan> deleteDelay = default)
        where TResourceIdentifier : notnull
        where TData : notnull
        where TKey : notnull
    {
        return inner.Then(
            state: (selector, keyComparer, shouldDeleteKey, deleteDelay),
            factory: static (input, inner) => new InMemoryStore<TResourceIdentifier, TKey, TKey, TData>(
                keySelector: input.selector,
                resourceIdentifierSelector: input.selector,
                keyComparer: input.keyComparer,
                shouldDeleteKey: input.shouldDeleteKey,
                deleteDelay: input.deleteDelay,
                inner: inner
            )
        );
    }

    /// <summary>
    /// Store the results in-memory.
    /// </summary>
    public static IResourceLoader<TResourceIdentifierIn, TData> StoreInMemory<TResourceIdentifierIn, TResourceIdentifierOut, TKey, TData>(
        this IResourceLoader<TResourceIdentifierOut, TData> inner,
        Func<TResourceIdentifierIn, TKey> keySelector,
        Func<TResourceIdentifierIn, TResourceIdentifierOut> resourceIdentifierSelector,
        IEqualityComparer<TKey> keyComparer,
        InMemoryStore<TResourceIdentifierIn, TResourceIdentifierOut, TKey, TData>.ShouldDeleteKey? shouldDeleteKey, DynamicData.Kernel.Optional<TimeSpan> deleteDelay = default)
        where TResourceIdentifierIn : notnull
        where TResourceIdentifierOut : notnull
        where TData : notnull
        where TKey : notnull
    {
        return inner.Then(
            state: (keySelector, resourceIdentifierSelector, keyComparer, shouldDeleteKey, deleteDelay),
            factory: static (input, inner) => new InMemoryStore<TResourceIdentifierIn, TResourceIdentifierOut, TKey, TData>(
                keySelector: input.keySelector,
                resourceIdentifierSelector: input.resourceIdentifierSelector,
                keyComparer: input.keyComparer,
                shouldDeleteKey: input.shouldDeleteKey,
                deleteDelay: input.deleteDelay,
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
