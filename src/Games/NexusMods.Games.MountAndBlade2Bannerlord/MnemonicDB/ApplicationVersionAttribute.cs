using Bannerlord.ModuleManager;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

/// <summary>
/// An attribute for the application version.
/// </summary>
public class ApplicationVersionAttribute(string ns, string name) :
    ScalarAttribute<ApplicationVersion, string>(ValueTags.Ascii, ns, name)
{
    
    protected override string ToLowLevel(ApplicationVersion value) => value.ToString();

    protected override ApplicationVersion FromLowLevel(string value, ValueTags tag) =>
        ApplicationVersion.TryParse(value, out var version) ? version : ApplicationVersion.Empty;
}
