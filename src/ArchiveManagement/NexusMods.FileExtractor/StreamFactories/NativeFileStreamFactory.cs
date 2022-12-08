using NexusMods.Interfaces.Streams;

namespace NexusMods.Paths;

public class NativeFileStreamFactory : IStreamFactory
{
    private AbsolutePath _file;

    private DateTime? _lastModifiedCache;

    public NativeFileStreamFactory(AbsolutePath file, IPath path)
    {
        _file = file;
        Name = path;
    }

    public NativeFileStreamFactory(AbsolutePath file)
    {
        _file = file;
        Name = file;
    }

    public Size Size => _file.Length;

    public ValueTask<Stream> GetStream()
    {
        return new ValueTask<Stream>(_file.Open(FileMode.Open, FileAccess.Read, FileShare.Read));
    }

    public DateTime LastModifiedUtc
    {
        get
        {
            _lastModifiedCache ??= _file.LastWriteTimeUtc;
            return _lastModifiedCache.Value;
        }
    }

    public IPath Name { get; }

    public AbsolutePath FullPath => (AbsolutePath) Name;
}