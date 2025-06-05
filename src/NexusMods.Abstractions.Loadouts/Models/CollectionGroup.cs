using DynamicData.Kernel;
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
    /// <summary>
    /// Find the user collection for a given loadout
    /// </summary>
    public static async ValueTask<CollectionGroup.ReadOnly[]> MutableCollections(this Loadout.ReadOnly loadout)
    {
        var db = loadout.Db;
        using var query = await db.Topology.QueryAsync(Loadout.MutableCollections);

        var result = query
            .Where(x => x.Loadout == loadout)
            .Select(x => CollectionGroup.Load(db, x.CollectionGroup))
            .OrderBy(x => x.Id)
            .ToArray();

        return result;
    }
}
