using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;
using NexusMods.Paths;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
/// Represents a relative path.
/// </summary>
public class RelativePathAttribute(string ns, string name) : ScalarAttribute<RelativePath, string, Utf8InsensitiveSerializer>(ns, name)
{
    /// <inheritdoc />
    protected override string ToLowLevel(RelativePath value) => value.Path;

    /// <inheritdoc />
    protected override RelativePath FromLowLevel(string value, AttributeResolver resolver) 
        => RelativePath.FromUnsanitizedInput(value);
}
