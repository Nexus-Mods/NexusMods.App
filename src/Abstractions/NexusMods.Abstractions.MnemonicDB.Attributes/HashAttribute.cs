using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
/// Stores a <see cref="Hash"/> as a <see cref="ulong"/>.
/// </summary>
public class HashAttribute(string ns, string name) : ScalarAttribute<Hash, ulong, UInt64Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override ulong ToLowLevel(Hash value) => value.Value;

    /// <inheritdoc />
    protected override Hash FromLowLevel(ulong value, AttributeResolver resolver) => Hash.From(value);
}
