using Bannerlord.ModuleManager;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Games.MountAndBlade2Bannerlord.MnemonicDB;

public class DependentModuleMetadata
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

    public class Model(ITransaction tx) : Entity(tx)
    {
        public string MetaId
        {
            get => DependentModuleMetadata.MetaId.Get(this);
            init => DependentModuleMetadata.MetaId.Add(this, value);
        }
        
        public LoadType LoadType
        {
            get => DependentModuleMetadata.LoadType.Get(this);
            init => DependentModuleMetadata.LoadType.Add(this, value);
        }
        
        public bool IsOptional
        {
            get => DependentModuleMetadata.IsOptional.Get(this);
            init => DependentModuleMetadata.IsOptional.Add(this, value);
        }
        
        public bool IsIncompatible
        {
            get => DependentModuleMetadata.IsIncompatible.Get(this);
            init => DependentModuleMetadata.IsIncompatible.Add(this, value);
        }
        
        public ApplicationVersion Version
        {
            get => DependentModuleMetadata.Version.Get(this);
            init => DependentModuleMetadata.Version.Add(this, value);
        }
        
        public ApplicationVersion MinVersion
        {
            get => DependentModuleMetadata.MinVersion.Get(this);
            init => DependentModuleMetadata.MinVersion.Add(this, value);
        }
        
        public ApplicationVersion MaxVersion
        {
            get => DependentModuleMetadata.MaxVersion.Get(this);
            init => DependentModuleMetadata.MaxVersion.Add(this, value);
        }
        
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
