using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.DataModel.Attributes;

/// <summary>
/// Stores a <see cref="Hash"/> as a <see cref="ulong"/>.
/// </summary>
public class HashAttribute(string ns, string name) : ScalarAttribute<Hash, ulong>(ValueTags.UInt64, ns, name)
{
    /// <inheritdoc />
    protected override ulong ToLowLevel(Hash value)
    {
        return value.Value;
    }
    
    /// <inheritdoc />
    protected override Hash FromLowLevel(ulong value, ValueTags tags)
    {
        return Hash.From(value);
    }
}
