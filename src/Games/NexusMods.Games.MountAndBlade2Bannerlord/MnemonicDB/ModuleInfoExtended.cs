using Bannerlord.LauncherManager.Models;
using Bannerlord.ModuleManager;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;

namespace NexusMods.Games.MountAndBlade2Bannerlord.MnemonicDB;

public static class ModuleInfoExtended
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
    public static readonly ReferencesAttribute SubModules = new(Namespace, nameof(SubModules));
    
    /// <summary>
    /// Modules that this module depends on.
    /// </summary>
    public static readonly ReferencesAttribute DependentModules = new(Namespace, nameof(DependentModules));
    
    /// <summary>
    /// Modules that should be loaded after this module.
    /// </summary>
    public static readonly ReferencesAttribute ModulesToLoadAfterThis = new(Namespace, nameof(ModulesToLoadAfterThis));
    
    /// <summary>
    /// Modules that are incompatible with this module.
    /// </summary>
    public static readonly ReferencesAttribute IncompatibleModules = new(Namespace, nameof(IncompatibleModules));
    
    /// <summary>
    /// Metadata of dependent modules.
    /// </summary>
    public static readonly ReferencesAttribute DependentModuleMetadatas = new(Namespace, nameof(DependentModuleMetadatas));
    
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


    public class Model(ITransaction tx) : Entity(tx)
    {
        
        public string ModuleId
        {
            get => ModuleInfoExtended.ModuleId.Get(this);
            init => ModuleInfoExtended.ModuleId.Add(this, value);
        }
        
        public string Name
        {
            get => ModuleInfoExtended.Name.Get(this);
            init => ModuleInfoExtended.Name.Add(this, value);
        }
        
        public bool IsOfficial
        {
            get => ModuleInfoExtended.IsOfficial.Get(this);
            init => ModuleInfoExtended.IsOfficial.Add(this, value);
        }
        
        public ApplicationVersion Version
        {
            get => ModuleInfoExtended.Version.Get(this);
            init => ModuleInfoExtended.Version.Add(this, value);
        }
        
        public bool IsSingleplayerModule
        {
            get => ModuleInfoExtended.IsSingleplayerModule.Get(this);
            init => ModuleInfoExtended.IsSingleplayerModule.Add(this, value);
        }
        
        public bool IsMultiplayerModule
        {
            get => ModuleInfoExtended.IsMultiplayerModule.Get(this);
            init => ModuleInfoExtended.IsMultiplayerModule.Add(this, value);
        }
        
        public bool IsServerModule
        {
            get => ModuleInfoExtended.IsServerModule.Get(this);
            init => ModuleInfoExtended.IsServerModule.Add(this, value);
        }
        
        /// <summary>
        /// The dependent modules of the module.
        /// </summary>
        public IEnumerable<DependentModule.Model> DependentModules
        {
            get
            {
                if (!ModuleInfoExtended.DependentModules.IsIn(Db, Id))
                    return Enumerable.Empty<DependentModule.Model>();
                return ModuleInfoExtended.DependentModules
                    .Get(this)
                    .Select(id => Db.Get<DependentModule.Model>(id));
            }
        }

        /// <summary>
        /// The dependent module metadata of the module.
        /// </summary>
        public IEnumerable<DependentModuleMetadata.Model> DependentModuleMetadatas
        {
            get
            {
                if (!ModuleInfoExtended.DependentModuleMetadatas.IsIn(Db, Id))
                    return Enumerable.Empty<DependentModuleMetadata.Model>();
                
                return ModuleInfoExtended.DependentModuleMetadatas
                    .Get(this)
                    .Select(id => Db.Get<DependentModuleMetadata.Model>(id));
            }
        }


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
