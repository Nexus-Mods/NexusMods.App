using NexusMods.Abstractions.Games.FileHashes;
using NexusMods.Abstractions.GOG;
using NexusMods.Abstractions.GOG.Values;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Steam.Values;
using NexusMods.Cascade;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Sdk;

namespace NexusMods.Networking.GOG;

public class ReadOnlyFileStore : IReadOnlyFileStore
{
    private readonly IClient _client;
    private readonly IFileHashesService _hashesService;
    private readonly IQueryResult<Hash> _knownHashes;
    private readonly IQueryResult<(ProductId ProductId, BuildId BuildId, RelativePath Path, Hash Hash)> _availableFiles;
    
    public ReadOnlyFileStore(IClient client, IFileHashesService fileHashesService, IConnection connection)
    {
        _client = client;
        _hashesService = fileHashesService;
        _ = _client.Login(CancellationToken.None);
        _knownHashes = connection.Topology.Query(Queries.AvailableHashes.Select(r => r.Hash));
        _availableFiles = connection.Topology.Query(Queries.AvailableHashes);
    }
    public ValueTask<bool> HaveFile(Hash hash)
    {
        return ValueTask.FromResult(_knownHashes.Contains(hash));
    }

    public async Task<Stream?> GetFileStream(Hash hash, CancellationToken token = default)
    {
        if (!_availableFiles.TryGetFirst(f => f.Hash == hash, out var row))
            return null;

        var builds = await _client.GetBuilds(row.ProductId, OS.windows, token);
        if (!builds.TryGetFirst(b => b.BuildId == row.BuildId, out var buildInfo))
            return null;
        
        var depot = await _client.GetDepot(buildInfo, token);
        return await _client.GetFileStream(row.ProductId, depot, row.Path, token);
    }
}
