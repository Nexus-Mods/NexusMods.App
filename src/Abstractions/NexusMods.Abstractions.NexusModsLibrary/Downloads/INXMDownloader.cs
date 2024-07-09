using JetBrains.Annotations;
using NexusMods.Abstractions.Downloads;

namespace NexusMods.Abstractions.NexusModsLibrary;

/// <summary>
/// Represents a downloader for <see cref="NXMDownloadActivity"/>.
/// </summary>
[PublicAPI]
public interface INXMDownloader : IDownloader;
