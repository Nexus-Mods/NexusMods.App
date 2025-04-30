using System.Diagnostics.CodeAnalysis;
using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Extensions.BCL;

namespace NexusMods.Abstractions.Resources.Caching;

/// <summary>
/// In-memory store for pipeline results with optional period cleanup tasks.
/// </summary>
/// <remarks>
/// Prefer this over <see cref="ResourceCache{TResourceIdentifier,TKey,TData}"/> and
/// <see cref="ScopedResourceCache{TResourceIdentifier,TKey,TData}"/> if you just need
/// storage instead of a bounded cache that adds and removes items based on access patterns.
/// </remarks>
/// <seealso cref="ResourceCache{TResourceIdentifier,TKey,TData}"/>
/// <seealso cref="ScopedResourceCache{TResourceIdentifier,TKey,TData}"/>
[PublicAPI]
public sealed class InMemoryStore<TResourceIdentifierIn, TResourceIdentifierOut, TKey, TData> : IResourceLoader<TResourceIdentifierIn, TData>, IDisposable
    where TResourceIdentifierIn : notnull
    where TResourceIdentifierOut : notnull
    where TData : notnull
    where TKey : notnull
{
    [SuppressMessage("ReSharper", "StaticMemberInGenericType", Justification = "Doesn't matter for value types")]
    private static readonly TimeSpan DefaultDelay = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Predicate whether to delete the key.
    /// </summary>
    public delegate ValueTask<bool> ShouldDeleteKey((TKey, TResourceIdentifierIn) key, CancellationToken cancellationToken);

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
        ShouldDeleteKey? shouldDeleteKey,
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

        if (shouldDeleteKey is not null)
        {
            _cts = new CancellationTokenSource();
            _cleanupTask = Task.Run(() => CleanupTask(shouldDeleteKey, delay: deleteDelay.ValueOr(() => DefaultDelay), _cts.Token));
        }
    }

    private async Task CleanupTask(ShouldDeleteKey shouldDeleteKey, TimeSpan delay, CancellationToken cancellationToken)
    {
        var keysToDelete = new Queue<(TKey, TResourceIdentifierIn)>();

        while (!_isDisposed && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var _ = _dictionarySemaphore.WaitDisposable(cancellationToken: cancellationToken);

                foreach (var key in _dictionary.Keys)
                {
                    var shouldDelete = await shouldDeleteKey(key, cancellationToken);
                    if (shouldDelete) keysToDelete.Enqueue(key);
                }

                while (keysToDelete.TryDequeue(out var key))
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
    /// <summary>
    /// Store the results in-memory.
    /// </summary>
    public static IResourceLoader<TResourceIdentifier, TData> StoreInMemory<TResourceIdentifier, TKey, TData>(
        this IResourceLoader<TResourceIdentifier, TData> inner,
        Func<TResourceIdentifier, TKey> keySelector,
        IEqualityComparer<TKey> keyComparer,
        InMemoryStore<TResourceIdentifier, TResourceIdentifier, TKey, TData>.ShouldDeleteKey? shouldDeleteKey,
        Optional<TimeSpan> deleteDelay = default)
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
        InMemoryStore<TResourceIdentifier, TKey, TKey, TData>.ShouldDeleteKey? shouldDeleteKey,
        Optional<TimeSpan> deleteDelay = default)
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
        InMemoryStore<TResourceIdentifierIn, TResourceIdentifierOut, TKey, TData>.ShouldDeleteKey? shouldDeleteKey,
        Optional<TimeSpan> deleteDelay = default)
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
}
