using JetBrains.Annotations;

namespace NexusMods.Paths;

/// <summary>
/// Implementation of <see cref="IFileSystem"/> for use with tests.
/// </summary>
[PublicAPI]
public class InMemoryFileSystem : BaseFileSystem
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public InMemoryFileSystem() { }

    #region Implementation

    private InMemoryFileSystem(Dictionary<AbsolutePath, AbsolutePath> pathMappings) : base(pathMappings) { }

    /// <inheritdoc/>
    public override IFileSystem CreateOverlayFileSystem(Dictionary<AbsolutePath, AbsolutePath> pathMappings)
        => new InMemoryFileSystem(pathMappings);

    /// <inheritdoc/>
    protected override Stream OpenFile(AbsolutePath path, FileMode mode, FileAccess access, FileShare share)
    {
        throw new NotImplementedException();
    }

    #endregion

}
