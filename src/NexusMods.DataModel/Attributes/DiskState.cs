using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Serialization.DataModel.Ids;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Paths;

namespace NexusMods.DataModel.Attributes;

/// <summary>
/// MnemonicDB attributes for the DiskStateTree registry.
/// </summary>b
/// 
public static class DiskState
{
    
    /// <summary>
    /// The associated game id.
    /// </summary>
    public static readonly Attribute<GameDomain> Game = new("NexusMods.DataModel.DiskStateRegistry/Game", isIndexed: true, noHistory: true);
    
    /// <summary>
    /// The game's root folder
    /// </summary>
    public static readonly Attribute<AbsolutePath> Root = new("NexusMods.DataModel.DiskStateRegistry/Root", isIndexed: true, noHistory: true);
    
    /// <summary>
    /// The associated loadout id.
    /// </summary>
    public static readonly Attribute<IId> LoadoutRevision = new("NexusMods.DataModel.DiskStateRegistry/LoadoutRevision", isIndexed: true, noHistory: true);

    /// <summary>
    /// The state of the disk.
    /// </summary>
    public static readonly Attribute<NexusMods.Abstractions.DiskState.DiskStateTree> State = new("NexusMods.DataModel.DiskStateRegistry/State", noHistory: true);
    
    
    internal class Model(ITransaction tx) : AEntity(tx)
    {
        /// <summary>
        /// The associated game type
        /// </summary>
        public GameDomain Game
        {
            get => Attributes.DiskState.Game.Get(this);
            set => Attributes.DiskState.Game.Add(this, value);
        }
    
        /// <summary>
        /// The associated game root folder
        /// </summary>
        public AbsolutePath Root
        {
            get => Attributes.DiskState.Root.Get(this);
            set => Attributes.DiskState.Root.Add(this, value);
        }
    
        /// <summary>
        /// The associated loadout revision.
        /// </summary>
        public IId LoadoutRevision
        {
            get => Attributes.DiskState.LoadoutRevision.Get(this);
            set => Attributes.DiskState.LoadoutRevision.Add(this, value);
        }
    
        /// <summary>
        /// The state of the disk.
        /// </summary>
        public NexusMods.Abstractions.DiskState.DiskStateTree DiskState
        {
            get => State.Get(this);
            set => State.Add(this, value);
        }
    }
}
