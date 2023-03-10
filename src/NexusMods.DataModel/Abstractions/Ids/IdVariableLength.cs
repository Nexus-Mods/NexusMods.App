using System.Text;

namespace NexusMods.DataModel.Abstractions.Ids;

/// <summary>
/// Represents an ID which uses variable length.
/// </summary>
public class IdVariableLength : AId
{
    private readonly ReadOnlyMemory<byte> _data;
    private readonly EntityCategory _category;

    /// <summary>
    /// Creates a new id for a temporary entity with the given byte id.
    /// </summary>
    /// <param name="category">The category to use.</param>
    /// <param name="data">The raw ID to use as storage.</param>
    public IdVariableLength(EntityCategory category, ReadOnlyMemory<byte> data)
    {
        _category = category;
        _data = data;
    }

    /// <summary>
    /// Creates a new id for a temporary entity with the given string id
    /// </summary>
    /// <param name="category">The category to use.</param>
    /// <param name="data">
    ///    The string ID to use as storage.
    ///    This ID will be encoded as UTF8.
    /// </param>
    public IdVariableLength(EntityCategory category, string data)
    {
        _category = category;
        _data = Encoding.UTF8.GetBytes(data);
    }

    /// <inheritdoc />
    public override EntityCategory Category => _category;

    /// <inheritdoc />
    public override int SpanSize => _data.Length;

    /// <inheritdoc />
    public override bool Equals(IId? other)
    {
        if (other == null || other.SpanSize != _data.Length) return false;
        if (Category != other.Category) return false;
        Span<byte> buff = stackalloc byte[_data.Span.Length];
        other.ToSpan(buff);
        return _data.Span.SequenceEqual(buff);
    }

    /// <inheritdoc />
    public override void ToSpan(Span<byte> span)
    {
        _data.Span.CopyTo(span);
    }
}
