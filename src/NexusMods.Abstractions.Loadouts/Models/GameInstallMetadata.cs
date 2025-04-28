using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// Metadata about a game installation. This model exists so that parts of the system can reference a single
/// MnemonicDb model, instead of a tuple of domain/store/path
/// </summary>
public partial class GameInstallMetadata : IModelDefinition
{
    private const string Namespace = "NexusMods.Loadouts.GameMetadata";

    /// <summary>
    /// The game's unique id.
    /// </summary>
    public static readonly GameIdAttribute GameId = new(Namespace, nameof(GameId)) { IsIndexed = true };
    
    /// <summary>
    /// User friendly name for the game.
    /// May be referred to from diagnostics, telemetry or otherwise.
    /// </summary>
    public static readonly StringAttribute Name = new(Namespace, nameof(Name));
    
    /// <summary>
    /// The name of the store the game is from
    /// </summary>
    public static readonly GameStoreAttribute Store = new(Namespace, "Store");
    
    /// <summary>
    /// The path to the game's installation directory.
    /// </summary>
    public static readonly StringAttribute Path = new(Namespace, "Path") {IsIndexed = true};
    
    /// <summary>
    /// The last applied loadout to this game state
    /// </summary>
    public static readonly ReferenceAttribute<Loadout> LastSyncedLoadout = new(Namespace, nameof(LastSyncedLoadout)) { IsOptional = true };
    
    /// <summary>
    /// The 'AsOf' transaction ID of the last applied loadout
    /// </summary>
    public static readonly ReferenceAttribute<Transaction> LastSyncedLoadoutTransaction = new(Namespace, nameof(LastSyncedLoadoutTransaction)) { IsOptional = true };
    
    /// <summary>
    /// The 'AsOf' transaction ID of the initial disk state when the game folder was first indexed
    /// </summary>
    public static readonly ReferenceAttribute<Transaction> InitialDiskStateTransaction = new(Namespace, nameof(InitialDiskStateTransaction)) { IsOptional = true };
    
    /// <summary>
    /// Current state of the game as seen on disk
    /// </summary>
    public static readonly BackReferenceAttribute<DiskStateEntry> DiskStateEntries = new(DiskStateEntry.Game);
    
    /// <summary>
    /// The last scanned disk state transaction ID
    /// </summary>
    public static readonly ReferenceAttribute<Transaction> LastScannedDiskStateTransaction = new(Namespace, nameof(LastScannedDiskStateTransaction)) { IsOptional = true };
    
    /// <summary>
    /// All the loadouts that are based on this game installation
    /// </summary>
    public static readonly BackReferenceAttribute<Loadout> Loadouts = new(Loadout.Installation);
}
