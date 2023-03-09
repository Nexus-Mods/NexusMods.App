using JetBrains.Annotations;

namespace NexusMods.Paths;

/// <summary>
/// Abstract class for implementations of <see cref="IFileSystem"/> that
/// provides helper functions and reduces code duplication.
/// </summary>
[PublicAPI]
public abstract class BaseFileSystem : IFileSystem
{
    private readonly Dictionary<AbsolutePath, AbsolutePath> _pathMappings = new();

    /// <summary>
    /// Constructor.
    /// </summary>
    protected BaseFileSystem() { }

    /// <summary>
    /// Constructor that accepts path mappings.
    /// </summary>
    /// <param name="pathMappings"></param>
    protected BaseFileSystem(Dictionary<AbsolutePath, AbsolutePath> pathMappings)
    {
        _pathMappings = pathMappings;
    }

    internal AbsolutePath GetMappedPath(AbsolutePath originalPath)
    {
        // direct mapping
        if (_pathMappings.TryGetValue(originalPath, out var mappedPath))
            return mappedPath;

        // indirect mapping via parent directory
        var (originalParentDirectory, newParentDirectory) = _pathMappings
            .FirstOrDefault(kv => originalPath.InFolder(kv.Key));

        if (newParentDirectory == default) return originalPath;

        var relativePath = originalPath.RelativeTo(originalParentDirectory);
        var newPath = newParentDirectory.CombineUnchecked(relativePath);

        return newPath;
    }

    #region IFileStream Implementation

    /// <inheritdoc/>
    public abstract IFileSystem CreateOverlayFileSystem(
        Dictionary<AbsolutePath, AbsolutePath> pathMappings);

    /// <inheritdoc/>
    public AbsolutePath FromFullPath(string fullPath)
        => AbsolutePath.FromFullPath(fullPath, this);

    /// <inheritdoc/>
    public AbsolutePath FromDirectoryAndFileName(string? directoryPath, string fullPath)
        => AbsolutePath.FromDirectoryAndFileName(directoryPath, fullPath, this);

    /// <inheritdoc/>
    public Stream ReadFile(AbsolutePath path) => OpenFile(path, FileMode.Open, FileAccess.Read, FileShare.Read);

    /// <inheritdoc/>
    public Stream WriteFile(AbsolutePath path) => OpenFile(path, FileMode.Open, FileAccess.Write, FileShare.Read);

    /// <inheritdoc/>
    public Stream CreateFile(AbsolutePath path) => OpenFile(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);

    /// <inheritdoc/>
    public Stream OpenFile(AbsolutePath path, FileMode mode, FileAccess access, FileShare share)
    {
        var actualPath = GetMappedPath(path);
        return InternalOpenFile(actualPath, mode, access, share);
    }

    #endregion

    #region Abstract Methods

    /// <inheritdoc cref="IFileSystem.OpenFile"/>
    protected abstract Stream InternalOpenFile(AbsolutePath path, FileMode mode, FileAccess access, FileShare share);

    #endregion
}
