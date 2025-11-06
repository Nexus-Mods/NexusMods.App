using NexusMods.Abstractions.GameLocators;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Sdk.Games;

namespace NexusMods.StandardGameLocators;

/// <summary>
/// Used to store information about manually added games.
/// </summary>
public partial class ManuallyAddedGame : IModelDefinition
{
    private const string Namespace = "NexusMods.StandardGameLocators.ManuallyAddedGame";

    /// <summary>
    /// The game domain this game install belongs to.
    /// </summary>
    public static readonly GameIdAttribute GameId = new(Namespace, nameof(GameId)) { IsIndexed = true };

    /// <summary>
    /// The version of the game.
    /// </summary>
    public static readonly StringAttribute Version = new(Namespace, nameof(Version));

    /// <summary>
    /// The path to the game install.
    /// </summary>
    public static readonly StringAttribute Path = new(Namespace, nameof(Path)) { IsIndexed = true };
    
    public partial struct ReadOnly : IGameLocatorResultMetadata
    {
        /// <inheritdoc />
        public IEnumerable<LocatorId> ToLocatorIds() => [LocatorId.From("NONE")];

        /// <inheritdoc/>
        public ILinuxCompatibilityDataProvider? LinuxCompatibilityDataProvider => null;
    }
}
