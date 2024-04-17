using NexusMods.Abstractions.Serialization.DataModel.Ids;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.DataModel.Attributes;

/// <summary>
/// Attribute for the IId type
/// </summary>
// ReSharper disable once InconsistentNaming
public class IIdAttribute(string ns, string name) : BlobAttribute<IId>(ns, name)
{
    /// <inheritdoc />
    protected override IId FromLowLevel(ReadOnlySpan<byte> value, ValueTags tag)
    {
        return IId.FromTaggedSpan(value);
    }

    /// <inheritdoc />
    protected override void WriteValue<TWriter>(IId value, TWriter writer)
    {
        var size = value.SpanSize + 1;
        var span = writer.GetSpan(size);
        value.ToTaggedSpan(span);
        writer.Advance(size);
    }
}
