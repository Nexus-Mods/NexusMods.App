using NexusMods.Abstractions.Steam.Values;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Games.GameHashes.Attributes;

/// <summary>
/// A Steam manifest id attribute.
/// </summary>
public class SteamManifestIdAttribute(string ns, string name) : ScalarAttribute<ManifestId, ulong, UInt64Serializer>(ns, name)
{
    protected override ulong ToLowLevel(ManifestId value) => value.Value;

    protected override ManifestId FromLowLevel(ulong value, AttributeResolver resolver) => ManifestId.From(value);
}
