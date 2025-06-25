using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace NexusMods.Sdk.Hashes;

/// <summary>
/// Represents a MD5 value.
/// </summary>
[PublicAPI]
[JsonConverter(typeof(Md5JsonConverter))]
public readonly struct Md5Value : IEquatable<Md5Value>
{
    internal const int Size = 16;
    internal const int HexStringSize = 32;
    private readonly InlineArray _array;

    /// <summary>
    /// Gets the value as a span.
    /// </summary>
    public ReadOnlySpan<byte> AsSpan() => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<InlineArray, byte>(ref Unsafe.AsRef(in _array)), length: Size);

    /// <summary>
    /// Gets the value as a <see cref="UInt128"/>.
    /// </summary>
    /// <returns></returns>
    public UInt128 AsUInt128() => MemoryMarshal.Read<UInt128>(AsSpan());

    private Md5Value(InlineArray array)
    {
        _array = array;
    }

    /// <summary>
    /// Creates a new value from a span.
    /// </summary>
    public static Md5Value From(ReadOnlySpan<byte> input)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(input.Length, Size, nameof(input));
        var slice = input[..Size];
    
        var array = new InlineArray();
        var span = array.AsSpan();
        slice.CopyTo(span);
    
        return new Md5Value(array);
    }

    /// <summary>
    /// Creates a new value from a <see cref="UInt128"/>.
    /// </summary>
    public static Md5Value From(UInt128 input)
    {
        var array = new InlineArray();
        var span = array.AsSpan();
        MemoryMarshal.Write(span, input);

        return new Md5Value(array);
    }
    
    /// <summary>
    /// Creates a new value from a hex string.
    /// </summary>
    public static Md5Value FromHex(ReadOnlySpan<char> input)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(input.Length, HexStringSize, nameof(input));
        var slice = input[..(Size * 2)];
    
        var array = new InlineArray();
        var span = array.AsSpan();
    
        var status = Convert.FromHexString(slice, span, out _, out _);
        if (status != OperationStatus.Done) throw new ArgumentException($"Failed to convert from hex: status={status},input=`{input.ToString()}`", nameof(input));
    
        return new Md5Value(array);
    }
    
    /// <summary>
    /// Writes the value as a hex string to <paramref name="destination"/>.
    /// </summary>
    public int ToHex(Span<char> destination)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(destination.Length, HexStringSize, nameof(destination));
    
        var success = Convert.TryToHexString(AsSpan(), destination, out var charsWritten);
        Debug.Assert(success, "should always be true after destination length check");
    
        return charsWritten;
    }

    /// <inheritdoc/>
    public bool Equals(Md5Value other) => other.AsSpan().SequenceEqual(AsSpan());

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Md5Value other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => MemoryMarshal.Read<int>(AsSpan());

    /// <inheritdoc/>
    public override string ToString() => Convert.ToHexString(AsSpan());

    [InlineArray(length: Size)]
    private struct InlineArray
    {
        private byte _value;

        public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref Unsafe.As<InlineArray, byte>(ref this), length: Size);
    }

    /// <summary/>
    public static bool operator ==(Md5Value left, Md5Value right) => left.Equals(right);
    /// <summary/>
    public static bool operator !=(Md5Value left, Md5Value right) => !(left == right);
}
