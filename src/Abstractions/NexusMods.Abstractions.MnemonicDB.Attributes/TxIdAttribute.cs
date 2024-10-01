using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
/// An attribute for a TxId.
/// </summary>
public class TxIdAttribute(string ns, string name) : ScalarAttribute<TxId, ulong>(ValueTags.UInt64, ns, name)
{
    /// <inheritdoc />
    protected override ulong ToLowLevel(TxId value)
    {
        return value.Value;
    }

    /// <inheritdoc />
    protected override TxId FromLowLevel(ulong value, ValueTags tags, AttributeResolver resolver)
    {
        return TxId.From(value);
    }
}
