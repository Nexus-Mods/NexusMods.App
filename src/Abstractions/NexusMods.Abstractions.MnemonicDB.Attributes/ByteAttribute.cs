using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
/// Attribute for storing a UInt8
/// </summary>
public class ByteAttribute(string ns, string name) : ScalarAttribute<byte, byte, UInt8Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override byte ToLowLevel(byte value) => value;

    /// <inheritdoc />
    protected override byte FromLowLevel(byte value, AttributeResolver resolver) => value;
}
