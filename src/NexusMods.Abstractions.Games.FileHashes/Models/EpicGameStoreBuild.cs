using NexusMods.Abstractions.EpicGameStore.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using BuildIdAttribute = NexusMods.Abstractions.EpicGameStore.Attributes.BuildIdAttribute;

namespace NexusMods.Abstractions.Games.FileHashes.Models;

public partial class EpicGameStoreBuild : IModelDefinition
{
    private const string Namespace = "NexusMods.Stores.EpicGameStore.EpicGameStoreBuild";
    
    /// <summary>
    /// The build ID of the Epic Game Store build.
    /// </summary>
    public static readonly BuildIdAttribute BuildId = new(Namespace, nameof(BuildId)) { IsIndexed = true };
    
    /// <summary>
    /// The Item ID of the Epic Game Store build.
    /// </summary>
    public static readonly ItemIdAttribute ItemId = new(Namespace, nameof(ItemId));
    
    /// <summary>
    /// The files in this build
    /// </summary>
    public static readonly ReferencesAttribute<PathHashRelation> Files = new(Namespace, nameof(Files));
    
    /// <summary>
    /// The application name of the Epic Game Store build.
    /// </summary>
    public static readonly StringAttribute AppName = new(Namespace, nameof(AppName));
    
    /// <summary>
    /// The manifest hash of the Epic Game Store build.
    /// </summary>
    public static readonly ManifestHashAttribute ManifestHash = new(Namespace, nameof(ManifestHash)) { IsIndexed = true };
    
    /// <summary>
    /// The label name of the Epic Game Store build.
    /// </summary>
    public static readonly StringAttribute LabelName = new(Namespace, nameof(LabelName));
    
    /// <summary>
    /// The build version of the Epic Game Store build.
    /// </summary>
    public static readonly StringAttribute BuildVersion = new(Namespace, nameof(BuildVersion));
    
    /// <summary>
    /// The date and time when the build was created.
    /// </summary>
    public static readonly TimestampAttribute CreatedAt = new(Namespace, nameof(CreatedAt));
    
    /// <summary>
    /// The date and time when the build was last updated.
    /// </summary>
    public static readonly TimestampAttribute UpdatedAt = new(Namespace, nameof(UpdatedAt));
}
