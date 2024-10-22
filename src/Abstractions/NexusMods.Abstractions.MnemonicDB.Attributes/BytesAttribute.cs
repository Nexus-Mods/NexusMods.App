using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
/// Bytes.
/// </summary>
public class BytesAttribute(string ns, string name) : ScalarAttribute<Memory<byte>, Memory<byte>>(ValueTag.Blob, ns, name)
{
    /// <inheritdoc />
    protected override Memory<byte> ToLowLevel(Memory<byte> value) => value;

    /// <inheritdoc />
    protected override Memory<byte> FromLowLevel(Memory<byte> value, AttributeResolver resolver) => value;
}
