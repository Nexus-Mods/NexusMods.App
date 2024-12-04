using NexusMods.Games.FileHashes.HashValues;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
/// An attribute representing an MD5 hash value.
/// </summary>
public class Md5Attribute(string ns, string name) : ScalarAttribute<Md5Hash, UInt128, UInt128Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override UInt128 ToLowLevel(Md5Hash value) => value.Value;

    /// <inheritdoc />
    protected override Md5Hash FromLowLevel(UInt128 value, AttributeResolver resolver) => Md5Hash.From(value);
}


