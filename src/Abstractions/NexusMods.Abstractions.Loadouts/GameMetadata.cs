using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// Metadata about a game installation. This model exists so that parts of the system can reference a single
/// MnemonicDb model, instead of a tuple of domain/store/path
/// </summary>
public partial class GameMetadata : IModelDefinition
{
    private const string Namespace = "NexusMods.Abstractions.Loadouts.GameMetadata";
    
    /// <summary>
    /// The game's domain.
    /// </summary>
    public static readonly GameDomainAttribute Domain = new(Namespace, "Domain");
    
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
    public static readonly ReferenceAttribute<Loadout> LastAppliedLoadout = new(Namespace, nameof(LastAppliedLoadout)) { IsOptional = true };
    
    /// <summary>
    /// The 'AsOf' transaction ID of the last applied loadout
    /// </summary>
    public static readonly ReferenceAttribute<Transaction> LastAppliedLoadoutTransaction = new(Namespace, nameof(LastAppliedLoadoutTransaction)) { IsOptional = true };
    
    /// <summary>
    /// The 'AsOf' transaction ID of the initial state when the game folder was first indexed
    /// </summary>
    public static readonly ReferenceAttribute<Transaction> InitialStateTransaction = new(Namespace, nameof(InitialStateTransaction)) { IsOptional = true };
    
    /// <summary>
    /// Current state of the game as seen on disk
    /// </summary>
    public static readonly BackReferenceAttribute<DiskStateEntry> DiskStateEntries = new(DiskStateEntry.Game);
    
    /// <summary>
    /// The last scanned transaction ID
    /// </summary>
    public static readonly ReferenceAttribute<Transaction> LastScannedTransaction = new(Namespace, nameof(LastScannedTransaction)) { IsOptional = true };
}
