using DynamicData.Kernel;
using NexusMods.HyperDuck;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// A definition of a collection. This could be a user's collection, a Nexus Mods collection or perhaps a collection from some other source.
/// </summary>
[Include<LoadoutItemGroup>]
public partial class CollectionGroup : IModelDefinition
{
    private const string Namespace = "NexusMods.Loadouts.CollectionGroup";

    /// <summary>
    /// If the collection is read-only it won't support adding new mods or modifying the existing files. 
    /// </summary>
    public static readonly BooleanAttribute IsReadOnly = new(Namespace, nameof(IsReadOnly)) { IsIndexed = true };
}

public static partial class CollectionGroupLoaderExtensions
{
    private static CompiledQuery<List<(EntityId CollectionGroup, string Name)>, EntityId> MutableCollectionsQuery =
        Query.Compile<List<(EntityId CollectionGroup, string Name)>, EntityId>("SELECT Id, Name FROM mdb_CollectionGroup() WHERE IsReadOnly = false AND LoadoutId = $1");
    
    /// <summary>
    /// Find the user collection for a given loadout
    /// </summary>
    public static Query<(EntityId CollectionId, string Name)> MutableCollections(this Loadout.ReadOnly loadout)
    {
        return Loadout.MutableCollections(loadout.Db.Connection, loadout);
    }
}
