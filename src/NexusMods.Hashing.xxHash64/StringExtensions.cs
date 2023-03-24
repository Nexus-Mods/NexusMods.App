using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace NexusMods.Hashing.xxHash64;

/// <summary>
/// Hashing related extensions for strings that might come in handy down the road.
/// </summary>
public static class StringExtensions
{
    private static readonly char[] HexLookup = "0123456789ABCDEF".ToArray();

    // TODO: I can elide bounds checks here, let me do it later - Sew https://github.com/Nexus-Mods/NexusMods.App/issues/214

    /// <summary>
    /// Converts the given bytes to a hexadecimal string.
    /// </summary>
    /// <param name="bytes">The bytes to convert.</param>
    /// <returns>The string in question.</returns>
    public static string ToHex(this ReadOnlySpan<byte> bytes)
    {
        Span<char> outputBuf = stackalloc char[bytes.Length * 2];
        ToHex(bytes, outputBuf);
        return new string(outputBuf);
    }

    /// <summary>
    /// Converts the given bytes to a hexadecimal string.
    /// </summary>
    /// <param name="bytes">The bytes to convert.</param>
    /// <param name="outputBuf">The buffer where the data should be output.</param>
    /// <returns>The string in question.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ToHex(this ReadOnlySpan<byte> bytes, Span<char> outputBuf)
    {
        for (var x = 0; x < bytes.Length; x++)
        {
            outputBuf[x * 2] = HexLookup[(bytes[x] >> 4)];
            outputBuf[(x * 2) + 1] = HexLookup[bytes[x] & 0xF];
        }
    }

    /// <summary>
    /// Converts a hex string back to its corresponding bytes.
    /// </summary>
    /// <param name="hex">The hex string itself.</param>
    /// <param name="bytes">The bytes for the hex string.</param>
    public static void FromHex(this string hex, Span<byte> bytes)
    {
        hex.AsSpan().FromHex(bytes);
    }

    /// <summary>
    /// Converts a hex string span of characters back to its corresponding bytes.
    /// </summary>
    /// <param name="hex">The hex string itself.</param>
    /// <param name="bytes">The bytes for the hex string.</param>
    public static void FromHex(this ReadOnlySpan<char> hex, Span<byte> bytes)
    {
        // TODO: Speed this up. The BCL's version is slow because it has to account for many possible format; while ours is made by us and should be clear. https://github.com/Nexus-Mods/NexusMods.App/issues/214
        for (var i = 0; i < bytes.Length; i++)
        {
            var hexOffset = i * 2;
            bytes[i] = byte.Parse(hex[hexOffset..(hexOffset + 2)], NumberStyles.HexNumber);
        }
    }

    /// <summary>
    /// Returns the xxHash64 of the given string using UTF8 encoding.
    /// </summary>
    /// <param name="text">The string to hash.</param>
    /// <returns>Hash of the given string.</returns>
    public static Hash XxHash64AsUtf8(this string text)
    {
        // This is only used in tests right now.
        var utf8 = Encoding.UTF8;
        var bytes = utf8.GetByteCount(text);
        using var mem = MemoryPool<byte>.Shared.Rent(bytes);
        var dataSpan = mem.Memory.Span[..bytes];
        utf8.GetBytes(text, dataSpan);
        return dataSpan.XxHash64();
    }
}
