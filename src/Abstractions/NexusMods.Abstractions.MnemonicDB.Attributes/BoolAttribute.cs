using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
/// Stores a boolean.
/// </summary>
public class BoolAttribute(string ns, string name) : ScalarAttribute<bool, byte>(ValueTags.UInt8, ns, name)
{
    /// <inheritdoc />
    protected override byte ToLowLevel(bool value) => value ? (byte)1 : (byte)0;

    /// <inheritdoc />
    protected override bool FromLowLevel(byte value, ValueTags tags) => value != 0;
}
