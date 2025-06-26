using NexusMods.Abstractions.Steam.Values;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Abstractions.Games.FileHashes.Attributes.Steam;

/// <summary>
/// An attribute for a Steam App ID.
/// </summary>
public class PackageIdAttribute(string ns, string name) : ScalarAttribute<PackageId, uint, UInt32Serializer>(ns, name) 
{
    protected override uint ToLowLevel(PackageId value) => value.Value;

    protected override PackageId FromLowLevel(uint value, AttributeResolver resolver) => PackageId.From(value);
}
