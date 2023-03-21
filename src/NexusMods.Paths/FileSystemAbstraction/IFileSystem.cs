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

    /// <summary>
    /// Returns a known path.
    /// </summary>
    /// <param name="knownPath"></param>
    /// <returns></returns>
    AbsolutePath GetKnownPath(KnownPath knownPath);

    /// <summary>
    /// Creates a new <see cref="AbsolutePath"/> from a full path.
    /// </summary>
    /// <param name="fullPath">Full path</param>
    /// <returns></returns>
    AbsolutePath FromFullPath(string fullPath);

    /// <summary>
    /// Creates a new <see cref="AbsolutePath"/> from a directory path and a file name.
    /// </summary>
    /// <param name="directoryPath">Directory path</param>
    /// <param name="fileName">File name</param>
    /// <returns></returns>
    AbsolutePath FromDirectoryAndFileName(string directoryPath, string fileName);

    /// <summary>
    /// Returns the <see cref="IFileEntry"/> of the file at the given path.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    IFileEntry GetFileEntry(AbsolutePath path);

    /// <summary>
    /// Returns the <see cref="IDirectoryEntry"/> of the directory at the given path.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    IDirectoryEntry GetDirectoryEntry(AbsolutePath path);

    /// <summary>
    /// Enumerates through all files present in the directory that match the
    /// provided pattern.
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="pattern"></param>
    /// <param name="recursive"></param>
    /// <returns></returns>
    IEnumerable<AbsolutePath> EnumerateFiles(AbsolutePath directory, string pattern = "*", bool recursive = true);

    /// <summary>
    /// Enumerates through all directories in the directory that match the
    /// provided pattern.
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="pattern"></param>
    /// <param name="recursive"></param>
    /// <returns></returns>
    IEnumerable<AbsolutePath> EnumerateDirectories(AbsolutePath directory, string pattern = "*", bool recursive = true);

    /// <summary>
    /// Enumerates through all file entries in the directory that match the
    /// provided pattern.
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="pattern"></param>
    /// <param name="recursive"></param>
    /// <returns></returns>
    IEnumerable<IFileEntry> EnumerateFileEntries(AbsolutePath directory, string pattern = "*", bool recursive = true);

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

    /// <summary>
    /// Moves a specified file to a new location.
    /// </summary>
    /// <param name="source">Path of the file to move.</param>
    /// <param name="dest">Path to move the file to.</param>
    /// <param name="overwrite"><c>true</c> to overwrite the destination file if it already exists; <c>false</c> otherwise</param>
    /// <exception cref="IOException">The destination already exists and <paramref name="overwrite"/> is <c>false</c></exception>
    void MoveFile(AbsolutePath source, AbsolutePath dest, bool overwrite);

    /// <summary>
    /// Reads all bytes from a file into an array.
    /// </summary>
    /// <param name="path">Path to the file.</param>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns></returns>
    Task<byte[]> ReadAllBytesAsync(AbsolutePath path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads all text from a file using UTF-8 encoding.
    /// </summary>
    /// <param name="path">Path to the file.</param>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns></returns>
    Task<string> ReadAllTextAsync(AbsolutePath path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes all bytes to a file.
    /// </summary>
    /// <param name="path">Path to the file.</param>
    /// <param name="data">Data to write.</param>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns></returns>
    Task WriteAllBytesAsync(AbsolutePath path, byte[] data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes all text to a file using UTF-8 encoding.
    /// </summary>
    /// <param name="path">Path to the file.</param>
    /// <param name="text">Text to write to the file.</param>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns></returns>
    Task WriteAllTextAsync(AbsolutePath path, string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes all lines of text to a file using UTF-8 encoding.
    /// </summary>
    /// <param name="path">Path to the file.</param>
    /// <param name="lines">Lines of text to write to the file.</param>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns></returns>
    Task WriteAllLinesAsync(AbsolutePath path, [InstantHandle(RequireAwait = true)] IEnumerable<string> lines, CancellationToken cancellationToken = default);
}
