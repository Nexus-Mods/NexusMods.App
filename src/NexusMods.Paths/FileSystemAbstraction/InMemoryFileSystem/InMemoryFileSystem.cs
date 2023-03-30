using System.Diagnostics;
using System.Text;
using JetBrains.Annotations;
using NexusMods.Paths.Utilities;

namespace NexusMods.Paths;

/// <summary>
/// Implementation of <see cref="IFileSystem"/> for use with tests.
/// </summary>
[PublicAPI]
public partial class InMemoryFileSystem : BaseFileSystem
{
    private readonly InMemoryDirectoryEntry _rootDirectory;

    private readonly Dictionary<AbsolutePath, InMemoryFileEntry> _files = new();
    private readonly Dictionary<AbsolutePath, InMemoryDirectoryEntry> _directories = new();

    /// <summary>
    /// Constructor.
    /// </summary>
    public InMemoryFileSystem() : this(new Dictionary<AbsolutePath, AbsolutePath>(), new Dictionary<KnownPath, AbsolutePath>(), false) { }

    private InMemoryFileSystem(
        Dictionary<AbsolutePath, AbsolutePath> pathMappings,
        Dictionary<KnownPath, AbsolutePath> knownPathMappings,
        bool convertCrossPlatformPaths) : base(pathMappings, knownPathMappings, convertCrossPlatformPaths)
    {
        _rootDirectory = new InMemoryDirectoryEntry(
            AbsolutePath.FromFullPath(OperatingSystem.IsWindows() ? "C:\\" : "/"),
            null!);

        _directories[_rootDirectory.Path] = _rootDirectory;
    }

    #region Helper Functions

    /// <summary>
    /// Helper function to add a new file to the in-memory file system.
    /// </summary>
    /// <param name="path">Path to the file.</param>
    /// <param name="contents"></param>
    public void AddFile(AbsolutePath path, byte[] contents)
        => InternalAddFile(path, contents);

    /// <summary>
    /// Helper function to add a new file to the in-memory file system.
    /// </summary>
    /// <param name="path">Path to the file.</param>
    /// <param name="contents"></param>
    public void AddFile(AbsolutePath path, string contents)
        => InternalAddFile(path, Encoding.UTF8.GetBytes(contents));

    /// <summary>
    /// Adds an empty file to the in-memory file system.
    /// </summary>
    /// <param name="path">Path to the file.</param>
    public void AddEmptyFile(AbsolutePath path)
        => AddFile(path, Array.Empty<byte>());

    private InMemoryFileEntry InternalAddFile(AbsolutePath path, byte[] contents)
    {
        if (!path.InFolder(_rootDirectory.Path))
            throw new ArgumentException($"Path {path} is not in root directory {_rootDirectory.Path}");

        var directory = GetOrAddDirectory(path.Parent);
        var inMemoryFile = new InMemoryFileEntry(this, path, directory, contents);

        _files.Add(path, inMemoryFile);
        directory.Files.Add(inMemoryFile.Path.RelativeTo(directory.Path), inMemoryFile);

        return inMemoryFile;
    }

    /// <summary>
    /// Adds a new directory to the in-memory file system.
    /// </summary>
    /// <param name="path">Path to the directory.</param>
    public void AddDirectory(AbsolutePath path)
        => GetOrAddDirectory(path);

    /// <summary>
    /// Adds multiple directories to the in-memory file system.
    /// </summary>
    /// <param name="paths">Paths to the directories</param>
    public void AddDirectories([InstantHandle] IEnumerable<AbsolutePath> paths)
    {
        foreach (var path in paths)
        {
            GetOrAddDirectory(path);
        }
    }

    private InMemoryDirectoryEntry GetOrAddDirectory(AbsolutePath path)
    {
        if (!path.InFolder(_rootDirectory.Path))
            throw new ArgumentException($"Path {path} is not in root directory {_rootDirectory.Path}");

        // directory already exists
        if (_directories.TryGetValue(path, out var existingDir))
            return existingDir;

        // directory doesn't exist, we have to create this directory and all
        // parent directories, using a scuffed top-to-bottom implementation for now
        var directoriesToCreate = new Stack<AbsolutePath>();

        var current = path;
        do
        {
            directoriesToCreate.Push(current);
            if (current == current.Parent)
                throw new UnreachableException("Infinite loop should not happen if our code is correct.");

            current = current.Parent;
        } while (current != _rootDirectory.Path);

        var currentParentDirectory = _rootDirectory;
        while (directoriesToCreate.TryPop(out var directoryPath))
        {
            if (!_directories.TryGetValue(directoryPath, out var directory))
            {
                directory = new InMemoryDirectoryEntry(directoryPath, _rootDirectory);

                currentParentDirectory.Directories[directory.Path.RelativeTo(currentParentDirectory.Path)] = directory;
                _directories[directoryPath] = directory;
            }

            currentParentDirectory = directory;
        }

        return _directories[path];
    }

    #endregion

    #region Implementation

    /// <inheritdoc/>
    public override IFileSystem CreateOverlayFileSystem(
        Dictionary<AbsolutePath, AbsolutePath> pathMappings,
        Dictionary<KnownPath, AbsolutePath> knownPathMappings,
        bool convertCrossPlatformPaths = false)
        => new InMemoryFileSystem(pathMappings, knownPathMappings, convertCrossPlatformPaths);

    /// <inheritdoc/>
    protected override IFileEntry InternalGetFileEntry(AbsolutePath path)
    {
        if (_files.TryGetValue(path, out var file)) return file;

        var parentDirectory = GetOrAddDirectory(path);
        var inMemoryFile = new InMemoryFileEntry(this, path, parentDirectory, Array.Empty<byte>());
        return inMemoryFile;
    }

    /// <inheritdoc/>
    protected override IDirectoryEntry InternalGetDirectoryEntry(AbsolutePath path)
    {
        if (_directories.TryGetValue(path, out var directory)) return directory;

        var parentDirectory = InternalGetDirectoryEntry(path.Parent);
        var inMemoryDirectory = new InMemoryDirectoryEntry(path, (InMemoryDirectoryEntry)parentDirectory);
        return inMemoryDirectory;
    }

    /// <inheritdoc/>
    protected override IEnumerable<AbsolutePath> InternalEnumerateFiles(AbsolutePath directory, string pattern, bool recursive)
    {
        return InternalEnumerateFileEntries(directory, pattern, recursive).Select(x => x.Path);
    }

    /// <inheritdoc/>
    protected override IEnumerable<AbsolutePath> InternalEnumerateDirectories(AbsolutePath directory, string pattern, bool recursive)
    {
        if (!_directories.TryGetValue(directory, out var directoryEntry))
            yield break;

        foreach (var subDirectoryEntry in directoryEntry.Directories.Values)
        {
            if (!EnumeratorHelpers.MatchesPattern(pattern, subDirectoryEntry.Path.GetFullPath(), MatchType.Win32))
                continue;

            yield return subDirectoryEntry.Path;
            if (!recursive) continue;

            foreach (var subDirectoryPath in InternalEnumerateDirectories(subDirectoryEntry.Path, pattern, recursive))
            {
                yield return subDirectoryPath;
            }
        }
    }

    /// <inheritdoc/>
    protected override IEnumerable<IFileEntry> InternalEnumerateFileEntries(AbsolutePath directory, string pattern, bool recursive)
    {
        if (!_directories.TryGetValue(directory, out var directoryEntry))
            yield break;

        foreach (var fileEntry in directoryEntry.Files.Values)
        {
            if (!EnumeratorHelpers.MatchesPattern(pattern, fileEntry.Path.GetFullPath(), MatchType.Win32))
                continue;
            yield return fileEntry;
        }

        if (!recursive) yield break;
        foreach (var subDirectoryEntry in directoryEntry.Directories.Values)
        {
            foreach (var subDirectoryFileEntry in InternalEnumerateFileEntries(subDirectoryEntry.Path, pattern, recursive))
            {
                yield return subDirectoryFileEntry;
            }
        }
    }

    /// <inheritdoc/>
    protected override Stream InternalOpenFile(AbsolutePath path, FileMode mode, FileAccess access, FileShare share)
    {
        // TODO: support more file modes
        if (mode != FileMode.Open)
            throw new NotImplementedException();

        if (!_files.TryGetValue(path, out var inMemoryFile))
            throw new FileNotFoundException("File does not exist!", path.FileName);

        var fileContents = inMemoryFile.Contents;
        var ms = new MemoryStream(fileContents, 0, fileContents.Length, access.HasFlag(FileAccess.Write));
        return ms;
    }

    /// <inheritdoc/>
    protected override void InternalCreateDirectory(AbsolutePath path)
        => AddDirectory(path);

    /// <inheritdoc/>
    protected override bool InternalDirectoryExists(AbsolutePath path)
        => _directories.ContainsKey(path);

    /// <inheritdoc/>
    protected override bool InternalFileExists(AbsolutePath path)
        => _files.ContainsKey(path);

    /// <inheritdoc/>
    protected override void InternalDeleteFile(AbsolutePath path)
    {
        if (!_files.TryGetValue(path, out var file))
            throw new FileNotFoundException($"File at {path} does not exist!");

        var parentDirectory = file.ParentDirectory;
        parentDirectory.Files.Remove(path.RelativeTo(parentDirectory.Path));
        _files.Remove(path);
    }

    /// <inheritdoc/>
    protected override void InternalDeleteDirectory(AbsolutePath path, bool recursive)
    {
        if (!_directories.TryGetValue(path, out var directory))
            throw new DirectoryNotFoundException($"Directory at {path} does not exist!");

        if (recursive)
        {
            foreach (var kv in directory.Files)
            {
                var (_, file) = kv;
                _files.Remove(file.Path);
            }

            foreach (var kv in directory.Directories)
            {
                var (_, subDirectory) = kv;
                InternalDeleteDirectory(subDirectory.Path, true);
            }
        }
        else
        {
            if (directory.Files.Any() || directory.Directories.Any())
                throw new IOException($"The directory at {path} is not empty!");

        }

        _directories.Remove(path);
    }

    /// <inheritdoc/>
    protected override void InternalMoveFile(AbsolutePath source, AbsolutePath dest, bool overwrite)
    {
        if (!_files.TryGetValue(source, out var sourceFile))
            throw new FileNotFoundException($"File does not exist at path {source}");

        if (_files.TryGetValue(dest, out var destFile))
        {
            if (!overwrite)
                throw new IOException($"Destination file at {dest} already exist!");

            destFile.Contents = sourceFile.Contents;
        }
        else
        {
            destFile = InternalAddFile(dest, sourceFile.Contents);
        }

        destFile.CreationTime = sourceFile.CreationTime;
        destFile.LastWriteTime = sourceFile.LastWriteTime;
        destFile.IsReadOnly = sourceFile.IsReadOnly;

        DeleteFile(source);
    }

    #endregion
}
