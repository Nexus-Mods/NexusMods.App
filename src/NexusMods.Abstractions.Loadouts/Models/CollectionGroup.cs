using System.Runtime.InteropServices;
using NexusMods.HyperDuck;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

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
    
    

    private const string CollectionEntities = """

                                              """;
    
    /// <summary>
    /// Performs a deep clone of the collection, and includes all child loadout items and sortable items and lists
    /// </summary>
    public static async Task<EntityId> Clone(IConnection conn, EntityId id)
    {
        var basisDb = conn.Db;

        Dictionary<EntityId, EntityId> remappedIds = new();
        var query = conn.Query<EntityId>($"""
                                          WITH RECURSIVE ChildLoadoutItems (Id) AS 
                                          (SELECT {id} 
                                          UNION
                                          SELECT Id FROM (SELECT Id, Parent FROM mdb_LoadoutItem(Db=>{basisDb})
                                          UNION ALL
                                          SELECT Id, ParentEntity FROM mdb_SortOrder(Db=>{basisDb})
                                          UNION ALL
                                          SELECT Id, ParentSortOrder FROM mdb_SortOrderItem(Db=>{basisDb}))
                                          WHERE Parent in (SELECT Id FROM ChildLoadoutItems))
                                          SELECT DISTINCT Id FROM ChildLoadoutItems
                                          """);
        var tx = conn.BeginTransaction();
        foreach (var itemId in query)
        {
            remappedIds.TryAdd(itemId, tx.TempId());
        }

        foreach (var (oldId, newId) in remappedIds)
        {
            var entity = basisDb[oldId];
            foreach (var datom in entity)
            {
                // Remap the value part of references
                if (datom.Prefix.ValueTag == ValueTag.Reference)
                {
                    var oldRef = (EntityId)datom.V;
                    if (!remappedIds.TryGetValue(oldRef, out var newRef))
                    {
                        tx.Add(datom);
                        continue;
                    }
                    tx.Add(new Datom(datom.Prefix, newRef));
                }
                // It's rare, but the Ref,UShort/String tuple type may include a ref that needs to be remapped
                else if (datom.Prefix.ValueTag == ValueTag.Tuple3_Ref_UShort_Utf8I)
                {
                    var (r, s, str) = (ValueTuple<EntityId, ushort, string>)datom.V;
                    if (!remappedIds.TryGetValue(r, out var newR))
                    {
                        tx.Add(datom);
                        continue;
                    }
                    var newTuple = (newR, s, str);
                    tx.Add(new Datom(datom.Prefix, newTuple));
                }
                // Otherwise just remap the E value
                else
                {
                    tx.Add(datom);
                }
            }
        }

        var result = await tx.Commit();
        return result[remappedIds[id]];
    }
}

public static partial class CollectionGroupLoaderExtensions
{
    
    /// <summary>
    /// Find the user collection for a given loadout
    /// </summary>
    public static Query<(EntityId CollectionId, string Name)> MutableCollections(this Loadout.ReadOnly loadout)
    {
        return Loadout.MutableCollections(loadout.Db.Connection, loadout);
    }
}
