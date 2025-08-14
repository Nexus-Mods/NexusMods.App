using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Collections;

/// <summary>
/// A collection loadout group that was sourced from a NexusMods collection library file.
/// </summary>
[Include<CollectionGroup>]
public partial class NexusCollectionLoadoutGroup : IModelDefinition
{
    private const string Namespace = "NexusMods.Collections.NexusCollectionLoadoutGroup";

    /// <summary>
    /// The collection.
    /// </summary>
    public static readonly ReferenceAttribute<CollectionMetadata> Collection = new(Namespace, nameof(Collection)) { IsIndexed = true };

    /// <summary>
    /// The revision.
    /// </summary>
    public static readonly ReferenceAttribute<CollectionRevisionMetadata> Revision = new(Namespace, nameof(Revision)) { IsIndexed = true };

    /// <summary>
    /// The collection library file.
    /// </summary>
    public static readonly ReferenceAttribute<NexusModsCollectionLibraryFile> LibraryFile = new(Namespace, nameof(LibraryFile));

    public partial struct ReadOnly
    {
        public IEnumerable<NexusCollectionItemLoadoutGroup.ReadOnly> GetCollectionItems()
        {
            return Db
                .GetBackRefs(LoadoutItem.Parent, Id)
                .AsModels<NexusCollectionItemLoadoutGroup.ReadOnly>(Db)
                .Where(static model => model.IsValid());
        }
    }

    public static async Task<EntityId> MakeEditableLocalCollection(IConnection conn, EntityId collId, string newName)
    {
        var cloneId = await CollectionGroup.Clone(conn, collId);
        var cloneEnt = Load(conn.Db, cloneId);
                
        using var tx = conn.BeginTransaction();
        // Remap the name
        tx.Add(cloneId, LoadoutItem.Name, newName);
        // Make it editable
        tx.Add(cloneId, CollectionGroup.IsReadOnly, false);
        // Retract the Nexus references as this is no longer associated with the official collection
        tx.Retract(cloneId, RevisionId, RevisionId.Get(cloneEnt));
        tx.Retract(cloneId, CollectionId, CollectionId.Get(cloneEnt));
        tx.Retract(cloneId, LibraryFileId, LibraryFileId.Get(cloneEnt));
        await tx.Commit();
        return cloneId;
    }
}
