using System.Buffers;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.Abstractions.NexusModsLibrary.Attributes;

/// <summary>
/// A hashed blob attribute for <see cref="Memory{T}"/>.
/// </summary>
public class MemoryAttribute(string ns, string name) : HashedBlobAttribute<Memory<byte>>(ns, name)
{
    /// <inheritdoc />
    protected override Memory<byte> FromLowLevel(ReadOnlySpan<byte> value, ValueTags tags, RegistryId registryId)
    {
        return new Memory<byte>(value.ToArray());
    }

    /// <inheritdoc />
    protected override void WriteValue<TWriter>(Memory<byte> value, TWriter writer)
    {
        writer.Write(value.Span);
    }
}
