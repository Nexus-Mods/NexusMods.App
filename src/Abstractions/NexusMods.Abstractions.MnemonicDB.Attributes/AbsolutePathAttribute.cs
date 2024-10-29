using Microsoft.Extensions.DependencyInjection;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;
using NexusMods.Paths;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
/// An absolute path, stored as a string (case-sensitive)
/// </summary>
public class AbsolutePathAttribute(string ns, string name) : ScalarAttribute<AbsolutePath, string, Utf8Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override string ToLowLevel(AbsolutePath value) => value.ToString();

    /// <inheritdoc />
    protected override AbsolutePath FromLowLevel(string value, AttributeResolver resolver) 
        => resolver.ServiceProvider.GetRequiredService<IFileSystem>().FromUnsanitizedFullPath(value);
}
