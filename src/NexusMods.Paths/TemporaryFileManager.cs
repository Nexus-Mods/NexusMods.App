namespace NexusMods.Paths;

public class TemporaryFileManager : IDisposable, IAsyncDisposable
{
    private readonly AbsolutePath _basePath;
    private readonly bool _deleteOnDispose;

    public TemporaryFileManager() : this(KnownFolders.EntryFolder.Combine("temp"))
    {
    }

    public TemporaryFileManager(AbsolutePath basePath, bool deleteOnDispose = true)
    {
        _deleteOnDispose = deleteOnDispose;
        _basePath = basePath;
        _basePath.CreateDirectory();
    }

    public void Dispose()
    {
        if (!_deleteOnDispose) return;
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
                Thread.Sleep(1000);
            }
        }
    }
    
    
    public async ValueTask DisposeAsync()
    {
        if (!_deleteOnDispose) return;
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
                await Task.Delay(1000);
            }
        }
    }

    public TemporaryPath CreateFile(Extension? ext = default, bool deleteOnDispose = true)
    {
        var path = _basePath.Combine(Guid.NewGuid().ToString());
        if (path.Extension != default)
            path = path.WithExtension(ext ?? Ext.Tmp);
        return new TemporaryPath(path, deleteOnDispose);
    }

    /// <summary>
    /// Returns a new temporary path, that is (optionally) deleted when disposed
    /// </summary>
    /// <returns></returns>
    public TemporaryPath CreateFolder(string prefix = "", bool deleteOnDispose = true)
    {
        var path = _basePath.Combine(prefix + Guid.NewGuid());
        path.CreateDirectory();
        return new TemporaryPath(path, deleteOnDispose);
    }

}

/// <summary>
/// A path that is (optionally) deleted when disposed. 
/// </summary>
public struct TemporaryPath : IDisposable, IAsyncDisposable
{
    public AbsolutePath Path { get; }
    public bool DeleteOnDispose { get; set; }

    public TemporaryPath(AbsolutePath path, bool deleteOnDispose = true)
    {
        Path = path;
        DeleteOnDispose = deleteOnDispose;
    }


    public void Dispose()
    {
        if (DeleteOnDispose)
            Path.Delete();
    }

    public override string ToString()
    {
        return Path.ToString();
    }

    public static implicit operator AbsolutePath(TemporaryPath tp)
    {
        return tp.Path;
    }

    public ValueTask DisposeAsync()
    {
        if (DeleteOnDispose) 
            Path.Delete();
        return ValueTask.CompletedTask;
    }
}