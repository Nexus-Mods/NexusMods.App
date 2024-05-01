using NexusMods.Abstractions.GameLocators;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Loadouts.Files;

/// <summary>
/// Clas version of IFileTreeNode
/// </summary>
public class FileTreeNode : IFileTreeNode
{
    /// <inheritdoc />
    public GamePath To { get; init; }

    /// <inheritdoc />
    public Hash Hash { get; init; }

    /// <inheritdoc />
    public Size Size { get; init; }
}
