using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Games.MountAndBlade2Bannerlord.MnemonicDB;

public partial class DependentModule : IModelDefinition
{
    private const string Namespace = "NexusMods.Games.MountAndBlade2Bannerlord.Models";
    
    /// <summary>
    /// The module ID of the dependent module.
    /// </summary>
    public static readonly StringAttribute ModuleId = new(Namespace, nameof(ModuleId)) {IsIndexed = true};
    
    /// <summary>
    /// The application version of the dependent module.
    /// </summary>
    public static readonly ApplicationVersionAttribute Version = new(Namespace, nameof(Version));
    
    /// <summary>
    /// True if the dependent module is optional.
    /// </summary>
    public static readonly BooleanAttribute IsOptional = new(Namespace, nameof(IsOptional));

    public partial struct ReadOnly
    {
        /// <summary>
        /// Converts this model to a <see cref="Bannerlord.ModuleManager.DependentModule"/>.
        /// </summary>
        /// <returns></returns>
        public Bannerlord.ModuleManager.DependentModule ToDependentModule()
        {
            return new(ModuleId, Version, IsOptional);
        }
        
    }
}
