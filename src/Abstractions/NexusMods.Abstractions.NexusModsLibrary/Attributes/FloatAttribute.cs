using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.Abstractions.NexusModsLibrary.Attributes;

public class FloatAttribute(string ns, string name) : ScalarAttribute<float, float>(ValueTags.Float32, ns, name)
{
    protected override float ToLowLevel(float value)
    {
        return value;
    }

    protected override float FromLowLevel(float value, ValueTags tags, RegistryId registryId)
    {
        return value;
    }
}
