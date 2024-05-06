using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.MnemonicDB.Attributes.Extensions;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Games.MountAndBlade2Bannerlord.MnemonicDB;

public static class SubModuleFileMetadata
{
    public const string Namespace = "NexusMods.Games.MountAndBlade2Bannerlord.Models.SubModuleFileMetadata";
    
    /// <summary>
    /// Is the sub-module file valid.
    /// </summary>
    public static readonly BooleanAttribute IsValid = new(Namespace, nameof(IsValid));
    
    /// <summary>
    /// Link to the sub-module info.
    /// </summary>
    public static readonly ReferenceAttribute ModuleInfo = new(Namespace, nameof(ModuleInfo));

    /// <summary>
    /// True if the entity is a sub-module file metadata.
    /// </summary>
    public static bool IsSubModuleFileMetadata(this Entity entity) 
        => entity.Contains(IsValid); 
    
    /// <summary>
    /// Selects the sub-module file metadata entities.
    /// </summary>
    public static IEnumerable<Model> OfSubModuleFileMetadata(this IEnumerable<Entity> entities) 
        => entities
            .Where(IsSubModuleFileMetadata)
            .Select(entity => entity.Remap<Model>());
    
    public class Model(ITransaction tx) : Entity(tx)
    {
        public bool IsValid
        {
            get => SubModuleFileMetadata.IsValid.Get(this);
            init => SubModuleFileMetadata.IsValid.Add(this, value);
        }

        public EntityId ModuleInfoId
        {
            get => SubModuleFileMetadata.ModuleInfo.Get(this);
            init => SubModuleFileMetadata.ModuleInfo.Add(this, value);
        }

        /// <summary>
        /// The sub-module module info.
        /// </summary>
        public ModuleInfoExtended.Model ModuleInfo => Db.Get<ModuleInfoExtended.Model>(ModuleInfoId);
    }
}
