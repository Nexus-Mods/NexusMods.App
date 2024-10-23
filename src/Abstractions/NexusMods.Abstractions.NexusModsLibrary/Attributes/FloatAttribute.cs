using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.Abstractions.NexusModsLibrary.Attributes;

public class FloatAttribute(string ns, string name) : ScalarAttribute<float, float>(ValueTag.Float32, ns, name)
{
    protected override float ToLowLevel(float value)
    {
        return value;
    }

    protected override float FromLowLevel(float value, AttributeResolver resolver)
    {
        return value;
    }
}
