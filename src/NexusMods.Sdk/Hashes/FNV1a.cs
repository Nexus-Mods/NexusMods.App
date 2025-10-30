using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace NexusMods.Sdk.Hashes;

/// <summary>
/// Hasher using 16-bit FNV1a algorithm.
/// </summary>
[PublicAPI]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class FNV1a16Hasher : IHasher<ushort, FNV1a16Hasher>
{
    public static ushort Hash(ReadOnlySpan<byte> input) => MixToShort(FNV1a32Hasher.Hash(input));
    public static ushort Hash(ReadOnlySpan<char> input) => MixToShort(FNV1a32Hasher.Hash(input));

    private static ushort MixToShort(uint hash)
    {
        return (ushort) ((hash >> 16) ^ (hash & 0xFFFF));
    }
}

/// <summary>
/// Hasher using 32-bit FNV1a algorithm.
/// </summary>
[PublicAPI]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class FNV1a32Hasher : IHasher<uint, FNV1a32Hasher>
{
    // https://datatracker.ietf.org/doc/html/draft-eastlake-fnv-35#name-fnv-constants
    private const uint Prime = 0x01000193;
    private const uint Offset = 0x811C9DC5;

    public static uint Hash(ReadOnlySpan<byte> input)
    {
        var hash = Offset;
        for (var i = 0; i < input.Length; i++)
        {
            var current = input[i];
            hash ^= current;
            hash *= Prime;
        }

        return hash;
    }

    public static uint Hash(ReadOnlySpan<char> input)
    {
        var hash = Offset;
        for (var i = 0; i < input.Length; i++)
        {
            var current = input[i];
            hash ^= current;
            hash *= Prime;
        }

        return hash;
    }
}

/// <summary>
/// Hasher using 64-bit FNV1a algorithm.
/// </summary>
[PublicAPI]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class FNV1a64Hasher : IHasher<ulong, FNV1a64Hasher>
{
    // https://datatracker.ietf.org/doc/html/draft-eastlake-fnv-35#name-fnv-constants
    private const ulong Prime = 0x00000100_000001B3;
    private const ulong Offset = 0xCBF29CE4_84222325;

    public static ulong Hash(ReadOnlySpan<byte> input)
    {
        var hash = Offset;
        for (var i = 0; i < input.Length; i++)
        {
            var current = input[i];
            hash ^= current;
            hash *= Prime;
        }

        return hash;
    }

    public static ulong Hash(ReadOnlySpan<char> input)
    {
        var hash = Offset;
        for (var i = 0; i < input.Length; i++)
        {
            var current = input[i];
            hash ^= current;
            hash *= Prime;
        }

        return hash;
    }
}
