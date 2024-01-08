using System.Buffers.Binary;

namespace NexusMods.DataModel.Abstractions.Ids;

/// <summary>
/// A 128-bit ID composed of two 64-bit IDs.
/// </summary>
public class TwoId64 : AId
{
    private readonly EntityCategory _type;
    private readonly ulong _a;
    private readonly ulong _b;

    /// <summary>
    /// Creates an ID composed of two 64-bit integers.
    /// </summary>
    /// <param name="type">Type of the entity used in this ID.</param>
    /// <param name="a">First part of the ID.</param>
    /// <param name="b">Second part of the ID.</param>
    public TwoId64(EntityCategory type, ulong a, ulong b)
    {
        _type = type;
        _a = a;
        _b = b;
    }

    /// <inheritdoc />
    public override EntityCategory Category => _type;

    /// <inheritdoc />
    public override int SpanSize => 16;

    /// <inheritdoc />
    public override bool Equals(IId? other)
    {
        if (other is TwoId64 id)
            return id._a == _a && id._b == _b && id._type == _type;

        return false;
    }

    /// <inheritdoc />
    public override void ToSpan(Span<byte> span)
    {
        BinaryPrimitives.WriteUInt64BigEndian(span, _a);
        BinaryPrimitives.WriteUInt64BigEndian(span[8..], _b);
    }
}
