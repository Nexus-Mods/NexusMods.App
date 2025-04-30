using NexusMods.Abstractions.Hashes;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Abstractions.Games.FileHashes.Attributes;

/// <summary>
/// An attribute for a CRC32 hash.
/// </summary>
public class Crc32Attribute(string ns, string name) : ScalarAttribute<Crc32, UInt32, UInt32Serializer>(ns, name)
{
    protected override uint ToLowLevel(Crc32 value)
    {
        return value.Value;
    }

    protected override Crc32 FromLowLevel(uint value, AttributeResolver resolver)
    {
        return Crc32.From(value);
    }
}
