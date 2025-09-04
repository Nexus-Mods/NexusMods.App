using NexusMods.Abstractions.Steam.Values;

namespace NexusMods.Networking.Steam.Exceptions;

public class FailedToGetRequestCode : Exception
{
    public FailedToGetRequestCode(AppId appId, DepotId depotId, ManifestId manifestId) : base($"Failed to get request code for {appId}/{depotId}/{manifestId}")
    {
        AppId = appId;
        DepotId = depotId;
        ManifestId = manifestId;
    }

    public ManifestId ManifestId { get; }

    public DepotId DepotId { get; }

    public AppId AppId { get; }
}
