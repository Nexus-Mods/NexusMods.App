using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Sdk.Hashes;

/// <summary>
/// Attribute for <see cref="Md5Value"/>.
/// </summary>
public class Md5Attribute(string ns, string name) : ScalarAttribute<Md5Value, UInt128, UInt128Serializer>(ns, name)
{
    /// <inheritdoc/>
    public override UInt128 ToLowLevel(Md5Value value) => value.AsUInt128();

    /// <inheritdoc/>
    public override Md5Value FromLowLevel(UInt128 value, AttributeResolver resolver) => Md5Value.From(value);
}
