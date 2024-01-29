using NexusMods.Abstractions.IO;
using NexusMods.Paths;

namespace NexusMods.Abstractions.FileStore.Trees;

/// <summary>
///     Represents a source file used to create the <see cref="ModFileTree"/>.
/// </summary>
public struct ModFileTreeSource
{
    /// <summary>
    /// Hash of the file
    /// </summary>
    public ulong Hash { get; init; }

    /// <summary>
    /// Size of the file
    /// </summary>
    public ulong Size { get; init; }

    /// <summary>
    /// Path of the file
    /// </summary>
    public RelativePath Path { get; init; }

    /// <summary>
    /// Provides access to the underlying file.
    /// </summary>
    public IStreamFactory? Factory { get; init; }

    /// <summary>
    ///     Creates a source file for the ModFileTree given a hash, size and path.
    /// </summary>
    /// <remarks>Intended use of this overload is for testing.</remarks>
    public ModFileTreeSource(ulong hash, ulong size, RelativePath path, IStreamFactory? factory = null)
    {
        Hash = hash;
        Size = size;
        Path = path;
        Factory = factory;
    }
}
