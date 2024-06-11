using Bannerlord.ModuleManager;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Games.MountAndBlade2Bannerlord.MnemonicDB;

public partial class DependentModuleMetadata : IModelDefinition
{
    private const string Namespace = "NexusMods.Games.MountAndBlade2Bannerlord.MnemonicDB.DependentModuleMetadata";
    
    /// <summary>
    /// The metadata ID.
    /// </summary>
    public static readonly StringAttribute MetaId = new(Namespace, nameof(MetaId)) {IsIndexed = true};
    
    /// <summary>
    /// The LoadType of the metadata.
    /// </summary>
    public static readonly EnumAttribute<LoadType> LoadType = new(Namespace, nameof(LoadType));
    
    /// <summary>
    /// True if the depenency is optional.
    /// </summary>
    public static readonly BooleanAttribute IsOptional = new(Namespace, nameof(IsOptional));
    
    /// <summary>
    /// True if the dependency is incompatible.
    /// </summary>
    public static readonly BooleanAttribute IsIncompatible = new(Namespace, nameof(IsIncompatible));
    
    /// <summary>
    /// The application version of the dependency.
    /// </summary>
    public static readonly ApplicationVersionAttribute Version = new(Namespace, nameof(Version));
    
    /// <summary>
    /// Minimum application version of the dependency.
    /// </summary>
    public static readonly ApplicationVersionAttribute MinVersion = new(Namespace, nameof(MinVersion));
    
    /// <summary>
    /// Maximum application version of the dependency.
    /// </summary>
    public static readonly ApplicationVersionAttribute MaxVersion = new(Namespace, nameof(MaxVersion));

    public partial struct ReadOnly
    {
        
        public Bannerlord.ModuleManager.DependentModuleMetadata ToModuleManagerDependentModuleMetadata()
        {
            return new()
            {
                Id = MetaId,
                LoadType = LoadType,
                IsOptional = IsOptional,
                IsIncompatible = IsIncompatible,
                Version = Version,
                VersionRange = new ApplicationVersionRange(MinVersion, MaxVersion),
            };
        }
        
    }
}
