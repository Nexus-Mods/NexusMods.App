using Mutagen.Bethesda.Plugins;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Games.CreationEngine.Attributes;

public class ModKeyAttribute(string ns, string name) : ScalarAttribute<ModKey, string, Utf8InsensitiveSerializer>(ns, name)
{
    protected override string ToLowLevel(ModKey value)
    {
        return value.ToString();
    }

    protected override ModKey FromLowLevel(string value, AttributeResolver resolver)
    {
        if (ModKey.TryFromNameAndExtension(value, out var key))
            return key;
        return ModKey.Null;
    }
}
