namespace NexusMods.DataModel.Abstractions.Ids;

/// <summary>
/// Represents a zero-sized dummy ID.
/// </summary>
public class IdEmpty : IId
{
    /// <inheritdoc />
    public int SpanSize => 0;

    /// <summary>
    /// Static item for easy reuse.
    /// </summary>
    public static readonly IId Empty = new IdEmpty();

    public EntityCategory Category => 0;

    /// <inheritdoc />
    public void ToSpan(Span<byte> span) { }

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns><c>true</c> if the current object is equal to the <paramref name="other" /> parameter; otherwise, <c>false</c>.</returns>
    public bool Equals(IId? other)
    {
        return other is IdEmpty;
    }
}
