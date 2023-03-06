using NexusMods.DataModel.Games;
using NexusMods.DataModel.JsonConverters;

namespace NexusMods.DataModel.Loadouts.ModFiles;

[JsonName("NexusMods.DataModel.ModFiles.GameFile")]
public record GameFile : AStaticModFile
{
    public required GameInstallation Installation { get; init; }
}
