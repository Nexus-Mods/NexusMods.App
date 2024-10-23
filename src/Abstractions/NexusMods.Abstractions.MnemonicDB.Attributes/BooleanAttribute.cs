using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
///    An attribute that represents a boolean value.
/// </summary>
public class BooleanAttribute(string ns, string name) : ScalarAttribute<bool, byte>(ValueTag.UInt8, ns, name)
{
    /// <inheritdoc />
    protected override byte ToLowLevel(bool value)
    {
        return value ? (byte) 1 : (byte) 0;
    }

    /// <inheritdoc />
    protected override bool FromLowLevel(byte value, AttributeResolver resolver)
    {
        return value == 1;
    }
}
