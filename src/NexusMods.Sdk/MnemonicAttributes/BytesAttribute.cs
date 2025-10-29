using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Sdk.MnemonicAttributes;

/// <summary>
/// Bytes.
/// </summary>
public class BytesAttribute(string ns, string name) : ScalarAttribute<Memory<byte>, Memory<byte>, BlobSerializer>(ns, name)
{
    /// <inheritdoc />
    public override Memory<byte> ToLowLevel(Memory<byte> value) => value;

    /// <inheritdoc />
    public override Memory<byte> FromLowLevel(Memory<byte> value, AttributeResolver resolver) => value;
}
