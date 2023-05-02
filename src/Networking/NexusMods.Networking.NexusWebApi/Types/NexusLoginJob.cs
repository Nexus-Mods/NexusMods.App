using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Interprocess.Jobs;
using NexusMods.DataModel.JsonConverters;

namespace NexusMods.Networking.NexusWebApi.Types;

[JsonName(nameof(NexusLoginJob))]
public record NexusLoginJob : AJobEntity
{
    public required Uri Uri { get; init; }
}
