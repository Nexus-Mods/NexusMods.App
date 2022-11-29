using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.Hashing.xxHash64;

namespace NexusMods.DataModel.Abstractions;

[JsonConverter(typeof(IdJsonConverter))]
public record struct Id(Hash Hash)
{
}

public class IdJsonConverter : JsonConverter<Id>
{ 
    public override Id Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new Id(new Hash(reader.GetUInt64()));
    }
    
    public override void Write(Utf8JsonWriter writer, Id value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue((ulong)value.Hash);
    }
}
