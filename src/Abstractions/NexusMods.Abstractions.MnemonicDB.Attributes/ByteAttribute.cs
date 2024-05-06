using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
/// Attribute for storing a UInt8
/// </summary>
public class ByteAttribute(string ns, string name) : ScalarAttribute<byte, byte>(ValueTags.UInt8, ns, name)
{
    protected override byte ToLowLevel(byte value)
    {
        return value;
    }

    protected override byte FromLowLevel(byte value, ValueTags tags)
    {
        return value;
    }
}
