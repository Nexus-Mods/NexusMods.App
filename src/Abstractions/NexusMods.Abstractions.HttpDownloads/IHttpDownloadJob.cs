using JetBrains.Annotations;
using NexusMods.Abstractions.Downloads;
using NexusMods.Abstractions.Library.Models;

namespace NexusMods.Abstractions.HttpDownloads;

/// <summary>
/// Represents an HTTP download.
/// </summary>
[PublicAPI]
public interface IHttpDownloadJob : IDownloadJob
{
    /// <summary>
    /// Gets the URI to download.
    /// </summary>
    Uri Uri { get; }

    /// <inheritdoc cref="DownloadedFile.DownloadPageUri"/>
    Uri DownloadPageUri { get; }
}
