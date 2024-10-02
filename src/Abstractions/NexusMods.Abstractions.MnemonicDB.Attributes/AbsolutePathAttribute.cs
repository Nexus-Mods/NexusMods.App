using Microsoft.Extensions.DependencyInjection;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.Paths;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
/// An absolute path, stored as a string (case-sensitive)
/// </summary>
public class AbsolutePathAttribute(string ns, string name) : ScalarAttribute<AbsolutePath, string>(ValueTags.Utf8, ns, name)
{
    /// <inheritdoc />
    protected override string ToLowLevel(AbsolutePath value)
    {
        return value.ToString();
    }

    /// <inheritdoc />
    protected override AbsolutePath FromLowLevel(string value, ValueTags tag, AttributeResolver resolver)

    {
        return resolver.ServiceProvider.GetRequiredService<IFileSystem>().FromUnsanitizedFullPath(value);
    }
}
