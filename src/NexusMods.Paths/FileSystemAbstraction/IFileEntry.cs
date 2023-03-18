using JetBrains.Annotations;

namespace NexusMods.Paths;

/// <summary>
/// Represents a file entry.
/// </summary>
[PublicAPI]
public interface IFileEntry
{
    /// <summary>
    /// Gets or sets the parent file system.
    /// </summary>
    IFileSystem FileSystem { get; set; }

    /// <summary>
    /// Gets the path to the file.
    /// </summary>
    AbsolutePath Path { get; }

    /// <summary>
    /// Gets the size of the current file.
    /// </summary>
    Size Size { get; }

    /// <summary>
    /// Gets or sets the time when the current file was last written to.
    /// </summary>
    DateTime LastWriteTime { get; set; }

    /// <summary>
    /// Gets or sets the creation time of the current file.
    /// </summary>
    DateTime CreationTime { get; set; }

    /// <summary>
    /// Gets or sets a value that determines if the current file is read only.
    /// </summary>
    bool IsReadOnly { get; set; }

    /// <summary>
    /// Gets the time when the current file was last written to in Coordinated Universal Time (UTC).
    /// </summary>
    DateTime LastWriteTimeUtc => LastWriteTime.ToUniversalTime();

    /// <summary>
    /// Gets the creation time of the current file in Coordinated Universal Time (UTC).
    /// </summary>
    DateTime CreationTimeUtc => CreationTime.ToUniversalTime();

    /// <summary>
    /// Returns the file version info of the current file.
    /// </summary>
    /// <returns></returns>
    FileVersionInfo GetFileVersionInfo();
}
