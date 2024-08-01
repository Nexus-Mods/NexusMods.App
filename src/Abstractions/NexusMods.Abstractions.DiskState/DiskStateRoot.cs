using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.DiskState;

/// <summary>
/// Root class for all disk state trees in the system
/// </summary>
public partial class DiskStateRoot : IModelDefinition
{
    private const string Namespace = "NexusMods.DataModel.DiskStateroot";
    
    /// <summary>
    /// The entries in the disk state
    /// </summary>
    public static readonly BackReferenceAttribute<DiskStateEntry> Entries = new(DiskStateEntry.Root);
    
    /// <summary>
    /// Temp
    /// </summary>
    public static readonly ReferenceAttribute<GameMetadata> GameInstallation = new(Namespace, nameof(GameInstallation));


    /// <summary>
    /// Update the data associated with the given root, deletes entries that are not in the given list and
    /// adds new entries that are not in the database yet.
    /// </summary>
    /// <param name="db"></param>
    /// <param name="tx"></param>
    /// <param name="root"></param>
    /// <param name="entries"></param>
    public static void Update(IDb db, ITransaction tx, DiskStateRootId root, IEnumerable<DiskStateEntry.ReadOnly> entries)
    {
        var existing = new Dictionary<GamePath, DiskStateEntry.ReadOnly>();
        {
            var existingEntries = db.Datoms(DiskStateEntry.Root, root);
            foreach (var datom in existingEntries)
            {
                var entry = DiskStateEntry.Load(db, datom.E);
                if (!entry.IsValid())
                    throw new InvalidDataException();
                existing[entry.Path] = entry;
            }
        }
        
        var indexed = entries.ToDictionary(e => e.Path);
        foreach (var newEntry in indexed.Values)
        {
            // Found a match
            if (existing.TryGetValue(newEntry.Path, out var found))
            {
                if (found.Hash != newEntry.Hash) 
                    tx.Add(found.Id, DiskStateEntry.Hash, newEntry.Hash);
                if (found.Size != newEntry.Size)
                    tx.Add(found.Id, DiskStateEntry.Size, newEntry.Size);
                if (found.LastModified != newEntry.LastModified)
                    tx.Add(found.Id, DiskStateEntry.LastModified, newEntry.LastModified);
            }
            // No match, so create a new entry
            else
            {
                _ = new DiskStateEntry.New(tx, tx.TempId(DiskStateEntry.EntryPartition))
                {
                    Path = newEntry.Path,
                    Hash = newEntry.Hash,
                    Size = newEntry.Size,
                    LastModified = newEntry.LastModified,
                    RootId = root,
                };
            }
        }
        
        foreach (var existingEntry in existing.Values)
        {
            if (indexed.ContainsKey(existingEntry.Path)) 
                continue;
            tx.Retract(existingEntry.Id, DiskStateEntry.Path, existingEntry.Path);
            tx.Retract(existingEntry.Id, DiskStateEntry.Hash, existingEntry.Hash);
            tx.Retract(existingEntry.Id, DiskStateEntry.Size, existingEntry.Size);
            tx.Retract(existingEntry.Id, DiskStateEntry.LastModified, existingEntry.LastModified);
            tx.Retract(existingEntry.Id, DiskStateEntry.Root, root);
        }
    }
    
    public partial struct ReadOnly
    {
        public DiskStateTree ToTree(GameInstallation installation)
        {
            var register = installation.LocationsRegister;
            var tree = new List<KeyValuePair<GamePath, DiskStateEntry.ReadOnly>>();
            foreach (var entry in Entries)
            {
                tree.Add(KeyValuePair.Create(entry.Path, entry));
            }
            return DiskStateTree.Create(tree);
        }
    }
}

