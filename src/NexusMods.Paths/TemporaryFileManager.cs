using NexusMods.Paths.Utilities;

namespace NexusMods.Paths;

/// <summary>
/// Utility for creating temporary folder and files to be later disposed.<br/>
/// Example Usage: `using var manager = new TemporaryFileManager()`.
/// </summary>
public class TemporaryFileManager : IDisposable, IAsyncDisposable
{
    private readonly AbsolutePath _basePath;
    private readonly bool _deleteOnDispose;
    
    /// <summary>
    /// Utility for creating temporary folder and files to be later disposed.
    /// </summary>
    public TemporaryFileManager() : this(KnownFolders.EntryFolder.Join("temp")) { }
    
    /// <summary>
    /// Utility for creating temporary folder and files to be later disposed.
    /// </summary>
    /// <param name="basePath">Path inside which the temporary data will be stored.</param>
    /// <param name="deleteOnDispose">True if all the paths should be deleted on disposal, else false.</param>
    public TemporaryFileManager(AbsolutePath basePath = default, bool deleteOnDispose = true)
    {
        if (basePath == default)
            basePath = KnownFolders.EntryFolder.Join("temp");
        
        _deleteOnDispose = deleteOnDispose;
        _basePath = basePath;
        _basePath.CreateDirectory();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _ = Dispose_Impl(false).Preserve();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await Dispose_Impl(true);
        GC.SuppressFinalize(this);
    }
    
    private async ValueTask Dispose_Impl(bool waitAsync)
    {        
        if (!_deleteOnDispose) 
            return;
        
        for (var retries = 0; retries < 10; retries++)
        {
            try
            {
                if (!_basePath.DirectoryExists())
                    return;

                _basePath.DeleteDirectory();
                return;
            }
            catch (IOException)
            {
                if (!waitAsync)
                    Thread.Sleep(1000);
                else
                    await Task.Delay(1000);
            }
        }
    }

    /// <summary>
    /// Returns a new temporary file, that is (optionally) deleted when disposed.
    /// </summary>
    public TemporaryPath CreateFile(Extension? ext = default, bool deleteOnDispose = true)
    {
        var path = _basePath.Join(Guid.NewGuid().ToString());
        if (path.Extension != default)
            path = path.WithExtension(ext ?? KnownExtensions.Tmp);
        
        return new TemporaryPath(path, deleteOnDispose);
    }

    /// <summary>
    /// Returns a new temporary folder, that is (optionally) deleted when disposed.
    /// </summary>
    public TemporaryPath CreateFolder(string prefix = "", bool deleteOnDispose = true)
    {
        var path = _basePath.Join(prefix + Guid.NewGuid());
        path.CreateDirectory();
        return new TemporaryPath(path, deleteOnDispose);
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
    /// <summary>
    /// Full path to the temporary location.
    /// </summary>
    public AbsolutePath Path { get; }
    
    /// <summary>
    /// True if deleted on dispose, else false.
    /// </summary>
    private bool DeleteOnDispose { get; }

    /// <summary>
    /// Represents a temporary folder or file that should be deleted upon disposal.
    /// </summary>
    /// <param name="path">Full path to the item.</param>
    /// <param name="deleteOnDispose">True to delete when disposed, else false</param>
    public TemporaryPath(AbsolutePath path, bool deleteOnDispose = true)
    {
        Path = path;
        DeleteOnDispose = deleteOnDispose;
    }
    
    /// <inheritdoc />
    public void Dispose()
    {
        if (DeleteOnDispose)
            Path.Delete();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (DeleteOnDispose) 
            Path.Delete();
        
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override string ToString() => Path.ToString();

    /// <summary/>
    public static implicit operator AbsolutePath(TemporaryPath tp) => tp.Path;
}