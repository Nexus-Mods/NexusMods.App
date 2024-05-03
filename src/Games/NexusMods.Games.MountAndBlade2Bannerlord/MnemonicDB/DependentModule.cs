using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;

namespace NexusMods.Games.MountAndBlade2Bannerlord.MnemonicDB;

public static class DependentModule
{
    public const string Namespace = "NexusMods.Games.MountAndBlade2Bannerlord.Models";
    
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
}
