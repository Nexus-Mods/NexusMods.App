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

    internal FileSystem(
        Dictionary<AbsolutePath, AbsolutePath> pathMappings,
        bool convertCrossPlatformPaths) : base(pathMappings, convertCrossPlatformPaths) { }

    /// <inheritdoc/>
    public override IFileSystem CreateOverlayFileSystem(
        Dictionary<AbsolutePath, AbsolutePath> pathMappings,
        bool convertCrossPlatformPaths = false)
        => new FileSystem(pathMappings, convertCrossPlatformPaths);

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
    {
        var fullPath = path.GetFullPath();
        if (!Directory.Exists(fullPath)) return;

        if (!recursive)
        {
            var isEmpty = EnumerateFiles(path, recursive: false).Any() ||
                          EnumerateDirectories(path, recursive: false).Any();
            if (!isEmpty) return;
        }

        foreach (var subDirectories in Directory.GetDirectories(fullPath))
        {
            InternalDeleteDirectory(FromFullPath(subDirectories), recursive);
        }

        try
        {
            var di = new DirectoryInfo(fullPath);
            if (di.Attributes.HasFlag(FileAttributes.ReadOnly))
                di.Attributes &= ~FileAttributes.ReadOnly;

            var attempts = 0;
        TopParent:

            try
            {
                Directory.Delete(fullPath, true);
            }
            catch (IOException)
            {
                if (attempts > 10)
                    throw;

                Thread.Sleep(100);
                attempts++;
                goto TopParent;
            }
        }
        catch (UnauthorizedAccessException)
        {
            Directory.Delete(fullPath, true);
        }
    }

    /// <inheritdoc/>
    protected override bool InternalFileExists(AbsolutePath path)
        => File.Exists(path.GetFullPath());

    /// <inheritdoc/>
    protected override void InternalDeleteFile(AbsolutePath path)
    {
        var fullPath = path.GetFullPath();
        if (File.Exists(fullPath))
        {
            try
            {
                File.Delete(fullPath);
            }
            catch (UnauthorizedAccessException)
            {
                var fi = new FileInfo(fullPath);

                if (fi.IsReadOnly)
                {
                    fi.IsReadOnly = false;
                    File.Delete(fullPath);
                }
                else
                {
                    throw;
                }
            }
        }

        if (Directory.Exists(fullPath))
            DeleteDirectory(path, true);
    }

    /// <inheritdoc/>
    protected override void InternalMoveFile(AbsolutePath source, AbsolutePath dest, bool overwrite)
        => File.Move(source.GetFullPath(), dest.GetFullPath(), overwrite);

    #endregion

}
