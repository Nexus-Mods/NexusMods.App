using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions;
using TransparentValueObjects;

namespace NexusMods.Abstractions.Loadouts.Ids;

/// <summary>
/// A Id that uniquely identifies a specific list. Names can collide and are often
/// used by users as short-hand for their Loadouts. Hence we give each Loadout a unique
/// Id. Essentially this is just a Guid, but we wrap this guid so that we can easily
/// distinguish it from other parts of the code that may use Guids for other object types
/// </summary>
[ValueObject<EntityId>]
[PublicAPI]
public readonly partial struct LoadoutId : ITypedId<Loadout.Model>
{
    
    /// <inheritdoc />
    EntityId ITypedEntityId.Value => Value;
    
    /// <summary>
    /// Resolve the Loadout.Model from the database
    /// </summary>
    public Loadout.Model Resolve(IDb db) => db.Get<Loadout.Model>(Value);
    
    /// <summary>
    /// Create a new LoadoutId from a loadout model
    /// </summary>
    public static implicit operator LoadoutId(Loadout.Model loadout) => From(loadout.Id);
    
    
    /// <summary>
    /// Try to parse a LoadoutId from a hex string
    /// </summary>
    public static bool TryParseFromHex(string hex, out LoadoutId id)
    {
        if (MnemonicDB.Attributes.Extensions.TryParseFromHex(hex, out var entityId))
        {
            id = From(entityId);
            return true;
        }
        id = default;
        return false;
    }
}
