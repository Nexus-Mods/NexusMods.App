using JetBrains.Annotations;

namespace NexusMods.Paths;

/// <summary>
/// File system abstraction.
/// </summary>
[PublicAPI]
public interface IFileSystem
{
    /// <summary>
    /// Creates a new <see cref="FileSystem"/> that allows for path mapping.
    /// </summary>
    /// <param name="pathMappings">Path mappings</param>
    /// <returns></returns>
    IFileSystem CreateOverlayFileSystem(Dictionary<AbsolutePath, AbsolutePath> pathMappings);

    AbsolutePath FromFullPath(string fullPath);

    AbsolutePath FromDirectoryAndFileName(string? directoryPath, string fullPath);

    /// <summary>
    /// Opens a file stream to the giving path.
    /// </summary>
    /// <param name="path">Path to the file.</param>
    /// <param name="mode">A <seealso cref="FileMode"/> value that specifies whether a file is created if one does not exist, and determines whether the contents of existing files are retained or overwritten.</param>
    /// <param name="access">A <seealso cref="FileAccess"/> value that specifies the operations that can be performed on the file.</param>
    /// <param name="share">A <seealso cref="FileShare"/> value specifying the type of access other threads have to the file.</param>
    /// <returns></returns>
    Stream OpenFile(AbsolutePath path,
        FileMode mode,
        FileAccess access = FileAccess.Read,
        FileShare share = FileShare.ReadWrite);

    /// <summary>
    /// Opens the file for read-only access.
    /// </summary>
    /// <param name="path">Path to the file.</param>
    /// <returns></returns>
    Stream ReadFile(AbsolutePath path);

    /// <summary>
    /// Opens the file for write-only access.
    /// </summary>
    /// <param name="path">Path to the file.</param>
    /// <returns></returns>
    Stream WriteFile(AbsolutePath path);

    /// <summary>
    /// Creates a new file for read/write access. If the file already exists,
    /// it will be overwritten.
    /// </summary>
    /// <param name="path">Path to the file.</param>
    /// <returns></returns>
    Stream CreateFile(AbsolutePath path);
}
