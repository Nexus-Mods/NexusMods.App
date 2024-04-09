using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Serialization.DataModel.Ids;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.DataModel.Attributes;

/// <summary>
/// MnemonicDB attributes for the DiskStateTree registry.
/// </summary>b
/// 
public class DiskStateTree
{
    /// <summary>
    /// The associated game id.
    /// </summary>
    public class Game() : Attribute<Game, GameDomain>(isIndexed: true, noHistory: true);
    
    /// <summary>
    /// The game's root folder
    /// </summary>
    public class Root() : Attribute<Root, AbsolutePath>(isIndexed: true, noHistory: true);
    
    /// <summary>
    /// The associated loadout id.
    /// </summary>
    public class LoadoutRevision() : Attribute<LoadoutRevision, IId>(isIndexed: true, noHistory: true);

    /// <summary>
    /// The state of the disk.
    /// </summary>
    public class DiskState() : Attribute<DiskState, NexusMods.Abstractions.DiskState.DiskStateTree>(noHistory: true);
}
