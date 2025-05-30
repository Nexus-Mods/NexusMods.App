using NexusMods.Abstractions.Games.FileHashes.Models;
using NexusMods.Backend.Stores.EpicGameStore.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
namespace NexusMods.Backend.Stores.EpicGameStore.Models;

public partial class EpicGameStoreBuild : IModelDefinition
{
    private const string Namespace = "NexusMods.Stores.EpicGameStore.EpicGameStoreBuild";
    
    /// <summary>
    /// The build ID of the Epic Game Store build.
    /// </summary>
    public static readonly BuildIdAttribute BuildId = new(Namespace, nameof(BuildId));
    
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
