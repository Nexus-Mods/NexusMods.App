using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.Abstractions.Serialization.DataModel;

namespace NexusMods.Networking.Downloaders.Tasks.State;

/// <summary>
/// State specific to <see cref="NxmDownloadTask"/> suspend.
/// </summary>
/// <param name="Query">Converts to/from NXMUrl</param>
[JsonName("NexusMods.Networking.Downloaders.Tasks.State.NxmDownloadState")]
public record NxmDownloadState(string Query) : DownloaderState.Model
{

    bool IsThis(Entity e) => e.Contains(NxmDownloadState.Query);
    
    // ReSharper disable once UnusedMember.Global - Required for serialization
    public NxmDownloadState() : this(string.Empty) { }

    /// <summary>
    /// Converts the query string back to a Nexus URL.
    /// </summary>
    public NXMUrl AsUrl() => NXMUrl.Parse(new Uri(Query));
}
