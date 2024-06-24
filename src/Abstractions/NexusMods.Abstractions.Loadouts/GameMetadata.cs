using NexusMods.MnemonicDB.Abstractions.Attributes;
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
    public static readonly StringAttribute Domain = new(Namespace, "Domain");
    
    /// <summary>
    /// The name of the store the game is from
    /// </summary>
    public static readonly StringAttribute Store = new(Namespace, "Store");
    
    /// <summary>
    /// The path to the game's installation directory.
    /// </summary>
    public static readonly StringAttribute Path = new(Namespace, "Path") {IsIndexed = true};
    
}
