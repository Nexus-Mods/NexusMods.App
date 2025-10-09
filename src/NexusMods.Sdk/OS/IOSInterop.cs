using JetBrains.Annotations;
using NexusMods.Paths;

namespace NexusMods.Sdk;

/// <summary>
/// OS-specific functionality.
/// </summary>
[PublicAPI]
public interface IOSInterop
{
    /// <summary>
    /// Gets the path to executable of the current process.
    /// </summary>
    AbsolutePath GetRunningExecutablePath(out string rawPath);

    /// <summary>
    /// Opens the Uri in the default application handler based on the Uri scheme.
    /// </summary>
    /// <seealso cref="OpenUri"/>
    void OpenUri(Uri uri);

    /// <summary>
    /// Opens the file in the default application handler.
    /// </summary>
    void OpenFile(AbsolutePath filePath);

    /// <summary>
    /// Opens the directory with the system explorer.
    /// </summary>
    void OpenDirectory(AbsolutePath directoryPath);

    /// <summary>
    /// Opens the directory with the system explorer and highlights the file.
    /// </summary>
    void OpenFileInDirectory(AbsolutePath filePath);

    /// <summary>
    /// Gets all file system mounts.
    /// </summary>
    ValueTask<FileSystemMount[]> GetFileSystemMounts(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the file system mount that hosts the file or directory at the given path.
    /// </summary>
    ValueTask<FileSystemMount?> GetFileSystemMount(AbsolutePath path, CancellationToken cancellationToken = default);

    ValueTask RegisterUriSchemeHandler(string scheme, bool setAsDefaultHandler = true, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a file system mount.
/// </summary>
[PublicAPI]
public record FileSystemMount(string Source, AbsolutePath Target, string Type, Size BytesTotal, Size BytesAvailable)
{
    public Size BytesUsed => BytesTotal - BytesAvailable;
}
