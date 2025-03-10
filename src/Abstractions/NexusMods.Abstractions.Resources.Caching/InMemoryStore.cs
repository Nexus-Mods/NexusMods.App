using DynamicData.Kernel;
using NexusMods.Extensions.BCL;

namespace NexusMods.Abstractions.Resources.Caching;

public sealed class InMemoryStore<TResourceIdentifierIn, TResourceIdentifierOut, TKey, TData> : IResourceLoader<TResourceIdentifierIn, TData>, IDisposable
    where TResourceIdentifierIn : notnull
    where TResourceIdentifierOut : notnull
    where TData : notnull
    where TKey : notnull
{
    private static readonly TimeSpan DefaultDelay = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Returns all keys to delete.
    /// </summary>
    public delegate ValueTask<(TKey, TResourceIdentifierIn)[]> GetKeysToDelete((TKey, TResourceIdentifierIn)[] keys, CancellationToken cancellationToken);

    private readonly Func<TResourceIdentifierIn, TKey> _keySelector;
    private readonly Func<TResourceIdentifierIn, TResourceIdentifierOut> _resourceIdentifierSelector;
    private readonly IResourceLoader<TResourceIdentifierOut, TData> _inner;

    private readonly Dictionary<(TKey, TResourceIdentifierIn), TData> _dictionary;
    private readonly SemaphoreSlim _dictionarySemaphore;

    private readonly Task? _cleanupTask;
    private readonly CancellationTokenSource? _cts;

    /// <summary>
    /// Constructor.
    /// </summary>
    public InMemoryStore(
        Func<TResourceIdentifierIn, TKey> keySelector,
        Func<TResourceIdentifierIn, TResourceIdentifierOut> resourceIdentifierSelector,
        IEqualityComparer<TKey> keyComparer,
        GetKeysToDelete? getKeysToDelete,
        Optional<TimeSpan> deleteDelay,
        IResourceLoader<TResourceIdentifierOut, TData> inner)
    {
        _keySelector = keySelector;
        _resourceIdentifierSelector = resourceIdentifierSelector;
        _inner = inner;

        _dictionary = new Dictionary<(TKey, TResourceIdentifierIn), TData>(EqualityComparer<(TKey, TResourceIdentifierIn)>.Create(
            equals: (a, b) => keyComparer.Equals(a.Item1, b.Item1),
            getHashCode: a => keyComparer.GetHashCode(a.Item1)
        ));

        _dictionarySemaphore = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        if (getKeysToDelete is not null)
        {
            _cts = new CancellationTokenSource();
            _cleanupTask = Task.Run(() => CleanupTask(getKeysToDelete, delay: deleteDelay.ValueOr(() => DefaultDelay), _cts.Token));
        }
    }

    private async Task CleanupTask(GetKeysToDelete getKeysToDelete, TimeSpan delay, CancellationToken cancellationToken)
    {
        while (!_isDisposed && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var _ = _dictionarySemaphore.WaitDisposable(cancellationToken: cancellationToken);

                var keysToDelete = await getKeysToDelete(_dictionary.Keys.ToArray(), cancellationToken);
                foreach (var key in keysToDelete)
                {
                    if (_dictionary.Remove(key, out var data) && data is IDisposable disposableData)
                    {
                        disposableData.Dispose();
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }

            try
            {
                await Task.Delay(delay, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Resource<TData>> LoadResourceAsync(TResourceIdentifierIn resourceIdentifier, CancellationToken cancellationToken)
    {
        var key = _keySelector(resourceIdentifier);
        var tuple = (key, resourceIdentifier);

        if (_dictionary.TryGetValue(tuple, out var data))
        {
            return new Resource<TData>
            {
                Data = data,
            };
        }

        var nestedResourceIdentifier = _resourceIdentifierSelector(resourceIdentifier);
        var resource = await _inner.LoadResourceAsync(nestedResourceIdentifier, cancellationToken);
        if (_dictionary.TryGetValue(tuple, out data))
        {
            return new Resource<TData>
            {
                Data = data,
            };
        }

        using var _ = _dictionarySemaphore.WaitDisposable(cancellationToken: cancellationToken);
        if (_dictionary.TryGetValue(tuple, out data))
        {
            return new Resource<TData>
            {
                Data = data,
            };
        }

        _dictionary.Add(tuple, resource.Data);
        return resource;
    }

    private bool _isDisposed;

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _cts?.Dispose();

        _dictionarySemaphore.Dispose();

        foreach (var kv in _dictionary)
        {
            var disposable = kv.Value as IDisposable;
            disposable?.Dispose();
        }

        _dictionary.Clear();
    }
}

public static partial class ExtensionsMethods
{
    public static IResourceLoader<TResourceIdentifier, TData> StoreInMemory<TResourceIdentifier, TKey, TData>(
        this IResourceLoader<TResourceIdentifier, TData> inner,
        Func<TResourceIdentifier, TKey> keySelector,
        IEqualityComparer<TKey> keyComparer,
        InMemoryStore<TResourceIdentifier, TResourceIdentifier, TKey, TData>.GetKeysToDelete? getKeysToDelete,
        Optional<TimeSpan> deleteDelay = default)
        where TResourceIdentifier : notnull
        where TData : notnull
        where TKey : notnull
    {
        return inner.Then(
            state: (keySelector, keyComparer, getKeysToDelete, deleteDelay),
            factory: static (input, inner) => new InMemoryStore<TResourceIdentifier, TResourceIdentifier, TKey, TData>(
                keySelector: input.keySelector,
                resourceIdentifierSelector: static x => x,
                keyComparer: input.keyComparer,
                getKeysToDelete: input.getKeysToDelete,
                deleteDelay: input.deleteDelay,
                inner: inner
            )
        );
    }

    public static IResourceLoader<TResourceIdentifier, TData> StoreInMemory<TResourceIdentifier, TKey, TData>(
        this IResourceLoader<TKey, TData> inner,
        Func<TResourceIdentifier, TKey> selector,
        IEqualityComparer<TKey> keyComparer,
        InMemoryStore<TResourceIdentifier, TKey, TKey, TData>.GetKeysToDelete? getKeysToDelete,
        Optional<TimeSpan> deleteDelay = default)
        where TResourceIdentifier : notnull
        where TData : notnull
        where TKey : notnull
    {
        return inner.Then(
            state: (selector, keyComparer, getKeysToDelete, deleteDelay),
            factory: static (input, inner) => new InMemoryStore<TResourceIdentifier, TKey, TKey, TData>(
                keySelector: input.selector,
                resourceIdentifierSelector: input.selector,
                keyComparer: input.keyComparer,
                getKeysToDelete: input.getKeysToDelete,
                deleteDelay: input.deleteDelay,
                inner: inner
            )
        );
    }

    public static IResourceLoader<TResourceIdentifierIn, TData> StoreInMemory<TResourceIdentifierIn, TResourceIdentifierOut, TKey, TData>(
        this IResourceLoader<TResourceIdentifierOut, TData> inner,
        Func<TResourceIdentifierIn, TKey> keySelector,
        Func<TResourceIdentifierIn, TResourceIdentifierOut> resourceIdentifierSelector,
        IEqualityComparer<TKey> keyComparer,
        InMemoryStore<TResourceIdentifierIn, TResourceIdentifierOut, TKey, TData>.GetKeysToDelete? getKeysToDelete,
        Optional<TimeSpan> deleteDelay = default)
        where TResourceIdentifierIn : notnull
        where TResourceIdentifierOut : notnull
        where TData : notnull
        where TKey : notnull
    {
        return inner.Then(
            state: (keySelector, resourceIdentifierSelector, keyComparer, getKeysToDelete, deleteDelay),
            factory: static (input, inner) => new InMemoryStore<TResourceIdentifierIn, TResourceIdentifierOut, TKey, TData>(
                keySelector: input.keySelector,
                resourceIdentifierSelector: input.resourceIdentifierSelector,
                keyComparer: input.keyComparer,
                getKeysToDelete: input.getKeysToDelete,
                deleteDelay: input.deleteDelay,
                inner: inner
            )
        );
    }
}
