using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.Paths;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
/// A MneumonicDB attribute for a Size value
/// </summary>
public class SizeAttribute(string ns, string name) : ScalarAttribute<Size, ulong>(ValueTags.UInt64, ns, name)
{
    protected override ulong ToLowLevel(Size value) => value.Value;

    protected override Size FromLowLevel(ulong value, ValueTags tags) => Size.From(value);
}
