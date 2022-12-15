using System.Buffers.Binary;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.Hashing.xxHash64;

namespace NexusMods.DataModel.Abstractions;

[JsonConverter(typeof(IdJsonConverter))]
public interface Id : IEquatable<Id>
{
    public int SpanSize { get; }
    public void ToSpan(Span<byte> span);
    
    public EntityCategory Category { get; }
    public string SpanHex 
    {
        get
        {
            Span<byte> span = stackalloc byte[SpanSize];
            ToSpan(span);
            return Convert.ToHexString(span);
        }
    }

    public static Id FromTaggedSpan(ReadOnlySpan<byte> span)
    {
        var tag = (EntityCategory)span[0];
        
        switch (span.Length)
        {
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
    
    public static Id FromSpan(EntityCategory category, ReadOnlySpan<byte> span)
    {
        if (span.Length == 8)
        {
            return new Id64(category, BinaryPrimitives.ReadUInt64BigEndian(span));
        }
        if (span.Length == 16)
        {
            return new TwoId64(category, BinaryPrimitives.ReadUInt64BigEndian(span), BinaryPrimitives.ReadUInt64BigEndian(span[8..]));
        }

        var mem = new Memory<byte>(new byte[span.Length - 1]);
        span.CopyTo(mem.Span);
        return new IdVariableLength(category, mem);
    }

    public void ToTaggedSpan(Span<byte> span)
    {
        span[0] = (byte)Category;
        ToSpan(span[1..]);
    }

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
}

public abstract class AId : Id
{
    public abstract bool Equals(Id? other);

    public override string ToString()
    {
        Span<byte> span = stackalloc byte[SpanSize];
        ToSpan(span);
        return $"{Category}-{Convert.ToHexString(span)}";
    }

    public abstract int SpanSize { get; }
    public abstract void ToSpan(Span<byte> span);
    public abstract EntityCategory Category { get; }

    public override int GetHashCode()
    {
        Span<byte> span = stackalloc byte[SpanSize];
        ToSpan(span);
        var hash = new HashCode();
        hash.AddBytes(span);
        return hash.ToHashCode();
    }

    public override bool Equals(object? obj)
    {
        if (obj is Id id)
            return Equals(id);
        return false;
    }
}

public class RootId : AId
{
    private readonly RootType _type;
    public RootId(RootType type)
    {
        _type = type;
    }
    
    public override bool Equals(Id? other)
    {
        if (other is RootId id)
            return id._type == _type;
        return false;
    }

    public override int SpanSize => 1;
    public override void ToSpan(Span<byte> span)
    {
        span[0] = (byte)_type;
    }

    public override EntityCategory Category => EntityCategory.Roots;
}

public class TwoId64 : AId
{
    private readonly EntityCategory _type;
    private readonly ulong _a;
    private readonly ulong _b;

    public TwoId64(EntityCategory type, ulong a, ulong b)
    {
        _type = type;
        _a = a;
        _b = b;
    }
    
    public override bool Equals(Id? other)
    {
        if (other is TwoId64 id)
            return id._a == _a && id._b == _b && id._type == _type;
        return false;
    }

    public override int SpanSize => 16;
    public override void ToSpan(Span<byte> span)
    {
        BinaryPrimitives.WriteUInt64BigEndian(span, _a);
        BinaryPrimitives.WriteUInt64BigEndian(span[8..], _b);
    }

    public override EntityCategory Category => _type;
}

public class IdEmpty : Id
{
    public EntityCategory Category => 0;
    
    public bool Equals(Id? other)
    {
        return other is IdEmpty;
    }
    public int SpanSize => 0;
    public static Id Empty = new IdEmpty();

    public void ToSpan(Span<byte> span)
    {
    }
}

public class IdVariableLength : AId
{
    private readonly ReadOnlyMemory<byte> _data;
    private readonly EntityCategory _category;

    public IdVariableLength(EntityCategory category, ReadOnlyMemory<byte> data)
    {
        _category = category;
        _data = data;
    }
    public override bool Equals(Id? other)
    {
        if (other == null || other.SpanSize == _data.Length) return false;
        Span<byte> buff = stackalloc byte[_data.Span.Length];
        other.ToSpan(buff);
        return _data.Span.SequenceEqual(buff);
    }

    public override int SpanSize => _data.Length;

    public override void ToSpan(Span<byte> span)
    {
        _data.Span.CopyTo(span);
    }

    public override EntityCategory Category => _category;
}

public class Id64 : AId
{
    private readonly EntityCategory _category;
    private readonly ulong _id;

    public Id64(EntityCategory category, ulong id)
    {
        _id = id;
        _category = category;
    }

    public override bool Equals(Id? other)
    {
        if (other is not { SpanSize: 8 }) return false;
        Span<byte> buff = stackalloc byte[8];
        other.ToSpan(buff);
        return BinaryPrimitives.ReadUInt64BigEndian(buff) == _id;
    }

    public override int SpanSize => 8;
    public override void ToSpan(Span<byte> span)
    {
        BinaryPrimitives.WriteUInt64BigEndian(span, _id);
    }

    public override EntityCategory Category => _category;
}

public class IdJsonConverter : JsonConverter<Id>
{ 
    public override Id Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString()!;
        var spanSize = (int)Math.Ceiling((double)str.Length / 4) * 3;
        Span<byte> span = stackalloc byte[spanSize]; 
        Convert.TryFromBase64String(str, span, out var _);

        return Id.FromTaggedSpan(span);
    }
    
    public override void Write(Utf8JsonWriter writer, Id value, JsonSerializerOptions options)
    {
        Span<byte> span = stackalloc byte[value.SpanSize + 1];
        value.ToTaggedSpan(span);
        writer.WriteBase64StringValue(span);
    }

}
