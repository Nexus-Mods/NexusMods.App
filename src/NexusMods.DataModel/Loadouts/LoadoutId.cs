using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.DataModel.Abstractions;
using NexusMods.Hashing.xxHash64;
using Vogen;

namespace NexusMods.DataModel.Loadouts;

/// <summary>
/// A Id that uniquely identifies a specific list. Names can collide and are often
/// used by users as short-hand for their Loadouts. Hence we give each Loadout a unique
/// Id. Essentially this is just a Guid, but we wrap this guid so that we can easily
/// distinguish it from other parts of the code that may use Guids for other object types
/// </summary>
[ValueObject<Guid>(conversions: Conversions.None)]
[JsonConverter(typeof(LoadoutIdConverter))]
public readonly partial struct LoadoutId : ICreatable<LoadoutId>
{
    public static LoadoutId FromHex(ReadOnlySpan<char> hex)
    {
        Span<byte> span = stackalloc byte[16];
        hex.FromHex(span);
        return From(new Guid(span));
    }

    public void ToHex(Span<char> span)
    {
        Span<byte> bytes = stackalloc byte[16];
        _value.TryWriteBytes(bytes);
        ((ReadOnlySpan<byte>)bytes).ToHex(span);
    }

    public override string ToString()
    {
        Span<byte> span = stackalloc byte[16];
        _value.TryWriteBytes(span);
        return ((ReadOnlySpan<byte>)span).ToHex();
    }

    public static LoadoutId Create()
    {
        return From(Guid.NewGuid());
    }
}

public class LoadoutIdConverter : JsonConverter<LoadoutId>
{
    public override LoadoutId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString();
        return LoadoutId.FromHex(str);
    }

    public override void Write(Utf8JsonWriter writer, LoadoutId value, JsonSerializerOptions options)
    {
        Span<char> span = stackalloc char[32];
        value.ToHex(span);
        writer.WriteStringValue(span);
    }
}
