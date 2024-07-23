using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.DataModel.Attributes;

/// <summary>
/// MnemonicDB attributes for the DiskStateTree registry.
/// </summary>b
public partial class DiskState : IModelDefinition
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
    public static readonly ReferenceAttribute<Loadout> Loadout = new(Namespace, nameof(Loadout)) { IsIndexed = true, NoHistory = true };
    
    /// <summary>
    /// The associated transaction id.
    /// </summary>
    public static readonly TxIdAttribute TxId = new(Namespace, nameof(TxId)) { IsIndexed = true, NoHistory = true };

    /// <summary>
    /// The state of the disk.
    /// </summary>
    public static readonly DiskStateAttribute State = new(Namespace, nameof(State)) { NoHistory = true };
}

/// <summary>
///     A sibling of <see cref="DiskState"/>, but only for the initial state.
/// </summary>
/// <remarks>
///     We don't want to keep history in <see cref="DiskState"/> as that is only
///     supposed to hold the latest state, so in order to keep things clean,
///     we separated this out to the class.
///
///     This will also make cleaning out loadouts in MneumonicDB easier in the future.
/// </remarks>
public partial class InitialDiskState : IModelDefinition
{
    private static readonly string Namespace = "NexusMods.DataModel.InitialDiskStates";

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
    /// The state of the disk.
    /// </summary>
    public static readonly DiskStateAttribute State = new(Namespace, nameof(State)) { NoHistory = true };
}
