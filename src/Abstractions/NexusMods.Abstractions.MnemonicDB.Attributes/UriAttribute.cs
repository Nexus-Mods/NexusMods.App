using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
/// Represents a URL attribute, stored as a UTF-8 string.
/// </summary>
public class UriAttribute(string ns, string name) : ScalarAttribute<Uri, string>(ValueTags.Utf8, ns, name) 
{
    /// <inheritdoc />
    protected override string ToLowLevel(Uri value)
    {
        return value.ToString();
    }

    /// <inheritdoc />
    protected override Uri FromLowLevel(string value, ValueTags tag)
    {
        return new Uri(value);
    }
}
