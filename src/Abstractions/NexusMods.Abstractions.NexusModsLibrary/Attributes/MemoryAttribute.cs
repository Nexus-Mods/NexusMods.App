using System.Buffers;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.Abstractions.NexusModsLibrary.Attributes;

/// <summary>
/// A hashed blob attribute for <see cref="Memory{T}"/>.
/// </summary>
public class MemoryAttribute(string ns, string name) : ScalarAttribute<Memory<byte>, Memory<byte>>(ValueTag.HashedBlob, ns, name)
{
    /// <inheritdoc />
    protected override Memory<byte> ToLowLevel(Memory<byte> value)
    {
        return value;
    }

    /// <inheritdoc />
    protected override Memory<byte> FromLowLevel(Memory<byte> value, AttributeResolver resolver)
    {
        return value;
    }
}
