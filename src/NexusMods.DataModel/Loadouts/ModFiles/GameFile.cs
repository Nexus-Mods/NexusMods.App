using NexusMods.DataModel.JsonConverters;
using NexusMods.Interfaces;

namespace NexusMods.DataModel.Loadouts.ModFiles;

[JsonName("NexusMods.DataModel.ModFiles.GameFile")]
public record GameFile : AStaticModFile
{
    public required GameInstallation Installation { get; init; }
}