using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using NexusMods.Abstractions.Serialization.DataModel;
using NexusMods.Abstractions.Serialization.DataModel.Ids;
using NexusMods.Hashing.xxHash64;
using TransparentValueObjects;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// A Id that uniquely identifies a specific list. Names can collide and are often
/// used by users as short-hand for their Loadouts. Hence we give each Loadout a unique
/// Id. Essentially this is just a Guid, but we wrap this guid so that we can easily
/// distinguish it from other parts of the code that may use Guids for other object types
/// </summary>
[ValueObject<Guid>]
[JsonConverter(typeof(LoadoutIdConverter))]
[PublicAPI]
public readonly partial struct LoadoutId : IAugmentWith<DefaultValueAugment>
{
    /// <inheritdoc/>
    public static LoadoutId DefaultValue { get; } = From(Guid.Empty);

    // Note: We store this as hex because we need to serialize to JSON.

    /// <summary>
    /// Deserializes a loadout ID from hex string.
    /// </summary>
    /// <param name="hex">The span of characters storing the value for this loadout.</param>
    public static LoadoutId FromHex(ReadOnlySpan<char> hex)
    {
        Span<byte> span = stackalloc byte[16];
        hex.FromHex(span);
        return From(new Guid(span));
    }

    /// <summary>
    /// Serializes the loadout id to this hex string.
    /// </summary>
    /// <param name="span">To span.</param>
    public void ToHex(Span<char> span)
    {
        Span<byte> bytes = stackalloc byte[16];
        Value.TryWriteBytes(bytes);
        ((ReadOnlySpan<byte>)bytes).ToHex(span);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        Span<byte> span = stackalloc byte[16];
        Value.TryWriteBytes(span);
        return ((ReadOnlySpan<byte>)span).ToHex();
    }

    /// <summary>
    /// Creates a new loadout ID.
    /// </summary>
    public static LoadoutId Create() => From(Guid.NewGuid());

    /// <summary>
    /// Converts the loadout ID to a byte array.
    /// </summary>
    /// <returns></returns>
    public byte[] ToArray()
    {
        Span<byte> span = stackalloc byte[16];
        Value.TryWriteBytes(span);
        return span.ToArray();
    }

    /// <summary>
    /// Returns a new loadout ID from the given byte array.
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static LoadoutId From(byte[] bytes)
    {
        return From(new Guid(bytes));
    }

    /// <summary>
    /// Convert a 128bit id to a loadout id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static LoadoutId From(IId id)
    {
        Span<byte> span = stackalloc byte[16];
        id.ToSpan(span);
        return From(span);
    }

    /// <summary>
    /// Convert a 16 byte span to a loadout id.
    /// </summary>
    /// <param name="span"></param>
    /// <returns></returns>
    public static LoadoutId From(ReadOnlySpan<byte> span) => From(new Guid(span));

    /// <summary>
    /// Converts the loadout ID to an entity ID.
    /// </summary>
    /// <param name="category"></param>
    /// <returns></returns>
    public IId ToEntityId(EntityCategory category)
    {
        Span<byte> span = stackalloc byte[16];
        Value.TryWriteBytes(span);
        return IId.FromSpan(category, span);
    }

    /// <summary>
    /// Parses a loadout ID from the ToString() representation.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="loadoutId"></param>
    /// <returns></returns>
    public static bool TryParseFromHex(string input, out LoadoutId loadoutId)
    {
        return TryParseFromHex((ReadOnlySpan<char>)input, out loadoutId);
    }

    /// <summary>
    /// Parses a loadout ID from the ToString() representation.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="loadoutId"></param>
    /// <returns></returns>
    public static bool TryParseFromHex(ReadOnlySpan<char> input, out LoadoutId loadoutId)
    {
        if (input.Length != 32)
        {
            loadoutId = DefaultValue;
            return false;
        }

        try
        {
            loadoutId = FromHex(input);
            return true;
        }
        catch
        {
            loadoutId = DefaultValue;
            return false;
        }

    }
}

/// <inheritdoc />
public class LoadoutIdConverter : JsonConverter<LoadoutId>
{
    /// <inheritdoc />
    public override LoadoutId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString();
        return LoadoutId.FromHex(str);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, LoadoutId value, JsonSerializerOptions options)
    {
        Span<char> span = stackalloc char[32];
        value.ToHex(span);
        writer.WriteStringValue(span);
    }
}
