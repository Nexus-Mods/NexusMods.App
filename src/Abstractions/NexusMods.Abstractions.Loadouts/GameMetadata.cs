using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// Metadata about a game installation. This model exists so that parts of the system can reference a single
/// MnemonicDb model, instead of a tuple of domain/store/path
/// </summary>
public partial class GameMetadata : IModelDefinition
{
    // Note: this namespace doesn't match the C# namespace due to it being moved 
    // after creation. This could possibly me migrated in the future but there's noting
    // wrong keeping it as-is
    private const string Namespace = "NexusMods.DataModel.GameRegistry.GameMetadata";
    
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
