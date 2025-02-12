using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using OperatingSystem = NexusMods.Abstractions.Games.FileHashes.Values.OperatingSystem;

namespace NexusMods.Abstractions.Games.FileHashes.Models;

/// <summary>
/// A version definition that maps a human firendly name to other important data such as
/// store ids and operating system
/// </summary>
public partial class VersionDefinition : IModelDefinition
{
    private const string Namespace = "NexusMods.Abstractions.Games.FileHashes.VersionDefinition";

    /// <summary>
    /// The human friendly name of the version
    /// </summary>
    public static readonly StringAttribute Name = new(Namespace, nameof(Name)) { IsIndexed = true };
    
    /// <summary>
    /// The operating system this version is for
    /// </summary>
    public static readonly EnumByteAttribute<OperatingSystem> OperatingSystem = new(Namespace, nameof(OperatingSystem)) { IsIndexed = true };
    
    /// <summary>
    /// The game id this version is for
    /// </summary>
    public static readonly GameIdAttribute GameId = new(Namespace, nameof(GameId)) { IsIndexed = true };
    
    /// <summary>
    /// The gog build ids for this version
    /// </summary>
    public static readonly StringsAttribute GOG = new(Namespace, nameof(GOG)) { IsIndexed = true };
    
    /// <summary>
    /// The associated Steam ManifestIDs for this version
    /// </summary>
    public static readonly StringsAttribute Steam = new(Namespace, nameof(Steam)) { IsIndexed = true };
    
    /// <summary>
    /// The resolved gog builds for this version (if they exist)
    /// </summary>
    public static readonly ReferencesAttribute<GogBuild> GogBuilds = new(Namespace, nameof(GogBuilds));
    
    /// <summary>
    /// The resolved steam manfiests builds for this version (if they exist)
    /// </summary>
    public static readonly ReferencesAttribute<SteamManifest> SteamManifests = new(Namespace, nameof(SteamManifests));
}
