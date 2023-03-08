namespace NexusMods.Paths;

/// <summary>
/// Default implementation of <see cref="IFileSystem"/>.
/// </summary>
public class FileSystem : IFileSystem
{
    /// <summary>
    /// Shared instance of the default implementation.
    /// </summary>
    public static readonly IFileSystem Shared = new FileSystem();

    private FileSystem() { }

    /// <inheritdoc/>
    public AbsolutePath FromFullPath(string fullPath)
        => AbsolutePath.FromFullPath(fullPath, this);

    /// <inheritdoc/>
    public AbsolutePath FromDirectoryAndFileName(string? directoryPath, string fullPath)
        => AbsolutePath.FromDirectoryAndFileName(directoryPath, fullPath, this);
}
