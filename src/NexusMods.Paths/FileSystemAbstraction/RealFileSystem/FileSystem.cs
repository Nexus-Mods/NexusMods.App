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

    private static EnumerationOptions GetSearchOptions(bool recursive) => new()
    {
        AttributesToSkip = 0,
        RecurseSubdirectories = recursive,
        MatchType = MatchType.Win32
    };

    internal FileSystem() { }

    #region Implementation

    internal FileSystem(Dictionary<AbsolutePath, AbsolutePath> pathMappings) : base(pathMappings) { }

    /// <inheritdoc/>
    public override IFileSystem CreateOverlayFileSystem(Dictionary<AbsolutePath, AbsolutePath> pathMappings)
        => new FileSystem(pathMappings);

    /// <inheritdoc/>
    protected override IFileEntry InternalGetFileEntry(AbsolutePath path)
        => new FileEntry(this, path);

    /// <inheritdoc/>
    protected override IDirectoryEntry InternalGetDirectoryEntry(AbsolutePath path)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    protected override IEnumerable<AbsolutePath> InternalEnumerateFiles(AbsolutePath directory, string pattern, bool recursive)
    {
        var options = GetSearchOptions(recursive);
        using var enumerator = new FilesEnumerator(directory.GetFullPath(), pattern, options);
        while (enumerator.MoveNext())
        {
            var item = enumerator.Current;
            if (item.IsDirectory) continue;
            yield return FromDirectoryAndFileName(enumerator.CurrentDirectory, item.FileName);
        }
    }

    /// <inheritdoc/>
    protected override IEnumerable<AbsolutePath> InternalEnumerateDirectories(AbsolutePath directory, string pattern, bool recursive)
    {
        var options = GetSearchOptions(recursive);
        var enumerator = new DirectoriesEnumerator(directory.GetFullPath(), "*", options);
        while (enumerator.MoveNext())
        {
            var item = enumerator.Current;
            yield return FromFullPath(AbsolutePath.JoinPathComponents(enumerator.CurrentDirectory, item));
        }
    }

    /// <inheritdoc/>
    protected override IEnumerable<IFileEntry> InternalEnumerateFileEntries(AbsolutePath directory, string pattern, bool recursive)
    {
        var options = GetSearchOptions(recursive);
        var enumerator = new FilesEnumeratorEx(directory.GetFullPath(), pattern, options);

        while (enumerator.MoveNext())
        {
            var item = enumerator.Current;
            if (item.IsDirectory) continue;
            yield return new FileEntry(this, FromDirectoryAndFileName(enumerator.CurrentDirectory, item.FileName));
        }
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

    /// <inheritdoc/>
    protected override void InternalMoveFile(AbsolutePath source, AbsolutePath dest, bool overwrite)
        => File.Move(source.GetFullPath(), dest.GetFullPath(), overwrite);

    #endregion

}
