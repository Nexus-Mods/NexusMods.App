using JetBrains.Annotations;
using NexusMods.Abstractions.Downloads;

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

    /// <inheritdoc cref="Sdk.Models.Library.DownloadedFile.DownloadPageUri"/>
    Uri DownloadPageUri { get; }
}
