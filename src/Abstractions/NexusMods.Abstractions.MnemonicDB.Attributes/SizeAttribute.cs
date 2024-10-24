using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;
using NexusMods.Paths;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
/// A MneumonicDB attribute for a Size value
/// </summary>
public class SizeAttribute(string ns, string name) : ScalarAttribute<Size, ulong, UInt64Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override ulong ToLowLevel(Size value) => value.Value;

    /// <inheritdoc />
    protected override Size FromLowLevel(ulong value, AttributeResolver resolver) => Size.From(value);
}
