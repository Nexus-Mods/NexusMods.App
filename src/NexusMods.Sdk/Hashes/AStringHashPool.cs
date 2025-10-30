using System.Collections.Immutable;
using System.Numerics;
using JetBrains.Annotations;

namespace NexusMods.Sdk.Hashes;

/// <summary>
/// Thread-safe string hash pool.
/// </summary>
[PublicAPI]
public abstract class AStringHashPool<TNumber> where TNumber : unmanaged, IBinaryInteger<TNumber>, IUnsignedNumber<TNumber>
{
    private const uint MaxIterations = 100;

    private readonly string _name;
    private ImmutableDictionary<TNumber, string> _cache;

    /// <summary>
    /// Constructor.
    /// </summary>
    protected AStringHashPool(string name)
    {
        _name = name;
        _cache = ImmutableDictionary<TNumber, string>.Empty;
    }

    /// <summary>
    /// Computes the hash for the input.
    /// </summary>
    protected abstract TNumber Hash(string input);

    /// <summary>
    /// Hashes the input and adds the result to the pool.
    /// </summary>
    /// <exception cref="HashCollisionException">Thrown when a hash collision is detected.</exception>
    /// <exception cref="BoundedAtomicUpdateException">Thrown when the atomic update failed within the provided bounds</exception>
    public TNumber GetOrAdd(string input)
    {
        var hash = Hash(input);

        if (_cache.TryGetValue(hash, out var existing))
        {
            if (!StringComparer.Ordinal.Equals(input, existing))
                throw new HashCollisionException($"Hash collision detected in pool '{_name}' for {hash:X} between '{input}' and '{existing}'");
            return hash;
        }

        uint iteration = 0;
        ImmutableDictionary<TNumber, string> currentCache, updatedCache;

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
    public bool TryGet(TNumber hash, out string? value) => _cache.TryGetValue(hash, out value);

    /// <summary>
    /// Reverse lookup of the value based on the hash.
    /// </summary>
    public string this[TNumber hash] => _cache[hash];

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
