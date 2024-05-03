using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
/// Represents a URL attribute.
/// </summary>
public class UrlAttribute(string ns, string name) : ScalarAttribute<Uri, string>(ValueTags.Utf8, ns, name)
{
    protected override string ToLowLevel(Uri value)
    {
        return value.ToString();
    }
    
    protected override Uri FromLowLevel(string value, ValueTags tag)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri : 
            throw new InvalidDataException("Cannot parse the URL.");
    }
}
