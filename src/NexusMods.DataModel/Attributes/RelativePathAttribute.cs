using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.Paths;

namespace NexusMods.DataModel.Attributes;

/// <summary>
/// Represents a relative path.
/// </summary>
public class RelativePathAttribute(string ns, string name) : ScalarAttribute<RelativePath, string>(ValueTags.Utf8Insensitive, ns, name)
{
    /// <inheritdoc />
    protected override string ToLowLevel(RelativePath value)
    {
        return value.Path;
    }

    /// <inheritdoc />
    protected override RelativePath FromLowLevel(string value, ValueTags tags)
    {
        return RelativePath.FromUnsanitizedInput(value);
    }
}
