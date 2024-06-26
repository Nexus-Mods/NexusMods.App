using Bannerlord.LauncherManager.Models;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Games.MountAndBlade2Bannerlord.MnemonicDB;

public partial class ModuleInfoExtended : IModelDefinition
{
    private const string Namespace = "NexusMods.Games.MountAndBlade2Bannerlord.MnemonicDB.ModuleInfoExtended";
    
    /// <summary>
    /// Path of the module.
    /// </summary>
    public static readonly RelativePathAttribute Path = new(Namespace, nameof(Path));
    
    /// <summary>
    /// The module ID.
    /// </summary>
    public static readonly StringAttribute ModuleId = new(Namespace, nameof(ModuleId)) {IsIndexed = true};
    
    /// <summary>
    /// The module name.
    /// </summary>
    public static readonly StringAttribute Name = new(Namespace, nameof(Name));
    
    /// <summary>
    /// True if the module is official.
    /// </summary>
    public static readonly BooleanAttribute IsOfficial = new(Namespace, nameof(IsOfficial));
    
    /// <summary>
    /// Application version of the module.
    /// </summary>
    public static readonly ApplicationVersionAttribute Version = new(Namespace, nameof(Version));
    
    /// <summary>
    /// True if the module is a singleplayer module.
    /// </summary>
    public static readonly BooleanAttribute IsSingleplayerModule = new(Namespace, nameof(IsSingleplayerModule));
    
    /// <summary>
    /// True if the module is a multiplayer module.
    /// </summary>
    public static readonly BooleanAttribute IsMultiplayerModule = new(Namespace, nameof(IsMultiplayerModule));
    
    /// <summary>
    /// True if this is a server module.
    /// </summary>
    public static readonly BooleanAttribute IsServerModule = new(Namespace, nameof(IsServerModule));
    
    /// <summary>
    /// Sub-modules of the module.
    /// </summary>
    public static readonly ReferencesAttribute<SubModuleInfo> SubModules = new(Namespace, nameof(SubModules));
    
    /// <summary>
    /// Modules that this module depends on.
    /// </summary>
    public static readonly ReferencesAttribute<DependentModule> DependentModules = new(Namespace, nameof(DependentModules));
    
    /// <summary>
    /// Modules that should be loaded after this module.
    /// </summary>
    public static readonly ReferencesAttribute<ModuleInfoExtended> ModulesToLoadAfterThis = new(Namespace, nameof(ModulesToLoadAfterThis));
    
    /// <summary>
    /// Modules that are incompatible with this module.
    /// </summary>
    public static readonly ReferencesAttribute<ModuleInfoExtended> IncompatibleModules = new(Namespace, nameof(IncompatibleModules));
    
    /// <summary>
    /// Metadata of dependent modules.
    /// </summary>
    public static readonly ReferencesAttribute<DependentModuleMetadata> DependentModuleMetadatas = new(Namespace, nameof(DependentModuleMetadatas));
    
    /// <summary>
    /// Url of the module's page.
    /// </summary>
    public static readonly UriAttribute Url = new(Namespace, nameof(Url));


    
    /// <summary>
    /// Adds the information of the module to the transaction.
    /// </summary>
    public static EntityId AddTo(ModuleInfoExtendedWithPath info, ITransaction tx)
    {
        var id = tx.TempId();
        tx.Add(id, Path, info.Path);
        tx.Add(id, ModuleId, info.Id);
        tx.Add(id, Name, info.Name);
        tx.Add(id, IsOfficial, info.IsOfficial);
        tx.Add(id, Version, info.Version);
        tx.Add(id, IsSingleplayerModule, info.IsSingleplayerModule);
        tx.Add(id, IsMultiplayerModule, info.IsMultiplayerModule);
        tx.Add(id, IsServerModule, info.IsServerModule);
        
        foreach (var subModule in info.SubModules)
        {
            var subModuleId = tx.TempId();
            tx.Add(id, SubModules, subModuleId);
            tx.Add(subModuleId, SubModuleInfo.Name, subModule.Name);
            tx.Add(subModuleId, SubModuleInfo.DLLName, subModule.DLLName);
            foreach (var assembly in subModule.Assemblies)
                tx.Add(subModuleId, SubModuleInfo.Assemblies, assembly);
        }
        
        foreach (var dependentModule in info.DependentModules)
        {
            var dependentModuleId = tx.TempId();
            tx.Add(id, DependentModules, dependentModuleId);
            tx.Add(dependentModuleId, DependentModule.ModuleId, dependentModule.Id);
            tx.Add(dependentModuleId, DependentModule.Version, dependentModule.Version);
            tx.Add(dependentModuleId, DependentModule.IsOptional, dependentModule.IsOptional);
        }


        foreach (var meta in info.DependentModuleMetadatas)
        {
            var metaId = tx.TempId();
            tx.Add(id, DependentModuleMetadatas, metaId);
            tx.Add(metaId, DependentModuleMetadata.MetaId, meta.Id);
            tx.Add(metaId, DependentModuleMetadata.LoadType, meta.LoadType);
            tx.Add(metaId, DependentModuleMetadata.IsOptional, meta.IsOptional);
            tx.Add(metaId, DependentModuleMetadata.IsIncompatible, meta.IsIncompatible);
            tx.Add(metaId, DependentModuleMetadata.Version, meta.Version);
            tx.Add(metaId, DependentModuleMetadata.MinVersion, meta.VersionRange.Min);
            tx.Add(metaId, DependentModuleMetadata.MaxVersion, meta.VersionRange.Max);
        }

        return id;
    }
    
    public partial struct ReadOnly
    {
        /// <summary>
        /// Convert a MneumonicDB entity to a ModuleInfoExtendedWithPath.
        /// </summary>
        public ModuleInfoExtendedWithPath FromEntity()
        {
            return new ModuleInfoExtendedWithPath
            {
                Id = ModuleId,
                Name = Name,
                IsOfficial = IsOfficial,
                Version = Version,
                IsSingleplayerModule = IsSingleplayerModule,
                IsMultiplayerModule = IsMultiplayerModule,
                IsServerModule = IsServerModule,
                DependentModules = DependentModules.Select(dm => dm.ToDependentModule()).ToList(),
                DependentModuleMetadatas = DependentModuleMetadatas.Select(dm => dm.ToModuleManagerDependentModuleMetadata()).ToList(),
            };
        }
        
    }
}
