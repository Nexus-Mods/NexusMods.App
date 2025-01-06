using NexusMods.Games.GameHashes.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Games.GameHashes.Models;

public partial class SteamManifestEntry : IModelDefinition
{
    private const string Namespace = "NexusMods.Games.GameHashes.SteamManifestDefinition";
    
    /// <summary>
    /// The Steam Manifest id
    /// </summary>
    public static readonly SteamManifestIdAttribute ManifestId = new(Namespace, nameof(ManifestId)) { IsIndexed = true };
    
    /// <summary>
    /// The relative path to the file
    /// </summary>
    public static readonly RelativePathAttribute Path = new(Namespace, nameof(Path)) { IsIndexed = true };

    /// <summary>
    /// The Hash of the file
    /// </summary>
    public static readonly ReferenceAttribute<HashRelation> Hash = new(Namespace, nameof(Hash));
}
