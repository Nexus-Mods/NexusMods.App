using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Sdk.Hashes;

/// <summary>
/// Attribute for <see cref="Crc32Value"/>.
/// </summary>
public class Crc32Attribute(string ns, string name) : ScalarAttribute<Crc32Value, uint, UInt32Serializer>(ns, name)
{
    /// <inheritdoc/>
    public override uint ToLowLevel(Crc32Value value) => value.Value;

    /// <inheritdoc/>
    public override Crc32Value FromLowLevel(uint value, AttributeResolver resolver) => Crc32Value.From(value);
}
