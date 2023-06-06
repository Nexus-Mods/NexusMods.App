namespace NexusMods.Networking.Downloaders.Interfaces.Traits;

/// <summary>
/// Interface implemented by <see cref="IDownloadTask"/>(s) that have a download version.
/// </summary>
public interface IHaveDownloadVersion
{
    /// <summary>
    /// Version of the mod; can sometimes be arbitrary and not follow SemVer.
    /// </summary>
    public string Version { get; }
}
