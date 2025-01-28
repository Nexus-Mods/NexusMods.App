using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Games.FileHashes.Models;

/// <summary>
/// A human firendly name for a game version, such as "1.5" insead of "1.5.4.3343224-release"
/// </summary>
public partial class GameVersion : IModelDefinition
{
    private const string Namespace = "NexusMods.Abstractions.Games.FileHashes.GameVersionMapping";

    /// <summary>
    /// The gameId of his game version
    /// </summary>
    public static readonly GameIdAttribute GameId = new(Namespace, nameof(GameId)) { IsIndexed = true };
    
    /// <summary>
    /// The actual name of the game version, like 1.5.4.3343224-release
    /// </summary>
    public static readonly StringAttribute Name = new(Namespace, nameof(Name)) { IsIndexed = true };
    
    /// <summary>
    /// The alias of the game version, like 1.5.4 vs 1.5.4.3343224-release
    /// </summary>
    public static readonly StringAttribute Alias = new(Namespace, nameof(Alias)) { IsIndexed = true, IsOptional = true };
    
    /// <summary>
    /// The gog builds that are part of this version
    /// </summary>
    public static readonly ReferencesAttribute<GogBuild> GogBuilds = new(Namespace, nameof(GogBuilds));
    
    /// <summary>
    /// The Steam manifests that are part of this version
    /// </summary>
    public static readonly ReferencesAttribute<SteamManifest> SteamManifests = new(Namespace, nameof(SteamManifests));
}
