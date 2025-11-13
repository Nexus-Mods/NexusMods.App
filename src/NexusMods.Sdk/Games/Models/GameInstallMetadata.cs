using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Sdk.Loadouts;
using NexusMods.Sdk.NexusModsApi;

namespace NexusMods.Sdk.Games;

/// <summary>
/// Metadata about a game installation. This model exists so that parts of the system can reference a single
/// MnemonicDb model, instead of a tuple of domain/store/path
/// </summary>
public partial class GameInstallMetadata : IModelDefinition
{
    private const string Namespace = "NexusMods.Loadouts.GameMetadata";

    // TODO: replace nexus mods game id with actual game id
    /// <summary>
    /// The game's unique id.
    /// </summary>
    public static readonly NexusModsGameIdAttribute GameId = new(Namespace, nameof(GameId)) { IsIndexed = true };

    /// <summary>
    /// The name of the store the game is from
    /// </summary>
    public static readonly GameStoreAttribute Store = new(Namespace, nameof(Store));

    /// <summary>
    /// The path to the game's installation directory.
    /// </summary>
    public static readonly StringAttribute Path = new(Namespace, nameof(Path)) { IsIndexed = true };

    // TODO: remove
    /// <summary>
    /// User friendly name for the game.
    /// May be referred to from diagnostics, telemetry or otherwise.
    /// </summary>
    [Obsolete("to be removed")]
    public static readonly StringAttribute Name = new(Namespace, nameof(Name));

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
    /// The last scanned disk state transaction ID
    /// </summary>
    public static readonly ReferenceAttribute<Transaction> LastScannedDiskStateTransaction = new(Namespace, nameof(LastScannedDiskStateTransaction)) { IsOptional = true };
}
