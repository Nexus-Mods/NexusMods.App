using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
/// A collection of URIs, stored as strings
/// </summary>
public class UrisAttribute(string ns, string name) : CollectionAttribute<Uri, string>(ValueTag.Utf8, ns, name) 
{
    /// <inheritdoc />
    protected override string ToLowLevel(Uri value) => value.ToString();

    /// <inheritdoc />
    protected override Uri FromLowLevel(string value, AttributeResolver resolver) => new(value);
}
