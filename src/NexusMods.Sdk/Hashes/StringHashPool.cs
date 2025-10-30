using System.Collections.Immutable;
using JetBrains.Annotations;

namespace NexusMods.Sdk.Hashes;

/// <summary>
/// Thread-safe string hash pool using provided hasher.
/// </summary>
[PublicAPI]
public class StringHashPool<THash, THasher>
    where THash : unmanaged, IEquatable<THash>
    where THasher : IHasher<THash, THasher>
{
    private const uint MaxIterations = 100;

    private readonly string _name;
    private ImmutableDictionary<THash, string> _cache;

    /// <summary>
    /// Constructor.
    /// </summary>
    public StringHashPool(string name)
    {
        _name = name;
        _cache = ImmutableDictionary<THash, string>.Empty;
    }

    /// <summary>
    /// Hashes the input and adds the result to the pool.
    /// </summary>
    /// <exception cref="HashCollisionException">Thrown when a hash collision is detected.</exception>
    /// <exception cref="BoundedAtomicUpdateException">Thrown when the atomic update failed within the provided bounds</exception>
    public THash GetOrAdd(string input)
    {
        var hash = THasher.Hash(input);

        if (_cache.TryGetValue(hash, out var existing))
        {
            if (!StringComparer.Ordinal.Equals(input, existing))
                throw new HashCollisionException($"Hash collision detected in pool '{_name}' for {hash} between '{input}' and '{existing}'");
            return hash;
        }

        uint iteration = 0;
        ImmutableDictionary<THash, string> currentCache, updatedCache;

        do
        {
            currentCache = _cache;
            updatedCache = currentCache.Add(hash, input);

            if (++iteration >= MaxIterations) throw new BoundedAtomicUpdateException($"Failed to atomically update the cache of pool '{_name}' within {MaxIterations} iterations");
        } while (!ReferenceEquals(currentCache, Interlocked.CompareExchange(ref _cache, updatedCache, currentCache)));

        return hash;
    }

    /// <summary>
    /// Reverse lookup of the value based on the hash.
    /// </summary>
    public bool TryGet(THash hash, out string? value) => _cache.TryGetValue(hash, out value);

    /// <summary>
    /// Reverse lookup of the value based on the hash.
    /// </summary>
    public string this[THash hash] => _cache[hash];

    /// <inheritdoc/>
    public override string ToString() => $"Name = {_name}, Count = {_cache.Count}";
}

[PublicAPI]
public class HashCollisionException : Exception
{
    public HashCollisionException(string message) : base(message) { }
}

[PublicAPI]
public class BoundedAtomicUpdateException : Exception
{
    public BoundedAtomicUpdateException(string message) : base(message) { }
}
