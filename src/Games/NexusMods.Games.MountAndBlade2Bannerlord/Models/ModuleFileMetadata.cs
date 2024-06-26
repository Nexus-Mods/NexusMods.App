using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Models;

[Include<Metadata>]
public partial class ModuleFileMetadata : IModelDefinition
{
    private const string Namespace = "NexusMods.Games.MountAndBlade2Bannerlord.Models.ModuleFileMetadata";
    
    /// <summary>
    /// The original relative path of the file.
    /// </summary>
    public static readonly RelativePathAttribute OriginalRelativePath = new(Namespace, nameof(OriginalRelativePath));
}
