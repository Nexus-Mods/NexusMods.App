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

    IFileEntry GetFileEntry(AbsolutePath path);

    IDirectoryEntry GetDirectoryEntry(AbsolutePath path);

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

    /// <summary>
    /// Creates a directory if it does not already exists.
    /// </summary>
    /// <param name="path"></param>
    void CreateDirectory(AbsolutePath path);

    /// <summary>
    /// Determines whether the given path refers to an existing directory on disk.
    /// </summary>
    /// <param name="path">Path to the directory.</param>
    /// <returns></returns>
    bool DirectoryExists(AbsolutePath path);

    /// <summary>
    /// Deletes a specified empty directory, or a specified
    /// directory and any directory contents (subdirectories and files).
    /// </summary>
    /// <param name="path">Path to the directory.</param>
    /// <param name="recursive">
    /// <c>true</c> to delete this directory and it's contents,
    /// <c>false</c> to only delete this directory if it's empty. If <paramref name="recursive"/>
    /// is set to <c>false</c> and the directory is not empty, an exception will be thrown.
    /// </param>
    /// <exception cref="IOException">The directory specified is not empty and <paramref name="recursive"/>
    /// is set to <c>false</c>.</exception>
    void DeleteDirectory(AbsolutePath path, bool recursive);

    /// <summary>
    /// Determines whether the specified file exists.
    /// </summary>
    /// <param name="path">Path to the file.</param>
    /// <returns></returns>
    bool FileExists(AbsolutePath path);

    /// <summary>
    /// Deletes the specified file.
    /// </summary>
    /// <param name="path">Path to the file.</param>
    void DeleteFile(AbsolutePath path);
}
