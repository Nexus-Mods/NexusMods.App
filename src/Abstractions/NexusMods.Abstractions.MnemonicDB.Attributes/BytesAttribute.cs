using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
/// Bytes.
/// </summary>
public class BytesAttribute(string ns, string name) : BlobAttribute<byte[]>(ns, name)
{
    /// <inheritdoc/>
    protected override byte[] FromLowLevel(ReadOnlySpan<byte> value, ValueTags tags, AttributeResolver resolver) => value.ToArray();

    /// <inheritdoc/>
    protected override void WriteValue<TWriter>(byte[] value, TWriter writer)
    {
        var span = writer.GetSpan(sizeHint: value.Length);

        value.CopyTo(span);
        writer.Advance(value.Length);
    }
}
