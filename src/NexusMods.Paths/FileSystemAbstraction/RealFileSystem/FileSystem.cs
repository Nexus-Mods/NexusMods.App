using JetBrains.Annotations;

namespace NexusMods.Paths;

/// <summary>
/// Default implementation of <see cref="IFileSystem"/>.
/// </summary>
[PublicAPI]
public partial class FileSystem : BaseFileSystem
{
    /// <summary>
    /// Shared instance of the default implementation.
    /// </summary>
    public static readonly IFileSystem Shared = new FileSystem();

    private FileSystem() { }

    #region Implementation

    private FileSystem(Dictionary<AbsolutePath, AbsolutePath> pathMappings) : base(pathMappings) { }

    /// <inheritdoc/>
    public override IFileSystem CreateOverlayFileSystem(Dictionary<AbsolutePath, AbsolutePath> pathMappings)
        => new FileSystem(pathMappings);

    /// <inheritdoc/>
    protected override IFileEntry InternalGetFileEntry(AbsolutePath path)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    protected override IDirectoryEntry InternalGetDirectoryEntry(AbsolutePath path)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    protected override Stream InternalOpenFile(AbsolutePath path, FileMode mode, FileAccess access, FileShare share)
        => File.Open(path.GetFullPath(), mode, access, share);

    /// <inheritdoc/>
    protected override void InternalCreateDirectory(AbsolutePath path)
        => Directory.CreateDirectory(path.GetFullPath());

    /// <inheritdoc/>
    protected override bool InternalDirectoryExists(AbsolutePath path)
        => Directory.Exists(path.GetFullPath());

    /// <inheritdoc/>
    protected override void InternalDeleteDirectory(AbsolutePath path, bool recursive)
        => Directory.Delete(path.GetFullPath(), recursive);

    /// <inheritdoc/>
    protected override bool InternalFileExists(AbsolutePath path)
        => File.Exists(path.GetFullPath());

    /// <inheritdoc/>
    protected override void InternalDeleteFile(AbsolutePath path)
        => File.Delete(path.GetFullPath());

    #endregion

}
