

using Bannerlord.ModuleManager;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

public class ApplicationVersionAttribute(string ns, string name) :
    ScalarAttribute<ApplicationVersion, string>(ValueTags.Ascii, ns, name)
{
    
    protected override string ToLowLevel(ApplicationVersion value) => value.ToString();
    
    public override ApplicationVersion FromLowLevel(string value) 
        => ApplicationVersion.TryParse(value, out var version) ? version : ApplicationVersion.Empty;
}
