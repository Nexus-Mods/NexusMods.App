using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
///    An attribute that represents an enum value.
/// </summary>
public class EnumAttribute<T>(string ns, string name) : ScalarAttribute<T, int, Int32Serializer>(ns, name)
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
    protected override T FromLowLevel(int value, AttributeResolver resolver)
    {
        // Same as ToLowLevel, the cast to object is removed by the JIT
        return (T)(object)value;
    }
}

/// <summary>
///    An attribute that represents an enum value with a backing type of a byte.
/// </summary>
public class EnumByteAttribute<T>(string ns, string name) : ScalarAttribute<T, byte, UInt8Serializer>(ns, name)
    where T : Enum
{
    /// <inheritdoc />
    protected override byte ToLowLevel(T value)
    {
        // Looks like an allocation, but the cast to object is removed by the JIT since the type of
        // T is a compile-time constant. Verified via sharpLab.io
        return (byte)(object)value;
    }

    /// <inheritdoc />
    protected override T FromLowLevel(byte value, AttributeResolver resolver)
    {
        // Same as ToLowLevel, the cast to object is removed by the JIT
        return (T)(object)value;
    }
}
