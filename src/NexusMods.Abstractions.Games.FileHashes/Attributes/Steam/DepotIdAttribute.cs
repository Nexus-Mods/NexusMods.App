using NexusMods.Abstractions.Steam.Values;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Abstractions.Games.FileHashes.Attributes.Steam;

/// <summary>
/// A <see cref="DepotId"/> attribute.
/// </summary>
public class DepotIdAttribute(string ns, string name) : ScalarAttribute<DepotId, uint, UInt32Serializer>(ns, name)
{
    public override uint ToLowLevel(DepotId value)
    {
        return value.Value;
    }

    public override DepotId FromLowLevel(uint value, AttributeResolver resolver)
    {
        return DepotId.From(value);
    }
}
