using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;
using NexusMods.Networking.EpicGameStore.Values;

namespace NexusMods.Networking.EpicGameStore.Attributes;

public sealed class ItemIdAttribute(string ns, string name) : ScalarAttribute<ItemId, string, Utf8InsensitiveSerializer>(ns, name)
{
    protected override string ToLowLevel(ItemId value) => value.Value;

    protected override ItemId FromLowLevel(string value, AttributeResolver resolver) => ItemId.From(value);
}
