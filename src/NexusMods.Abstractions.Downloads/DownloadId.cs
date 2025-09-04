using JetBrains.Annotations;
using NexusMods.Abstractions.Jobs;
using TransparentValueObjects;

namespace NexusMods.Abstractions.Downloads;

/// <summary>
/// Unique identifier for a download that abstracts over <see cref="JobId"/>.
/// This allows the service to handle both active downloads (with <see cref="JobId"/>) and completed downloads.
/// </summary>
[PublicAPI]
[ValueObject<Guid>]
public readonly partial struct DownloadId : IAugmentWith<DefaultValueAugment, JsonAugment>
{
    /// <inheritdoc/>
    public static DownloadId DefaultValue { get; } = From(Guid.Empty);
    
    /// <summary>
    /// Implicitly converts a <see cref="JobId"/> to a <see cref="DownloadId"/>.
    /// </summary>
    /// <param name="jobId">The <see cref="JobId"/> to convert.</param>
    /// <returns>A <see cref="DownloadId"/> with the same underlying <see cref="Guid"/> value.</returns>
    public static implicit operator DownloadId(JobId jobId) => From(jobId.Value);
    
    /// <summary>
    /// Implicitly converts a <see cref="DownloadId"/> to a <see cref="JobId"/>.
    /// </summary>
    /// <param name="downloadId">The <see cref="DownloadId"/> to convert.</param>
    /// <returns>A <see cref="JobId"/> with the same underlying <see cref="Guid"/> value.</returns>
    public static implicit operator JobId(DownloadId downloadId) => JobId.From(downloadId.Value);
    
    /// <summary>
    /// Determines whether a <see cref="JobId"/> and a <see cref="DownloadId"/> are equal.
    /// </summary>
    /// <param name="left">The <see cref="JobId"/> to compare.</param>
    /// <param name="right">The <see cref="DownloadId"/> to compare.</param>
    /// <returns><see langword="true"/> if the values are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(JobId left, DownloadId right) => left.Value == right.Value;
    
    /// <summary>
    /// Determines whether a <see cref="JobId"/> and a <see cref="DownloadId"/> are not equal.
    /// </summary>
    /// <param name="left">The <see cref="JobId"/> to compare.</param>
    /// <param name="right">The <see cref="DownloadId"/> to compare.</param>
    /// <returns><see langword="true"/> if the values are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(JobId left, DownloadId right) => left.Value != right.Value;
    
    /// <summary>
    /// Determines whether a <see cref="DownloadId"/> and a <see cref="JobId"/> are equal.
    /// </summary>
    /// <param name="left">The <see cref="DownloadId"/> to compare.</param>
    /// <param name="right">The <see cref="JobId"/> to compare.</param>
    /// <returns><see langword="true"/> if the values are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(DownloadId left, JobId right) => left.Value == right.Value;
    
    /// <summary>
    /// Determines whether a <see cref="DownloadId"/> and a <see cref="JobId"/> are not equal.
    /// </summary>
    /// <param name="left">The <see cref="DownloadId"/> to compare.</param>
    /// <param name="right">The <see cref="JobId"/> to compare.</param>
    /// <returns><see langword="true"/> if the values are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(DownloadId left, JobId right) => left.Value != right.Value;
}