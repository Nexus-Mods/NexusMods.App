using System.Buffers.Binary;
using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.Hashing.xxHash64;

namespace NexusMods.DataModel.Abstractions;

[JsonConverter(typeof(IdJsonConverter))]
public record struct Id(EntityCategory Category, Hash Hash)
{
    
    public void ToTaggedSpan(Span<byte> span)
    {
        span[0] = (byte)Category;
        BinaryPrimitives.WriteUInt64BigEndian(span[1..], (ulong)Hash);
    }

    public static Id FromTaggedSpan(ReadOnlySpan<byte> span)
    {
        var category = (EntityCategory)span[0];
        var id = BinaryPrimitives.ReadUInt64BigEndian(span[1..]);
        return new Id(category, Hash.FromULong(id));
    }

    public static Id Empty = new();
}

public class IdJsonConverter : JsonConverter<Id>
{ 
    public override Id Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        Span<byte> span = stackalloc byte[12]; 
        Convert.TryFromBase64String(reader.GetString()!, span, out var _);
        
        return new Id((EntityCategory)span[0], Hash.FromULong(BinaryPrimitives.ReadUInt64BigEndian(span[1..])));
    }
    
    public override void Write(Utf8JsonWriter writer, Id value, JsonSerializerOptions options)
    {
        Span<byte> span = stackalloc byte[9];
        span[0] = (byte)value.Category;
        BinaryPrimitives.WriteUInt64BigEndian(span[1..], (ulong)value.Hash);
        writer.WriteBase64StringValue(span);
    }

}
