using System.Runtime.InteropServices;
using DynamicData.Kernel;
using NexusMods.HyperDuck;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
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
                                              WITH RECURSIVE ChildLoadoutItems (Id) AS
                                              (SELECT $Id
                                               UNION
                                               SELECT Id FROM (SELECT Id, Parent FROM mdb_LoadoutItem(Db=>$Db)
                                                               UNION ALL
                                                               SELECT Id, ParentEntity FROM mdb_SortOrder(Db=>$Db)
                                                               UNION ALL
                                                               SELECT Id, ParentSortOrder FROM mdb_SortOrderItem(Db=>$Db))
                                               WHERE Parent in (SELECT Id FROM ChildLoadoutItems))
                                              SELECT DISTINCT Id FROM ChildLoadoutItems
                                              """;
    
    /// <summary>
    /// Performs a deep clone of the collection, and includes all child loadout items and sortable items and lists
    /// </summary>
    public static async Task<EntityId> Clone(IConnection conn, EntityId id)
    {
        Span<byte> refScratch = stackalloc byte[8];
        using var writer = new PooledMemoryBufferWriter();

        var basisDb = conn.Db;

        Dictionary<EntityId, EntityId> remappedIds = new();
        var query = conn.Query<EntityId>(CollectionEntities, new { Db = basisDb, Id = id});
        using var tx = conn.BeginTransaction();
        foreach (var itemId in query)
        {
            remappedIds.TryAdd(itemId, tx.TempId());
        }

        foreach (var (oldId, newId) in remappedIds)
        {
            var entity = basisDb.Get(oldId);
            foreach (var datom in entity)
            {
                // Remap the value part of references
                if (datom.Prefix.ValueTag == ValueTag.Reference)
                {
                    var oldRef = EntityId.From(UInt64Serializer.Read(datom.ValueSpan));
                    if (!remappedIds.TryGetValue(oldRef, out var newRef))
                    {
                        tx.Add(newId, datom.A, datom.Prefix.ValueTag, datom.ValueSpan);
                        continue;
                    }
                    MemoryMarshal.Write(refScratch, newRef);
                    tx.Add(newId, datom.A, datom.Prefix.ValueTag, refScratch);
                }
                // It's rare, but the Ref,UShort/String tuple type may include a ref that needs to be remapped
                else if (datom.Prefix.ValueTag == ValueTag.Tuple3_Ref_UShort_Utf8I)
                {
                    var (r, s, str) = Tuple3_Ref_UShort_Utf8I_Serializer.Read(datom.ValueSpan);
                    if (!remappedIds.TryGetValue(r, out var newR))
                    {
                        tx.Add(newId, datom.A, datom.Prefix.ValueTag, datom.ValueSpan);
                        continue;
                    }
                    writer.Reset();
                    var newTuple = (newR, s, str);
                    Tuple3_Ref_UShort_Utf8I_Serializer.Write(newTuple, writer);
                    tx.Add(newId, datom.A, datom.Prefix.ValueTag, writer.GetWrittenSpan());
                }
                // Otherwise just remap the E value
                else
                {
                    tx.Add(newId, datom.A, datom.Prefix.ValueTag, datom.ValueSpan);
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
