using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.Models;

/// <summary>
/// Represents the RedMod load order
/// </summary>
[PublicAPI]
[Include<Abstractions.Loadouts.SortOrder>]
public partial class RedModSortOrder : IModelDefinition
{
    private const string Namespace = "NexusMods.Games.RedEngine.Cyberpunk2077.RedModSortOrder";
    
    /// <summary>
    /// Marker attribute for querying the model, while waiting for advanced db queries
    /// Needs to be explicitly set to true on new model creation
    /// </summary>
    public static readonly MarkerAttribute Marker = new(Namespace, nameof(Marker)); 
}
