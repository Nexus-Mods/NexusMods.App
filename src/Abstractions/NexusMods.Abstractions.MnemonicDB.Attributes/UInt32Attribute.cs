using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
/// A uint32 attribute
/// </summary>
public class UInt32Attribute(string ns, string name) : ScalarAttribute<uint, uint>(ValueTags.UInt32, ns, name)
{
    /// <inheritdoc />
    protected override uint ToLowLevel(uint value)
    {
        return value;
    }


    /// <inheritdoc />
    protected override uint FromLowLevel(uint value, ValueTags tags, AttributeResolver resolver)
    {
        return value;
    }
}
