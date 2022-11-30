using System.Buffers;
using System.Globalization;
using System.Text;

namespace NexusMods.Hashing.xxHash64;

public static class StringExtensions
{
    private static readonly char[] _hexLookup = "0123456789ABCDEF".ToArray();
    
    public static string ToHex(this ReadOnlySpan<char> bytes)
    {
        Span<char> outputBuf = stackalloc char[bytes.Length * 2];
        for (var x = 0; x < bytes.Length; x++)
        {
            outputBuf[x * 2] = _hexLookup[(bytes[x] >> 4)];
            outputBuf[(x * 2) + 1] = _hexLookup[bytes[x] & 0xF];
        }
        return new string(outputBuf);
    }
    
    public static string ToHex(this ReadOnlySpan<byte> bytes)
    {
        Span<char> outputBuf = stackalloc char[bytes.Length * 2];
        for (var x = 0; x < bytes.Length; x++)
        {
            outputBuf[x * 2] = _hexLookup[(bytes[x] >> 4)];
            outputBuf[(x * 2) + 1] = _hexLookup[bytes[x] & 0xF];
        }
        return new string(outputBuf);
    }
    
    public static void ToHex(this ReadOnlySpan<byte> bytes, Span<char> outputBuf)
    {
        for (var x = 0; x < bytes.Length; x++)
        {
            outputBuf[x * 2] = _hexLookup[(bytes[x] >> 4)];
            outputBuf[(x * 2) + 1] = _hexLookup[bytes[x] & 0xF];
        }
    }
    
    public static void FromHex(this string hex, Span<byte> bytes)
    {
        hex.AsSpan().FromHex(bytes);
    }

    public static void FromHex(this ReadOnlySpan<char> hex, Span<byte> bytes)
    {
        for (var i = 0; i < bytes.Length; i++)
        {
            var hexOffset = i * 2;
            bytes[i] = byte.Parse(hex[hexOffset..(hexOffset+2)], NumberStyles.HexNumber);
        }
    }
    
    /// <summary>
    /// Returns the xxHash64 of the given string
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static Hash XxHash64(this string s)
    {
        var bytes = Encoding.UTF8.GetByteCount(s);
        using var mem = MemoryPool<byte>.Shared.Rent(bytes);
        Encoding.UTF8.GetBytes(s, mem.Memory.Span[..bytes]);
        return ((ReadOnlySpan<byte>)mem.Memory.Span[..bytes]).XxHash64();
    }
}