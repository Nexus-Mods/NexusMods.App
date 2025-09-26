using NexusMods.Hashing.xxHash3;
using NexusMods.Sdk.FileStore;

namespace NexusMods.DataModel;

public class StreamSourceDispatcher : IStreamSourceDispatcher
{
    private readonly IReadOnlyStreamSource[] _sources;

    public StreamSourceDispatcher(IEnumerable<IReadOnlyStreamSource> sources)
    {
        _sources = sources.OrderBy(x => x.Priority).ToArray();
    }

    public async ValueTask<Stream?> OpenAsync(Hash hash, CancellationToken cancellationToken = default)
    {
        foreach (var source in _sources)
        {
            var result = await source.OpenAsync(hash, cancellationToken);
            if (result != null) return result;
        }
        return null;
    }

    public bool Exists(Hash hash)
    {
        foreach (var source in _sources)
            if (source.Exists(hash)) return true;
        return false;
    }
}
