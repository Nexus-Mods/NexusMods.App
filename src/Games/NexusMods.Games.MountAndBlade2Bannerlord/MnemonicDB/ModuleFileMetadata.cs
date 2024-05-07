using NexusMods.Abstractions.MnemonicDB.Attributes;

namespace NexusMods.Games.MountAndBlade2Bannerlord.MnemonicDB;

public static class ModuleFileMetadata
{
    private const string Namespace = "NexusMods.Games.MountAndBlade2Bannerlord.MnemonicDB.ModuleFileMetadata";
    
    /// <summary>
    /// The original relative path of the module file.
    /// </summary>
    public static readonly RelativePathAttribute OriginalRelativePath = new(Namespace, nameof(OriginalRelativePath));
}
