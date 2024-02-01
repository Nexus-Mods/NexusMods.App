using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Loadouts.Files;

/// <summary>
/// A mod file that is stored in the IFileStore. In other words,
/// this file is not generated on-the-fly or contain any sort of special
/// logic that defines its contents. Because of this we know the hash
/// and the size. This file may originally come from a download, a
/// tool's output, or a backed up game file.
/// </summary>
[JsonName("NexusMods.Abstractions.Installers.DTO.Files.StoredFile")]
public record StoredFile : AModFile, IStoredFile, IToFile
{
    /// <inheritdoc />
    public required Size Size { get; init; }

    /// <inheritdoc />
    public required Hash Hash { get; init; }

    /// <inheritdoc />
    public required GamePath To { get; init; }
}
