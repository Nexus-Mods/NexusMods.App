using System.Buffers;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Abstractions.NexusModsLibrary.Attributes;

/// <summary>
/// A hashed blob attribute for <see cref="Memory{T}"/>.
/// </summary>
public class MemoryAttribute(string ns, string name) : ScalarAttribute<Memory<byte>, Memory<byte>, HashedBlobSerializer>(ns, name)
{
    /// <inheritdoc />
    protected override Memory<byte> ToLowLevel(Memory<byte> value) => value;

    /// <inheritdoc />
    protected override Memory<byte> FromLowLevel(Memory<byte> value, AttributeResolver resolver) => value;
}
