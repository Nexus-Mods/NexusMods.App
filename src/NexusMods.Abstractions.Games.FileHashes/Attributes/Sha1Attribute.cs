using NexusMods.Abstractions.Hashes;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Abstractions.Games.FileHashes.Attributes;

/// <summary>
/// An attribute for a SHA1 hash
/// </summary>
public class Sha1Attribute(string ns, string name) : ScalarAttribute<Sha1, Memory<byte>, BlobSerializer>(ns, name)
{
    protected override Memory<byte> ToLowLevel(Sha1 value)
    {
        return value.ToArray();
    }

    protected override Sha1 FromLowLevel(Memory<byte> value, AttributeResolver resolver)
    {
        return Sha1.From(value.Span);
    }
}
