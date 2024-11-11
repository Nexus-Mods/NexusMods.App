using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Abstractions.Loadouts.Attributes;

/// <summary>
/// Int32 attribute (int)
/// </summary>
public sealed class Int32Attribute(string ns, string name) : ScalarAttribute<int, int, Int32Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override int ToLowLevel(int value) => value;

    /// <inheritdoc />
    protected override int FromLowLevel(int value, AttributeResolver resolver) => value;
}

