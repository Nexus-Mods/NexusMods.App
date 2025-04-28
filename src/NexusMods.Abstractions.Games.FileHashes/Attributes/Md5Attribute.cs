using NexusMods.Abstractions.Hashes;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Abstractions.Games.FileHashes.Attributes;

/// <summary>
/// An attribute for storing MD5 hashes.
/// </summary>
public class Md5Attribute(string ns, string name) : ScalarAttribute<Md5, UInt128, UInt128Serializer>(ns, name)
{
    protected override UInt128 ToLowLevel(Md5 value)
    {
        return value.ToUInt128();
    }

    protected override Md5 FromLowLevel(UInt128 value, AttributeResolver resolver)
    {
        return Md5.FromUInt128(value);
    }
}
