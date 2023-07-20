namespace NexusMods.Networking.Downloaders.Interfaces.Traits;

/// <summary>
/// Interface implemented by <see cref="IDownloadTask"/>(s) that have a known file size.
/// </summary>
public interface IHaveFileSize
{
    /// <summary>
    /// Size of the file being downloaded in bytes. A value of less than 0 means size is unknown.
    /// </summary>
    public long SizeBytes { get; }
}
