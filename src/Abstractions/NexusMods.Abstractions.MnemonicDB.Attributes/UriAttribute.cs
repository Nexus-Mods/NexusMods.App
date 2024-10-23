using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
/// Represents a URL attribute, stored as a UTF-8 string.
/// </summary>
public class UriAttribute(string ns, string name) : ScalarAttribute<Uri, string>(ValueTag.Utf8, ns, name) 
{
    /// <inheritdoc />
    protected override string ToLowLevel(Uri value) => value.ToString();

    /// <inheritdoc />
    protected override Uri FromLowLevel(string value, AttributeResolver resolver) => new(value);
}
