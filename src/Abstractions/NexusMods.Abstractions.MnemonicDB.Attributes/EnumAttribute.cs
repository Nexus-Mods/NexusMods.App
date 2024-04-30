using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
///    An attribute that represents an enum value.
/// </summary>
public class EnumAttribute<T>(string ns, string name) : ScalarAttribute<T, string>(ValueTags.Utf8, ns, name)
    where T : Enum
{
    /// <inheritdoc />
    protected override string ToLowLevel(T value)
    {
        return value.ToString();
    }

    /// <inheritdoc />
    protected override T FromLowLevel(string value, ValueTags tag)
    {
        return (T) Enum.Parse(typeof(T), value);
    }
}
