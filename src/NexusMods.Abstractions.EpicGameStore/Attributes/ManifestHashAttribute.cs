using NexusMods.Abstractions.EpicGameStore.Values;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Abstractions.EpicGameStore.Attributes;

public sealed class ManifestHashAttribute(string ns, string name) : ScalarAttribute<ManifestHash, string, Utf8InsensitiveSerializer>(ns, name)
{
    protected override string ToLowLevel(ManifestHash value) => value.Value;

    protected override ManifestHash FromLowLevel(string value, AttributeResolver resolver) => ManifestHash.From(value);
}
