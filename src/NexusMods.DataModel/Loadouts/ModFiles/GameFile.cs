using NexusMods.DataModel.Games;
using NexusMods.DataModel.JsonConverters;

namespace NexusMods.DataModel.Loadouts.ModFiles;

/// <summary>
/// This record is used for denoting files which belong to the game itself,
/// as opposed to one sourced from a game mod.
/// </summary>
[JsonName("NexusMods.DataModel.ModFiles.GameFile")]
public record GameFile : AStaticModFile
{
    /// <summary>
    /// Unique installation of a game this file is tied to.
    /// </summary>
    public required GameInstallation Installation { get; init; }
}
