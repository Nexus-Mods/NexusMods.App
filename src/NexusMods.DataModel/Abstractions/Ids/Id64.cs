using System.Buffers.Binary;

namespace NexusMods.DataModel.Abstractions.Ids;

/// <summary>
/// An ID represented as a big endian 8 byte value.
/// </summary>
public class Id64 : AId
{
    private readonly ulong _id;

    /// <inheritdoc />
    public override int SpanSize => 8;

    /// <inheritdoc />
    public override EntityCategory Category { get; }

    /// <summary>
    /// Creates a new 64-bit ID from the given category and raw ID.
    /// </summary>
    /// <param name="category">The category to use.</param>
    /// <param name="id">The raw ID to use as storage.</param>
    public Id64(EntityCategory category, ulong id)
    {
        _id = id;
        Category = category;
    }

    /// <inheritdoc />
    public override void ToSpan(Span<byte> span)
    {
        BinaryPrimitives.WriteUInt64BigEndian(span, _id);
    }

    /// <inheritdoc />
    public override bool Equals(IId? other)
    {
        // TODO: stackalloc and allocateuninitializedarray where needed.
        if (other is not { SpanSize: 8 }) return false;
        Span<byte> buff = stackalloc byte[8];
        other.ToSpan(buff);
        return BinaryPrimitives.ReadUInt64BigEndian(buff) == _id;
    }
}
