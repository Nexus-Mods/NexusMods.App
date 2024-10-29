using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
/// An attribute for a TxId.
/// </summary>
public class TxIdAttribute(string ns, string name) : ScalarAttribute<TxId, ulong, UInt64Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override ulong ToLowLevel(TxId value) => value.Value;

    /// <inheritdoc />
    protected override TxId FromLowLevel(ulong value, AttributeResolver resolver) => TxId.From(value);
}
