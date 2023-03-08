namespace NexusMods.Paths;

/// <summary>
/// File system abstraction.
/// </summary>
public interface IFileSystem
{
    /// <inheritdoc cref="AbsolutePath.FromFullPath"/>
    AbsolutePath FromFullPath(string fullPath);

    /// <inheritdoc cref="AbsolutePath.FromDirectoryAndFileName"/>
    AbsolutePath FromDirectoryAndFileName(string? directoryPath, string fullPath);
}
