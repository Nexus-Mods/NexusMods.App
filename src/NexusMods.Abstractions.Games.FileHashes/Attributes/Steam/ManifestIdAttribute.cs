using NexusMods.Abstractions.Steam.Values;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Abstractions.Games.FileHashes.Attributes.Steam;

/// <summary>
/// A manifest ID attribute.
/// </summary>
public class ManifestIdAttribute(string ns, string name) : ScalarAttribute<ManifestId, ulong, UInt64Serializer>(ns, name)
{
    protected override ulong ToLowLevel(ManifestId value)
    {
        return value.Value;
    }

    protected override ManifestId FromLowLevel(ulong value, AttributeResolver resolver)
    {
        return ManifestId.From(value);
    }
}
