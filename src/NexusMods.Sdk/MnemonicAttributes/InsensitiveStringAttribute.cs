using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Sdk.MnemonicAttributes;

/// <summary>
/// A string attribute that is case insensitive when stored and retrieved.
/// </summary>
public class InsensitiveStringAttribute(string ns, string name) : ScalarAttribute<string, string, Utf8InsensitiveSerializer>(ns, name)
{
    protected override string ToLowLevel(string value)
    {
        return value;
    }

    protected override string FromLowLevel(string value, AttributeResolver resolver)
    {
        return value;
    }
}
