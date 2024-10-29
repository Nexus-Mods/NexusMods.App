using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
/// A MneumonicDB attribute for a ulong value
/// </summary>
public class ULongAttribute(string ns, string name) : ScalarAttribute<ulong, ulong, UInt64Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override ulong ToLowLevel(ulong value) => value;

    /// <inheritdoc />
    protected override ulong FromLowLevel(ulong value, AttributeResolver resolver) => value;
}
