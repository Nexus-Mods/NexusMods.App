using NexusMods.DataModel.JsonConverters;
using NexusMods.Networking.NexusWebApi.Types;

namespace NexusMods.Networking.Downloaders.Tasks.State;

/// <summary>
/// State specific to <see cref="NxmDownloadTask"/> suspend.
/// </summary>
/// <param name="Query">Converts to/from NXMUrl</param>
[JsonName("NexusMods.Networking.Downloaders.Tasks.State.NxmDownloadState")]
public record NxmDownloadState(string Query) : ITypeSpecificState
{
    // ReSharper disable once UnusedMember.Global - Required for serialization
    public NxmDownloadState() : this(string.Empty) { }

    /// <summary>
    /// Converts the query string back to a Nexus URL.
    /// </summary>
    public NXMUrl AsUrl() => NXMUrl.Parse(new Uri(Query));
}
