using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;
using OneOf;

namespace NexusMods.Abstractions.Games.Attributes;


/// <summary>
/// Attribute to represent either a LoadoutId or a CollectionGroupId.
/// Stores the EntityId in the lower 64 bits and the index in the upper 64 bits.
/// 0 is used for LoadoutId and 1 for CollectionGroupId.
/// </summary>
public class LoadoutOrCollectionAttribute(string ns, string name) : ScalarAttribute<OneOf<LoadoutId, CollectionGroupId>, UInt128, UInt128Serializer>(ns, name) 
{
    /// <inheritdoc />
    protected override UInt128 ToLowLevel(OneOf<LoadoutId, CollectionGroupId> value)
    {
        UInt128 entityID = value.Match(
            loadoutId => loadoutId.Value.Value,
            collectionGroupId => collectionGroupId.Value.Value
        );
        return  entityID | (((UInt128)value.Index) << 64);
    }

    /// <inheritdoc />
    protected override OneOf<LoadoutId, CollectionGroupId> FromLowLevel(UInt128 value, AttributeResolver resolver)
    {
        UInt128 mask = ulong.MaxValue;
        var entityId = (ulong)(value & mask);
        var index = (int)(value >> 64);
        
        if (index == 0)
        {
            return LoadoutId.From(EntityId.From(entityId));
        }
        else
        {
            return CollectionGroupId.From(EntityId.From(entityId));
        }
    }
}
