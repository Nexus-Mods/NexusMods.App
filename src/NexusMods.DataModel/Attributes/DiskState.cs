using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Serialization.DataModel.Ids;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Paths;

namespace NexusMods.DataModel.Attributes;

/// <summary>
/// MnemonicDB attributes for the DiskStateTree registry.
/// </summary>b
/// 
public static class DiskState
{
    private static readonly string Namespace = "NexusMods.DataModel.DiskStateRegistry";
    
    /// <summary>
    /// The associated game id.
    /// </summary>
    public static readonly GameDomainAttribute Game = new(Namespace, nameof(Game)) { IsIndexed = true, NoHistory = true };
    
    /// <summary>
    /// The game's root folder. Stored as a string, since AbsolutePaths require an IFileSystem, and we don't know or care
    /// what filesystem is being used when reading/writing the data from the database.
    /// </summary>
    public static readonly StringAttribute Root = new(Namespace, nameof(Root)) { IsIndexed = true, NoHistory = true };
    
    /// <summary>
    /// The associated loadout id.
    /// </summary>
    public static readonly IIdAttribute LoadoutRevision = new(Namespace, nameof(LoadoutRevision)) { IsIndexed = true, NoHistory = true };

    /// <summary>
    /// The state of the disk.
    /// </summary>
    public static readonly DiskStateAttribute State = new(Namespace, nameof(State)) { NoHistory = true };
    
    
    [PublicAPI]
    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    internal class Model(ITransaction tx) : Entity(tx)
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
        public string Root
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
