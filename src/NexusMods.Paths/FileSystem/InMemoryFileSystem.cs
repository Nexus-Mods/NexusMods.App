using JetBrains.Annotations;

namespace NexusMods.Paths;

/// <summary>
/// Implementation of <see cref="IFileSystem"/> for use with tests.
/// </summary>
[PublicAPI]
public class InMemoryFileSystem : BaseFileSystem
{
    private readonly Dictionary<AbsolutePath, byte[]> _virtualFiles = new();
    private readonly List<AbsolutePath> _directories = new();

    /// <summary>
    /// Constructor.
    /// </summary>
    public InMemoryFileSystem() { }

    public void AddFile(AbsolutePath path, byte[] contents)
    {
        _virtualFiles[path] = contents;
    }

    #region Implementation

    private InMemoryFileSystem(Dictionary<AbsolutePath, AbsolutePath> pathMappings) : base(pathMappings) { }

    /// <inheritdoc/>
    public override IFileSystem CreateOverlayFileSystem(Dictionary<AbsolutePath, AbsolutePath> pathMappings)
        => new InMemoryFileSystem(pathMappings);

    /// <inheritdoc/>
    protected override Stream InternalOpenFile(AbsolutePath path, FileMode mode, FileAccess access, FileShare share)
    {
        // TODO: support more file modes
        if (mode != FileMode.Open)
            throw new NotImplementedException();

        if (!_virtualFiles.TryGetValue(path, out var fileContents))
            throw new FileNotFoundException("File does not exist!", path.FileName);

        var ms = new MemoryStream(fileContents, 0, fileContents.Length, access.HasFlag(FileAccess.Write));
        return ms;
    }

    /// <inheritdoc/>
    protected override void InternalCreateDirectory(AbsolutePath path)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    protected override bool InternalDirectoryExists(AbsolutePath path)
    {
        throw new NotImplementedException();
    }

    #endregion

}
