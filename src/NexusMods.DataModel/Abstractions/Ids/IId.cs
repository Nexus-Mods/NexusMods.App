using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NexusMods.DataModel.Abstractions.Ids;

/// <summary>
/// Represents a unique identifier for an item stored in the datastore (database).
/// </summary>
[JsonConverter(typeof(IdJsonConverter))]
public interface Id
{
    /// <summary>
    /// Size of the span required to store this ID.
    /// </summary>
    public int SpanSize { get; }

    /// <summary>
    /// Serializes to a span.
    /// </summary>
    public void ToSpan(Span<byte> span);

    /// <summary/>
    public EntityCategory Category { get; }

    /// <summary>
    /// Converts the Span to a hex string.
    /// </summary>
    public string SpanHex
    {
        get
        {
            Span<byte> span = stackalloc byte[SpanSize];
            ToSpan(span);
            return Convert.ToHexString(span);
        }
    }

    /// <summary>
    /// Returns the tagged span as a hex string.
    /// </summary>
    public string TaggedSpanHex
    {
        get
        {
            Span<byte> span = stackalloc byte[SpanSize + 1];
            ToTaggedSpan(span);
            return Convert.ToHexString(span);
        }
    }

    /// <summary>
    /// Converts a tagged span back into an ID.
    /// </summary>
    /// <param name="span">The span to convert back.</param>
    public static Id FromTaggedSpan(ReadOnlySpan<byte> span)
    {
        var tag = (EntityCategory)span[0];

        switch (span.Length)
        {
            case 1:
                return IdEmpty.Empty;
            case 9:
                return new Id64(tag, BinaryPrimitives.ReadUInt64BigEndian(span[1..]));
            case 17:
                return new TwoId64(tag, BinaryPrimitives.ReadUInt64BigEndian(span[1..]),
                    BinaryPrimitives.ReadUInt64BigEndian(span[9..]));
        }

        var mem = new Memory<byte>(new byte[span.Length - 1]);
        span[1..].CopyTo(mem.Span);
        return new IdVariableLength(tag, mem);
    }

    /// <summary>
    /// Creates an ID from an existing span of bytes.
    /// </summary>
    /// <param name="category">The category associated with this span.</param>
    /// <param name="span">The bytes from which the ID is obtained back from.</param>
    /// <returns>ID converted back from the Span.</returns>
    public static Id FromSpan(EntityCategory category, ReadOnlySpan<byte> span)
    {
        switch (span.Length)
        {
            case 0:
                return IdEmpty.Empty;
            case 8:
                return new Id64(category, BinaryPrimitives.ReadUInt64BigEndian(span));
            case 16:
                return new TwoId64(category, BinaryPrimitives.ReadUInt64BigEndian(span), BinaryPrimitives.ReadUInt64BigEndian(span[8..]));
        }

        var mem = new Memory<byte>(new byte[span.Length]);
        span.CopyTo(mem.Span);
        return new IdVariableLength(category, mem);
    }

    /// <summary>
    /// Converts the current Span to a tagged span; which embeds the category
    /// of the item in the first byte of the span.
    /// </summary>
    /// <param name="span"></param>
    public void ToTaggedSpan(Span<byte> span)
    {
        span[0] = (byte)Category;
        ToSpan(span[1..]);
    }

    /// <summary>
    /// Returns true if the given ID is a prefix of the current ID.
    /// </summary>
    /// <param name="prefix">The prefix to text.</param>
    bool IsPrefixedBy(Id prefix)
    {
        if (prefix.SpanSize > SpanSize)
            return false;

        Span<byte> ourSpan = stackalloc byte[SpanSize + 1];
        ToTaggedSpan(ourSpan);
        Span<byte> prefixSpan = stackalloc byte[prefix.SpanSize + 1];
        prefix.ToTaggedSpan(prefixSpan);
        return ourSpan.StartsWith(prefixSpan);
    }

    /// <summary>
    /// Creates an ID from an existing span of bytes.
    /// </summary>
    /// <param name="category">The category associated with this span.</param>
    /// <param name="bytes">The bytes from which the ID is obtained back from.</param>
    /// <returns>ID converted back from the Span.</returns>
    static Id? FromSpan(RootType category, byte[] bytes)
    {
        return category switch
        {
            RootType.Loadouts => FromSpan(EntityCategory.Loadouts, bytes),
            RootType.Tests => FromSpan(EntityCategory.TestData, bytes),
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
        };
    }

    /// <summary>
    /// Creates an unique ID for the given category.
    /// </summary>
    /// <param name="category">The category to create the ID for.</param>
    static Id CreateUnique(EntityCategory category)
    {
        var guid = Guid.NewGuid();
        Span<byte> span = stackalloc byte[16];
        guid.TryWriteBytes(span);
        return new IdVariableLength(category, span.ToArray());
    }
}

/// <inheritdoc />
public class IdJsonConverter : JsonConverter<Id>
{
    /// <inheritdoc />
    public override Id Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString()!;
        var spanSize = (int)Math.Ceiling((double)str.Length / 4) * 3;
        Span<byte> span = stackalloc byte[spanSize];
        Convert.TryFromBase64String(str, span, out _);

        return Id.FromTaggedSpan(span);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Id value, JsonSerializerOptions options)
    {
        Span<byte> span = stackalloc byte[value.SpanSize + 1];
        value.ToTaggedSpan(span);
        writer.WriteBase64StringValue(span);
    }
}
