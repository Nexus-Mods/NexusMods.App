using JetBrains.Annotations;

namespace NexusMods.Paths;

/// <summary>
/// Implementation of <see cref="IFileSystem"/> for use with tests.
/// </summary>
[PublicAPI]
public class InMemoryFileSystem : BaseFileSystem
{
    private readonly InMemoryDirectory _rootDirectory;

    private readonly Dictionary<AbsolutePath, InMemoryFile> _files = new();
    private readonly Dictionary<AbsolutePath, InMemoryDirectory> _directories = new();

    /// <summary>
    /// Constructor.
    /// </summary>
    public InMemoryFileSystem() : this(new Dictionary<AbsolutePath, AbsolutePath>()) { }

    private InMemoryFileSystem(Dictionary<AbsolutePath, AbsolutePath> pathMappings) : base(pathMappings)
    {
        _rootDirectory = new InMemoryDirectory(
            AbsolutePath.FromFullPath("/"),
            null!,
            new Dictionary<RelativePath, InMemoryFile>(),
            new Dictionary<RelativePath, InMemoryDirectory>());

        _directories[_rootDirectory.Path] = _rootDirectory;
    }

    private record InMemoryEntry(
        AbsolutePath Path,
        InMemoryDirectory ParentDirectory);

    private record InMemoryDirectory(
        AbsolutePath Path,
        InMemoryDirectory ParentDirectory,
        Dictionary<RelativePath, InMemoryFile> Files,
        Dictionary<RelativePath, InMemoryDirectory> Directories) : InMemoryEntry(Path, ParentDirectory);

    private record InMemoryFile(
        AbsolutePath Path,
        InMemoryDirectory ParentDirectory,
        byte[] Contents) : InMemoryEntry(Path, ParentDirectory);

    #region Helper Functions

    /// <summary>
    /// Helper function to add a new file to the in-memory file system.
    /// </summary>
    /// <param name="path">Path to the file.</param>
    /// <param name="contents"></param>
    public void AddFile(AbsolutePath path, byte[] contents)
    {
        if (!path.InFolder(_rootDirectory.Path))
            throw new ArgumentException($"Path {path} is not in root directory {_rootDirectory.Path}");

        var directory = GetOrAddDirectory(path.Parent);
        var inMemoryFile = new InMemoryFile(path, directory, contents);

        _files.Add(path, inMemoryFile);
        directory.Files.Add(inMemoryFile.Path.RelativeTo(directory.Path), inMemoryFile);
    }

    /// <summary>
    /// Adds an empty file to the in-memory file system.
    /// </summary>
    /// <param name="path">Path to the file.</param>
    public void AddEmptyFile(AbsolutePath path)
        => AddFile(path, Array.Empty<byte>());

    /// <summary>
    /// Adds a new directory to the in-memory file system.
    /// </summary>
    /// <param name="path">Path to the directory.</param>
    public void AddDirectory(AbsolutePath path)
    {
        if (!path.InFolder(_rootDirectory.Path))
            throw new ArgumentException($"Path {path} is not in root directory {_rootDirectory.Path}");

        GetOrAddDirectory(path);
    }

    private InMemoryDirectory GetOrAddDirectory(AbsolutePath path)
    {
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
            current = current.Parent;
        } while (current != _rootDirectory.Path);

        var currentParentDirectory = _rootDirectory;
        while (directoriesToCreate.TryPop(out var directoryPath))
        {
            if (!_directories.TryGetValue(directoryPath, out var directory))
            {
                directory = new InMemoryDirectory(
                    directoryPath,
                    _rootDirectory,
                    new Dictionary<RelativePath, InMemoryFile>(),
                    new Dictionary<RelativePath, InMemoryDirectory>());

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
    public override IFileSystem CreateOverlayFileSystem(Dictionary<AbsolutePath, AbsolutePath> pathMappings)
        => new InMemoryFileSystem(pathMappings);

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

    #endregion
}
