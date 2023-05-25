using NexusMods.DataModel.Interprocess.Jobs;
using NexusMods.DataModel.JsonConverters;

namespace NexusMods.Networking.NexusWebApi.NMA.Types;

/// <summary>
/// Represents a job used to report the OAuth2 authorization URL to the UI.
/// </summary>
[JsonName(nameof(NexusLoginJob))]
public record NexusLoginJob : AJobEntity
{
    /// <summary>
    /// The OAuth2 authorization URL
    /// </summary>
    public required Uri Uri { get; init; }
}
