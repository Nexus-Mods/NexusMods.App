using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Abstractions.NexusModsLibrary.Attributes;

/// <summary>
/// Float attribute (32-bit)
/// </summary>
public class FloatAttribute(string ns, string name) : ScalarAttribute<float, float, Float32Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override float ToLowLevel(float value) => value;

    /// <inheritdoc />
    protected override float FromLowLevel(float value, AttributeResolver resolver) => value;
}
