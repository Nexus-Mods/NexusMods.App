using NexusMods.MnemonicDB.Abstractions.Attributes;

namespace NexusMods.Abstractions.FileStore.ArchiveMetadata;

/// <summary>
/// DownloadAnalysis metadata for a download that was registered from a stream.
/// </summary>
public static class StreamBasedFileOriginMetadata
{
    private const string Namespace = "NexusMods.Abstractions.FileStore.ArchiveMetadata";
    
    /// <summary>
    /// If present, this archive was originated from a stream, likely a nested archive.
    /// </summary>
    public static readonly MarkerAttribute StreamBasedOrigin = new(Namespace, nameof(StreamBasedOrigin));
}
