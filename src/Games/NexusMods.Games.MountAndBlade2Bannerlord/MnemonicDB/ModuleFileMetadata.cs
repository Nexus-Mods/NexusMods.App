using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Games.MountAndBlade2Bannerlord.MnemonicDB;

[Include<NexusMods.Abstractions.Loadouts.Files.File>]
public partial class ModuleFileMetadata : IModelDefinition
{
    private const string Namespace = "NexusMods.Games.MountAndBlade2Bannerlord.MnemonicDB.ModuleFileMetadata";
    
    /// <summary>
    /// The original relative path of the module file.
    /// </summary>
    public static readonly RelativePathAttribute OriginalRelativePath = new(Namespace, nameof(OriginalRelativePath));
}
