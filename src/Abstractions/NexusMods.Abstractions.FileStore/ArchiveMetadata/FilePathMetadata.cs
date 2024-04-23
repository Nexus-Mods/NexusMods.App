using NexusMods.Abstractions.MnemonicDB.Attributes;

namespace NexusMods.Abstractions.FileStore.ArchiveMetadata;

/// <summary>
/// Archive metadata for a download that was installed from a file path.
/// </summary>
public static class FilePathMetadata
{
    private const string Namespace = "NexusMods.Abstractions.FileStore.ArchiveMetadata";
    
    /// <summary>
    /// The original name of the file, e.g. "mod_v134.zip".
    /// </summary>
    public static readonly RelativePathAttribute OriginalName = new (Namespace, nameof(OriginalName));
}
