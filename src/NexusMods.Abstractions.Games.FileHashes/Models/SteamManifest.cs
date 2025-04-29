using NexusMods.Abstractions.Games.FileHashes.Attributes.Steam;
using NexusMods.Abstractions.Steam.Values;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Games.FileHashes.Models;

public partial class SteamManifest : IModelDefinition
{
    private const string Namespace = "NexusMods.Abstractions.Games.FileHashes.SteamManifest";

    /// <summary>
    /// The app ID of this manifest
    /// </summary>
    public static readonly AppIdAttribute AppId = new(Namespace, nameof(AppId)) { IsIndexed = true };
    
    /// <summary>
    /// The depot ID of this manifest
    /// </summary>
    public static readonly DepotIdAttribute DepotId = new(Namespace, nameof(DepotId)) { IsIndexed = true };
    
    /// <summary>
    /// The manifest ID of this manifest
    /// </summary>
    public static readonly ManifestIdAttribute ManifestId = new(Namespace, nameof(ManifestId)) { IsIndexed = true };
    
    /// <summary>
    /// The branch of this manifest
    /// </summary>
    public static readonly StringAttribute Name = new(Namespace, nameof(Name)) { IsIndexed = true };
    
    /// <summary>
    /// The files in this manifest
    /// </summary>
    public static readonly ReferencesAttribute<PathHashRelation> Files = new(Namespace, nameof(Files));
}
