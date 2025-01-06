using NexusMods.Abstractions.GOG.Values;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Games.GameHashes.Attributes;

/// <summary>
/// A GOG build id attribute.
/// </summary>
public class GOGBuildIdAttribute(string ns, string name) : ScalarAttribute<BuildId, ulong, UInt64Serializer>(ns, name)
{
    protected override ulong ToLowLevel(BuildId value) => value.Value;

    protected override BuildId FromLowLevel(ulong value, AttributeResolver resolver) => BuildId.From(value);
}
