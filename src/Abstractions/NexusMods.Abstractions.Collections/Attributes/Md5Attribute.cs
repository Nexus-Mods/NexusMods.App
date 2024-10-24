using NexusMods.Abstractions.Collections.Types;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Abstractions.Collections.Attributes;

/// <summary>
/// An attribute representing an MD5 hash value.
/// </summary>
public class Md5Attribute(string ns, string name) : ScalarAttribute<Md5HashValue, UInt128, UInt128Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override UInt128 ToLowLevel(Md5HashValue value) => value.Value;

    /// <inheritdoc />
    protected override Md5HashValue FromLowLevel(UInt128 value, AttributeResolver resolver) 
        => Md5HashValue.From(value);
}
