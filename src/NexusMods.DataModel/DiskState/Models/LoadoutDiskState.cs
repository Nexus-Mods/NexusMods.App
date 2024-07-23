using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;

namespace NexusMods.DataModel.DiskState.Models;

/// <summary>
/// Disk state for a loadout.
/// </summary>
[Include<DiskState>]
public partial class LoadoutDiskState : IModelDefinition
{
    private const string Namespace = "NexusMods.DataModel.DiskState.LoadoutDiskState";
    
    /// <summary>
    /// The associated loadout id.
    /// </summary>
    public static readonly ReferenceAttribute<Loadout> Loadout = new(Namespace, nameof(Loadout)) { IsIndexed = true, NoHistory = true };
    
    /// <summary>
    /// The associated transaction id.
    /// </summary>
    public static readonly TxIdAttribute TxId = new(Namespace, nameof(TxId)) { IsIndexed = true, NoHistory = true };
    
}

public static class LoadoutDiskStateHelpers
{
    /// <summary>
    /// Update the state of the disk state to match the given tree
    /// </summary>
    public static void Update(this LoadoutDiskState.ReadOnly readOnly, ITransaction tx, DiskStateTree tree)
    {
        
        tx.Add(readOnly.Id, LoadoutDiskState.Loadout, readOnly.LoadoutId);
        tx.Add(readOnly.Id, LoadoutDiskState.TxId, readOnly.TxId);

        throw new NotImplementedException();

        /*
        foreach (var (prev, current) in tree.GetAllDescendentFiles().Merge(readOnly.AsDiskState().Entries, (a, b) => a.Path.CompareTo(b.Path)))
        {

        }
        */

    }

    /// <summary>
    /// Add all the entries in the tree to the disk state
    /// </summary>
    public static void AddAll(this DiskState.New diskState, ITransaction tx, DiskStateTree tree)
    {
        foreach (var entry in tree.GetAllDescendentFiles())
        {
            _ = new DiskStateEntry.New(tx)
            {
                DiskStateId = diskState.Id,
                Path = entry.Item.GamePath,
                Hash = entry.Item.Value.Hash,
                LastModified = entry.Item.Value.LastModified,
            };
        }
    }
}
