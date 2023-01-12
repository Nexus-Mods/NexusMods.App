using System.Buffers.Binary;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NexusMods.DataModel.Loadouts;

[JsonConverter(typeof(ModIdConverter))]
public class ModId : IEquatable<ModId>
{
    private readonly ulong _upper;
    private readonly ulong _lower;

    private ModId(ulong upper, ulong lower)
    {
        _upper = upper;
        _lower = lower;
    }

    public static ModId New()
    {
        Span<byte> span = stackalloc byte[16];
        Guid.NewGuid().TryWriteBytes(span);
        return new ModId(BinaryPrimitives.ReadUInt64BigEndian(span), 
            BinaryPrimitives.ReadUInt64BigEndian(span[8..]));
    }

    public static bool operator ==(ModId a, ModId b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(ModId a, ModId b)
    {
        return !(a == b);
    }


    public override int GetHashCode()
    {
        return HashCode.Combine(_upper, _lower);
    }

    public override string ToString()
    {
        return $"ModId-{_upper:x8}{_lower:x8}";
    }
    
    public class ModIdConverter : JsonConverter<ModId>
    {
        public override ModId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var data = reader.GetBytesFromBase64();
            return new ModId(BinaryPrimitives.ReadUInt64BigEndian(data),
                BinaryPrimitives.ReadUInt64BigEndian(data[8..]));
        }

        public override void Write(Utf8JsonWriter writer, ModId value, JsonSerializerOptions options)
        {
            Span<byte> span = stackalloc byte[16];
            BinaryPrimitives.WriteUInt64BigEndian(span, value._upper);
            BinaryPrimitives.WriteUInt64BigEndian(span[8..], value._lower);
            writer.WriteBase64StringValue(span);
        }
    }

    public bool Equals(ModId? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return _upper == other._upper && _lower == other._lower;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((ModId)obj);
    }
}

