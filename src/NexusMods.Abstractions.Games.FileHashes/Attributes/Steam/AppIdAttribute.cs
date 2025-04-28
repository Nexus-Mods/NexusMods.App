using NexusMods.Abstractions.Steam.Values;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Abstractions.Games.FileHashes.Attributes.Steam;

/// <summary>
/// An attribute for a Steam App ID.
/// </summary>
public class AppIdAttribute(string ns, string name) : ScalarAttribute<AppId, uint, UInt32Serializer>(ns, name) 
{
    protected override uint ToLowLevel(AppId value)
    {
        return value.Value;
    }

    protected override AppId FromLowLevel(uint value, AttributeResolver resolver)
    {
        return AppId.From(value);
    }
}
