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

    private AbsolutePath GetMappedPath(AbsolutePath originalPath)
    {
        return _pathMappings.TryGetValue(originalPath, out var mappedPath)
            ? mappedPath
            : originalPath;
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
    Stream IFileSystem.OpenFile(AbsolutePath path, FileMode mode, FileAccess access, FileShare share)
    {
        var actualPath = GetMappedPath(path);
        return OpenFile(actualPath, mode, access, share);
    }

    #endregion

    #region Abstract Methods

    /// <inheritdoc cref="IFileSystem.OpenFile"/>
    protected abstract Stream OpenFile(AbsolutePath path, FileMode mode, FileAccess access, FileShare share);

    #endregion
}
