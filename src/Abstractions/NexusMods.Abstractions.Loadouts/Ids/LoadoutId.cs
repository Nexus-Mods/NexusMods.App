using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using NexusMods.Abstractions.Serialization.DataModel;
using NexusMods.Abstractions.Serialization.DataModel.Ids;
using NexusMods.Hashing.xxHash64;
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
}
