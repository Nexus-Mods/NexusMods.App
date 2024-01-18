using NexusMods.Abstractions.DataModel.Entities.Mods;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Installers.DTO.Files;

/// <summary>
/// This record is used for denoting files which belong to the game itself,
/// as opposed to one sourced from a game mod.
/// </summary>
[JsonName("NexusMods.DataModel.ModFiles.GameFile")]
public record GameFile : AModFile, IToFile, IStoredFile
{
    /// <inheritdoc />
    public required GamePath To { get; init; }

    /// <inheritdoc />
    public required Size Size { get; init; }

    /// <inheritdoc />
    public required Hash Hash { get; init; }
}
