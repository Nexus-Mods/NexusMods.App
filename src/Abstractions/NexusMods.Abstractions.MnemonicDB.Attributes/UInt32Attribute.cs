using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
/// A uint32 attribute
/// </summary>
public class UInt32Attribute(string ns, string name) : ScalarAttribute<uint, uint, UInt32Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override uint ToLowLevel(uint value)
    {
        return value;
    }


    /// <inheritdoc />
    protected override uint FromLowLevel(uint value, AttributeResolver resolver)
    {
        return value;
    }
}
