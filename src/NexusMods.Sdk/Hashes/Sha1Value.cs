using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Sdk.Hashes;

/// <summary>
/// Represents a SHA1 value.
/// </summary>
[PublicAPI]
[JsonConverter(typeof(Sha1JsonConverter))]
public readonly struct Sha1Value : IEquatable<Sha1Value>
{
    internal const int Size = 20;
    internal const int HexStringSize = 40;
    private readonly InlineArray _array;

    /// <summary>
    /// Gets the value as a span.
    /// </summary>
    public ReadOnlySpan<byte> AsSpan() => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<InlineArray, byte>(ref Unsafe.AsRef(in _array)), length: Size);

    private Sha1Value(InlineArray array)
    {
        _array = array;
    }

    /// <summary>
    /// Creates a new value from a span.
    /// </summary>
    public static Sha1Value From(ReadOnlySpan<byte> input)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(input.Length, Size, nameof(input));
        var slice = input[..Size];

        var array = new InlineArray();
        var span = array.AsSpan();
        slice.CopyTo(span);

        return new Sha1Value(array);
    }

    /// <summary>
    /// Creates a new value from a hex string.
    /// </summary>
    public static Sha1Value FromHex(ReadOnlySpan<char> input)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(input.Length, HexStringSize, nameof(input));
        var slice = input[..(Size * 2)];

        var array = new InlineArray();
        var span = array.AsSpan();

        var status = Convert.FromHexString(slice, span, out _, out _);
        if (status != OperationStatus.Done) throw new ArgumentException($"Failed to convert from hex: status={status},input=`{input.ToString()}`", nameof(input));

        return new Sha1Value(array);
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
    public bool Equals(Sha1Value other) => other.AsSpan().SequenceEqual(AsSpan());

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Sha1Value other && Equals(other);

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
    public static bool operator ==(Sha1Value left, Sha1Value right) => left.Equals(right);
    /// <summary/>
    public static bool operator !=(Sha1Value left, Sha1Value right) => !(left == right);
}

/// <summary>
/// Attribute for <see cref="Sha1Value"/>.
/// </summary>
public class Sha1Attribute(string ns, string name) : ScalarAttribute<Sha1Value, Memory<byte>, BlobSerializer>(ns, name)
{
    /// <inheritdoc/>
    protected override Memory<byte> ToLowLevel(Sha1Value value) => value.AsSpan().ToArray();

    /// <inheritdoc/>
    protected override Sha1Value FromLowLevel(Memory<byte> value, AttributeResolver resolver) => Sha1Value.From(value.Span);
}

internal class Sha1JsonConverter : JsonConverter<Sha1Value>
{
    public override Sha1Value Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var hex = reader.GetString();
        if (hex is null) throw new JsonException();

        return Sha1Value.FromHex(hex);
    }

    public override void Write(Utf8JsonWriter writer, Sha1Value value, JsonSerializerOptions options)
    {
        Span<char> hex = stackalloc char[Sha1Value.HexStringSize];

        var numWritten = value.ToHex(hex);
        Debug.Assert(numWritten == hex.Length, "entire span should be written to");

        writer.WriteStringValue(hex);
    }
}
