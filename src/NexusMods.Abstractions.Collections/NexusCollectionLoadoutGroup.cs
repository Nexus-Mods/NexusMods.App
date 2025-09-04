using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
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

        // Retract the Nexus references in items so that this is no longer associated with the official collection
        var db = conn.Db;
        foreach (var item in conn.Query<EntityId>("SELECT Id FROM mdb_NexusCollectionItemLoadoutGroup(Db=>$Db) WHERE Parent = $Collection", new { Db = db, Collection = cloneId }))
        {
            var ent = NexusCollectionItemLoadoutGroup.Load(db, item);
            tx.Retract(item, NexusCollectionItemLoadoutGroup.Download, NexusCollectionItemLoadoutGroup.Download.Get(ent));
            tx.Retract(item, NexusCollectionItemLoadoutGroup.IsRequired, NexusCollectionItemLoadoutGroup.IsRequired.Get(ent));
            
            if (NexusCollectionReplicatedLoadoutGroup.Replicated.TryGetValue(ent, out var replicated))
                tx.Retract(item, NexusCollectionReplicatedLoadoutGroup.Replicated, replicated);
            
            if (NexusCollectionBundledLoadoutGroup.CollectionLibraryFileId.TryGetValue(ent, out var bundleLibraryFileId))
                tx.Retract(item, NexusCollectionBundledLoadoutGroup.CollectionLibraryFileId, bundleLibraryFileId);

            if (NexusCollectionBundledLoadoutGroup.BundleDownload.TryGetValue(ent, out var bundleDownload))
            {
                tx.Retract(item, NexusCollectionBundledLoadoutGroup.BundleDownload, bundleDownload);
                
                // We've now orphaned the bundled files, so we'll now create a download archive that contains the files this loadout group needs.

                // Create a new library item, 
                var libraryFile = new ManuallyCreatedArchive.New(tx, out var libraryFileId)
                {
                    Source = ManuallyCreatedArchive.CreationSource.CollectionBundled,
                    LibraryItem = new LibraryItem.New(tx, libraryFileId)
                    {
                        Name = "Bundled Files - " + ent.AsLoadoutItemGroup().AsLoadoutItem().Name,
                    }
                };
                // Mark this as an archive
                tx.Add(libraryFileId, LibraryArchive.Archive, Null.Instance);
                
                // Link the mod group to the archive
                tx.Add(item, LibraryLinkedLoadoutItem.LibraryItemId, libraryFileId);

                var added = new HashSet<Hash>();
                // Now link up all the required items
                foreach (var child in ent.AsLoadoutItemGroup().Children.OfTypeLoadoutItemWithTargetPath().OfTypeLoadoutFile())
                {
                    // Don't add duplicates
                    if (added.Contains(child.Hash))
                        continue;
                    
                    var name = child.AsLoadoutItemWithTargetPath().AsLoadoutItem().Name;
                    _ = new LibraryArchiveFileEntry.New(tx, out var fileId)
                    {
                        ParentId = libraryFileId,
                        Path = child.AsLoadoutItemWithTargetPath().TargetPath.Item3,
                        LibraryFile = new LibraryFile.New(tx, fileId)
                        {
                            Hash = child.Hash,
                            Size = child.Size,
                            FileName = name,
                            LibraryItem = new LibraryItem.New(tx, fileId)
                            {
                                Name = name,
                            },
                        },
                    };
                    added.Add(child.Hash);
                }
            }

        }
        
        await tx.Commit();
        return cloneId;
    }
}
