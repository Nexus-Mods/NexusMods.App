using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Serialization.DataModel.Ids;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Paths;

namespace NexusMods.DataModel.Models;

internal class SavedDiskState(ITransaction tx) : AEntity(tx)
{
    /// <summary>
    /// The associated game type
    /// </summary>
    public GameDomain Game
    {
        get => Attributes.DiskStateTree.Game.Get(this);
        set => Attributes.DiskStateTree.Game.Add(this, value);
    }
    
    /// <summary>
    /// The associated game root folder
    /// </summary>
    public AbsolutePath Root
    {
        get => Attributes.DiskStateTree.Root.Get(this);
        set => Attributes.DiskStateTree.Root.Add(this, value);
    }
    
    /// <summary>
    /// The associated loadout revision.
    /// </summary>
    public IId LoadoutRevision
    {
        get => Attributes.DiskStateTree.LoadoutRevision.Get(this);
        set => Attributes.DiskStateTree.LoadoutRevision.Add(this, value);
    }
    
    /// <summary>
    /// The state of the disk.
    /// </summary>
    public DiskStateTree DiskState
    {
        get => Attributes.DiskStateTree.DiskState.Get(this);
        set => Attributes.DiskStateTree.DiskState.Add(this, value);
    }
}
