using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Loadouts.Files;

/// <summary>
/// This record is used for denoting files which belong to the game itself,
/// as opposed to one sourced from a game mod.
/// </summary>
[JsonName("NexusMods.Abstractions.Installers.DTO.Files.GameFile")]
public record GameFile : AModFile, IToFile, IStoredFile
{
    /// <inheritdoc />
    public required GamePath To { get; init; }

    /// <inheritdoc />
    public required Size Size { get; init; }

    /// <inheritdoc />
    public required Hash Hash { get; init; }
}
