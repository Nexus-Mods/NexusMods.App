using JetBrains.Annotations;
using NexusMods.Abstractions.Downloads;

namespace NexusMods.Abstractions.HttpDownloads;

/// <summary>
/// Represents a downloader for <see cref="IHttpDownloadActivity"/>.
/// </summary>
[PublicAPI]
public interface IHttpDownloader : IDownloader;
