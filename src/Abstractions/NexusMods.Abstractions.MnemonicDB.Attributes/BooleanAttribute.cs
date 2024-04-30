using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
///    An attribute that represents a boolean value.
/// </summary>
public class BooleanAttribute(string ns, string name) : ScalarAttribute<bool, byte>(ValueTags.UInt8, ns, name)
{
    /// <inheritdoc />
    protected override byte ToLowLevel(bool value)
    {
        return value ? (byte) 1 : (byte) 0;
    }

    /// <inheritdoc />
    protected override bool FromLowLevel(byte value, ValueTags tags)
    {
        return value == 1;
    }
}
