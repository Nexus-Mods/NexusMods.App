using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace NexusMods.Sdk.Hashes;

/// <summary>
/// Represents a hash algorithm.
/// </summary>
[PublicAPI]
public interface IHasher<out THash, TSelf>
    where THash : unmanaged, IEquatable<THash>
    where TSelf : IHasher<THash, TSelf>
{
    /// <summary>
    /// Hashes the input.
    /// </summary>
    static abstract THash Hash(ReadOnlySpan<byte> input);

    /// <summary>
    /// Hashes the input.
    /// </summary>
    static virtual THash Hash(ReadOnlySpan<char> input) => TSelf.Hash(MemoryMarshal.AsBytes(input));
}

[PublicAPI]
public static class Hasher<THash, THasher>
    where THash : unmanaged, IEquatable<THash>
    where THasher : IHasher<THash, THasher>
{
    /// <summary>
    /// Hashes the input.
    /// </summary>
    public static THash Hash(ReadOnlySpan<char> input) => THasher.Hash(input);
}
