using JetBrains.Annotations;
using NexusMods.Abstractions.Downloads;

namespace NexusMods.Abstractions.NexusModsLibrary;

/// <summary>
/// Represents an NXM download.
/// </summary>
[PublicAPI]
public interface INXMDownloadJob : IDownloadJob
{
    NexusModsFileMetadata.ReadOnly FileMetadata { get; }
}
