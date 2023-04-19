using System.Globalization;
using NexusMods.Paths.Utilities;

namespace NexusMods.Paths;

/// <summary>
/// Utility for creating temporary folder and files to be later disposed.
/// </summary>
public class TemporaryFileManager : IDisposable
{
    private readonly IFileSystem _fileSystem;
    private readonly AbsolutePath _basePath;
    private readonly bool _deleteOnDispose;

    private bool _isDisposed;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="fileSystem"><see cref="IFileSystem"/> implementation to use.</param>
    /// <param name="basePath">Base directory to use for temporary files. If <c>default</c>, a sub-directory inside <see cref="KnownPath.TempDirectory"/> will be used.</param>
    /// <param name="deleteOnDispose">If <c>true</c>, all files inside <paramref name="basePath"/> will be deleted when disposed.</param>
    public TemporaryFileManager(IFileSystem fileSystem, AbsolutePath basePath = default, bool deleteOnDispose = true)
    {
        _fileSystem = fileSystem;
        _basePath = basePath == default
            ? _fileSystem
                .GetKnownPath(KnownPath.TempDirectory)
                .CombineUnchecked($"NexusMods.App-{Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture)}")
            : basePath;
        _deleteOnDispose = deleteOnDispose;

        _fileSystem.CreateDirectory(_basePath);
    }

    /// <summary>
    /// Returns a new temporary file, that is (optionally) deleted when disposed.
    /// </summary>
    public TemporaryPath CreateFile(Extension? ext = default, bool deleteOnDispose = true)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(TemporaryFileManager));

        var path = _basePath.CombineUnchecked(Guid.NewGuid().ToString());
        if (path.Extension != default)
            path = path.AppendExtension(ext ?? KnownExtensions.Tmp);

        return new TemporaryPath(_fileSystem, path, deleteOnDispose);
    }

    /// <summary>
    /// Returns a new temporary folder, that is (optionally) deleted when disposed.
    /// </summary>
    public TemporaryPath CreateFolder(string prefix = "", bool deleteOnDispose = true)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(TemporaryFileManager));

        var path = _basePath.CombineUnchecked(prefix + Guid.NewGuid());
        _fileSystem.CreateDirectory(path);
        return new TemporaryPath(_fileSystem, path, deleteOnDispose);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">
    /// <c>true</c> to release both managed and unmanaged resources;
    /// <c>false</c> to release only unmanaged resources.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed) return;
        if (disposing)
        {
            if (_deleteOnDispose)
            {
                _fileSystem.DeleteDirectory(_basePath, recursive: true);
            }
        }

        _isDisposed = true;
    }
}

/// <summary>
/// A path that is (optionally) deleted when disposed.
/// </summary>
/// <remarks>
///    If the owner <see cref="TemporaryFileManager"/> is disposed; this path should be disposed too.
/// </remarks>
public readonly struct TemporaryPath : IDisposable, IAsyncDisposable
{
    private readonly IFileSystem _fileSystem;
    private readonly bool _deleteOnDispose;

    /// <summary>
    /// Full path to the temporary location.
    /// </summary>
    public readonly AbsolutePath Path;

    /// <summary>
    /// Represents a temporary folder or file that should be deleted upon disposal.
    /// </summary>
    /// <param name="fileSystem"><see cref="IFileSystem"/> implementation to use.</param>
    /// <param name="path">Full path to the item.</param>
    /// <param name="deleteOnDispose">True to delete when disposed, else false</param>
    public TemporaryPath(IFileSystem fileSystem, AbsolutePath path, bool deleteOnDispose = true)
    {
        _fileSystem = fileSystem;
        _deleteOnDispose = deleteOnDispose;
        Path = path;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose_Impl();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Dispose_Impl();
        return ValueTask.CompletedTask;
    }

    private void Dispose_Impl()
    {
        if (_deleteOnDispose && _fileSystem.FileExists(Path))
            _fileSystem.DeleteFile(Path);
    }

    /// <inheritdoc />
    public override string ToString() => Path.ToString();

    /// <summary/>
    public static implicit operator AbsolutePath(TemporaryPath tp) => tp.Path;
}
