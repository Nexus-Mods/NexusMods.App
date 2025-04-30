using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Abstractions.Loadouts.Attributes;

/// <summary>
/// Datom attribute for a Guid.
/// </summary>
public sealed class GuidAttribute(string ns, string name) : ScalarAttribute<Guid, UInt128, UInt128Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override UInt128 ToLowLevel(Guid value) => MemoryMarshal.Read<UInt128>(value.ToByteArray());
    
    /// <inheritdoc />
    protected override Guid FromLowLevel(UInt128 value, AttributeResolver resolver)
    {
        var bytes = new byte[16];
        MemoryMarshal.Write(bytes, in value);
        return new Guid(bytes);
    }
    
}
