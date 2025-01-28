using NexusMods.Abstractions.GOG.Values;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Abstractions.Games.FileHashes.Attributes.Gog;

/// <summary>
/// An attribute for storing GOG build IDs.
/// </summary>
public class BuildIdAttribute(string ns, string name) : ScalarAttribute<BuildId, ulong, UInt64Serializer>(ns, name)
{
    protected override ulong ToLowLevel(BuildId value)
    {
        return value.Value;
    }

    protected override BuildId FromLowLevel(ulong value, AttributeResolver resolver)
    {
        return BuildId.From(value);
    }
}
