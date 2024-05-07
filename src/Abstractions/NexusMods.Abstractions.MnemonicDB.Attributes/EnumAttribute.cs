using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
///    An attribute that represents an enum value.
/// </summary>
public class EnumAttribute<T>(string ns, string name) : ScalarAttribute<T, int>(ValueTags.Int32, ns, name)
    where T : Enum
{
    /// <inheritdoc />
    protected override int ToLowLevel(T value)
    {
        // Looks like an allocation, but the cast to object is removed by the JIT since the type of
        // T is a compile-time constant. Verified via sharpLab.io
        return (int)(object)value;
    }

    /// <inheritdoc />
    protected override T FromLowLevel(int value, ValueTags tags)
    {
        // Same as ToLowLevel, the cast to object is removed by the JIT
        return (T)(object)value;
    }
}
