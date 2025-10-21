using NexusMods.Abstractions.Games.FileHashes.Attributes.Gog;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using OperatingSystem = NexusMods.Abstractions.Games.FileHashes.Values.OperatingSystem;

namespace NexusMods.Abstractions.Games.FileHashes.Models;

/// <summary>
/// Metadata for a GOG build.
/// </summary>
public partial class GogBuild : IModelDefinition
{
    private const string Namespace = "NexusMods.Abstractions.Games.FileHashes.GogBuild";
    
    /// <summary>
    /// The GOG build ID.
    /// </summary>
    public static readonly BuildIdAttribute BuildId = new(Namespace, nameof(BuildId)) { IsIndexed = true, IsOptional = true };
    
    /// <summary>
    /// The GOG product ID.
    /// </summary>
    public static readonly ProductIdAttribute ProductId = new(Namespace, nameof(ProductId)) { IsIndexed = true };
    
    /// <summary>
    /// The unique manifest ID for this build.
    /// </summary>
    public static readonly StringAttribute ManifestId = new(Namespace, nameof(ManifestId)) { IsIndexed = true };
    
    /// <summary>
    /// The Operating System the build is for.
    /// </summary>
    public static readonly EnumByteAttribute<OperatingSystem> OperatingSystem = new(Namespace, nameof(OperatingSystem));
    
    /// <summary>
    /// The version string of the GOG build.
    /// </summary>
    public static readonly StringAttribute VersionName = new(Namespace, nameof(Version)) { IsIndexed = true };
    
    /// <summary>
    /// Various tags for the build
    /// </summary>
    public static readonly StringsAttribute Tags = new(Namespace, nameof(Tags));
    
    /// <summary>
    /// True if the build is public, false if it is private.
    /// </summary>
    public static readonly BooleanAttribute Public = new(Namespace, nameof(Public));
    
    /// <summary>
    /// (Deprecated) The files in the GOG build, use Depots instead.
    /// </summary>
    public static readonly ReferencesAttribute<PathHashRelation> Files = new(Namespace, nameof(Files));
    
    /// <summary>
    /// All the depots in this build
    /// </summary>
    public static readonly ReferencesAttribute<GogDepot> Depots = new(Namespace, nameof(Depots));
}
