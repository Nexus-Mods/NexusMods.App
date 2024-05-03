using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;

namespace NexusMods.Games.MountAndBlade2Bannerlord.MnemonicDB;

public class SubModuleFileMetadata
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
}
