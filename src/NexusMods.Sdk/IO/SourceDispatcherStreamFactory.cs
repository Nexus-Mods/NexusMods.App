using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;
using NexusMods.Sdk.FileStore;

namespace NexusMods.Sdk.IO;

public class SourceDispatcherStreamFactory : IStreamFactory
{
    private readonly IStreamSourceDispatcher _dispatcher;
    private readonly Hash _hash;
    
    public SourceDispatcherStreamFactory(RelativePath fileName, Hash hash, IStreamSourceDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        _hash = hash;
        FileName = fileName;
    }

    public RelativePath FileName { get; }
    public async ValueTask<Stream> GetStreamAsync()
    {
        var stream = await _dispatcher.OpenAsync(_hash);
        return stream ?? throw new FileNotFoundException($"The file {FileName} with hash '{_hash}' could not be found in any stream source.");
    }
}
