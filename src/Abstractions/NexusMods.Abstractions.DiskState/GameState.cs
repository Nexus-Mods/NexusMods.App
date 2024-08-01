using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.DiskState;

/// <summary>
/// Actual game state, as seen on disk
/// </summary>
[Include<DiskStateRoot>]
public partial class GameState : IModelDefinition
{
    public const string Namespace = "NexusMods.Abstractions.DiskState.GameState";
    
    /// <summary>
    /// The game this state is for
    /// </summary>
    public static readonly ReferenceAttribute<GameMetadata> Game = new(Namespace, nameof(Game));
}
