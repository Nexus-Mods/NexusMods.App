using System.Diagnostics;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Sdk.MnemonicAttributes;

/// <summary>
/// Attribute for <see cref="Guid"/>.
/// </summary>
public sealed class GuidAttribute(string ns, string name) : ScalarAttribute<Guid, UInt128, UInt128Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override UInt128 ToLowLevel(Guid value)
    {
        Span<byte> bytes = stackalloc byte[16];
        var success = value.TryWriteBytes(bytes);
        Debug.Assert(success);

        return MemoryMarshal.Read<UInt128>(bytes);
    }

    /// <inheritdoc />
    protected override Guid FromLowLevel(UInt128 value, AttributeResolver resolver)
    {
        Span<byte> bytes = stackalloc byte[16];
        MemoryMarshal.Write(bytes, value);

        return new Guid(bytes);
    }
}
