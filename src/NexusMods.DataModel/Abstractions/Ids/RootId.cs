namespace NexusMods.DataModel.Abstractions.Ids;

/// <summary>
/// Represents the unique index of a <see cref="Root{T}"/>.
/// </summary>
public class RootId : AId
{
    private readonly RootType _type;

    /// <summary/>
    /// <param name="type">
    ///    Type of the root stored.
    /// </param>
    public RootId(RootType type)
    {
        _type = type;
    }

    /// <inheritdoc />
    public override EntityCategory Category => EntityCategory.Roots;

    /// <inheritdoc />
    public override bool Equals(IId? other)
    {
        if (other is RootId id)
            return id._type == _type;

        return false;
    }

    /// <inheritdoc />
    public override int SpanSize => 1;

    /// <inheritdoc />
    public override void ToSpan(Span<byte> span)
    {
        span[0] = (byte)_type;
    }
}
