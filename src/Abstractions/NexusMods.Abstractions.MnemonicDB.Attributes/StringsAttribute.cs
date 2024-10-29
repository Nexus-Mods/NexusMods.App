using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
/// A unordered collection of strings.
/// </summary>
public class StringsAttribute(string ns, string name) : CollectionAttribute<string, string, Utf8Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override string ToLowLevel(string value) => value;

    /// <inheritdoc />
    protected override string FromLowLevel(string value, AttributeResolver resolver) => value;
}
