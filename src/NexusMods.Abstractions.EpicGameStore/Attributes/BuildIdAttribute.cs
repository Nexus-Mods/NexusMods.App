using NexusMods.Abstractions.EpicGameStore.Values;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Abstractions.EpicGameStore.Attributes;

public sealed class BuildIdAttribute(string ns, string name) : ScalarAttribute<BuildId, string, Utf8InsensitiveSerializer>(ns, name)
{
    public override string ToLowLevel(BuildId value) => value.Value;

    public override BuildId FromLowLevel(string value, AttributeResolver resolver) => BuildId.From(value);
}
