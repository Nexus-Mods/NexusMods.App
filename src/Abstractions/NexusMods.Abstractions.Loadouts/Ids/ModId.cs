using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.MnemonicDB.Abstractions;
using TransparentValueObjects;

namespace NexusMods.Abstractions.Loadouts.Ids;

/// <summary>
/// A mod is a collection of files that have been grouped together, most often because
/// they are from the same installation archive.
/// </summary>
[ValueObject<EntityId>]
public readonly partial struct ModId : ITypedId<Mod.Model>
{
    EntityId ITypedEntityId.Value => Value;
}
