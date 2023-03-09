using System.Text.Json.Serialization;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;
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
// ReSharper disable once PartialTypeWithSinglePart
public readonly partial struct LoadoutId : ICreatable<LoadoutId>
{
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
        _value.TryWriteBytes(bytes);
        ((ReadOnlySpan<byte>)bytes).ToHex(span);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        Span<byte> span = stackalloc byte[16];
        _value.TryWriteBytes(span);
        return ((ReadOnlySpan<byte>)span).ToHex();
    }

    /// <summary>
    /// Creates a new loadout ID.
    /// </summary>
    public static LoadoutId Create()
    {
        return From(Guid.NewGuid());
    }
}
