using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Abstractions.NexusModsLibrary.Attributes;

/// <summary>
/// An attribute that holds a <see cref="CollectionId"/> value.
/// </summary>
public class CollectionIdAttribute(string ns, string name) : ScalarAttribute<CollectionId, ulong, UInt64Serializer>(ns, name)
{
    /// <inheritdoc />
    public override ulong ToLowLevel(CollectionId value) => value.Value;

    /// <inheritdoc />
    public override CollectionId FromLowLevel(ulong value, AttributeResolver resolver) => CollectionId.From(value);
}
