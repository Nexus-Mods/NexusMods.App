namespace NexusMods.DataModel.Abstractions.Ids;

/// <summary>
/// Base class for all non-empty IDs.
/// </summary>
public abstract class AId : IId
{
    /// <inheritdoc />
    public abstract int SpanSize { get; }

    /// <inheritdoc />
    public abstract void ToSpan(Span<byte> span);

    /// <inheritdoc />
    public abstract EntityCategory Category { get; }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        Span<byte> span = stackalloc byte[SpanSize];
        ToSpan(span);
        var hash = new HashCode();
        hash.AddBytes(span);
        return hash.ToHashCode();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        Span<byte> span = stackalloc byte[SpanSize];
        ToSpan(span);
        return $"{Category.ToStringFast()}-{Convert.ToHexString(span)}";
    }

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns><c>true</c> if the current object is equal to the <paramref name="other" /> parameter; otherwise, <c>false</c>.</returns>
    public abstract bool Equals(IId? other);

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns><c>true</c> if the current object is equal to the <paramref name="other" /> parameter; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? other)
    {
        if (other is IId id)
            return Equals(id);

        return false;
    }
}
