using NexusMods.DataModel.Games;
using NexusMods.DataModel.JsonConverters;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts.ModFiles;

/// <summary>
/// This record is used for denoting files which belong to the game itself,
/// as opposed to one sourced from a game mod.
/// </summary>
[JsonName("NexusMods.DataModel.ModFiles.GameFile")]
public record GameFile : AModFile, IToFile, IFromArchive
{
    /// <summary>
    /// Unique installation of a game this file is tied to.
    /// </summary>
    public required GameInstallation Installation { get; init; }

    /// <inheritdoc />
    public required GamePath To { get; init; }

    /// <inheritdoc />
    public required Size Size { get; init; }

    /// <inheritdoc />
    public required Hash Hash { get; init; }
}
