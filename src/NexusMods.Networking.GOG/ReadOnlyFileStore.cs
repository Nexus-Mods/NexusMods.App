using NexusMods.Abstractions.Games.FileHashes;
using NexusMods.Abstractions.GOG;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Steam.Values;
using NexusMods.Cascade;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Networking.GOG;

public class ReadOnlyFileStore : IReadOnlyFileStore
{
    private readonly IClient _client;
    private readonly IFileHashesService _hashesService;
    private readonly IQueryResult<Hash> _knownHashes;
    private readonly IQueryResult<(AppId, EntityId, Hash)> _availableFiles;
    
    public ReadOnlyFileStore(IClient client, IFileHashesService fileHashesService, IConnection connection)
    {
        _client = client;
        _hashesService = fileHashesService;
        _ = _client.Login(CancellationToken.None);
        _knownHashes = connection.Topology.QueryNoWait(Queries.AvailableHashes);
        _availableFiles = connection.Topology.QueryNoWait(Queries.AvailableFiles);
    }
    public ValueTask<bool> HaveFile(Hash hash)
    {
        throw new NotImplementedException();
    }

    public Task<Stream?> GetFileStream(Hash hash, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
}
