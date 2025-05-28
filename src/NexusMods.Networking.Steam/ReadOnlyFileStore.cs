using NexusMods.Abstractions.Games.FileHashes;
using NexusMods.Abstractions.Games.FileHashes.Models;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Steam;
using NexusMods.Abstractions.Steam.DTOs;
using NexusMods.Abstractions.Steam.Values;
using NexusMods.Cascade;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Sdk.Hashes;

namespace NexusMods.Networking.Steam;

public class ReadOnlyFileStore : IReadOnlyFileStore
{
    private readonly ISteamSession _session;
    private readonly IFileHashesService _hashesService;
    private readonly IQueryResult<Hash> _knownHashes;
    private readonly IQueryResult<(AppId, EntityId, Hash)> _availableFiles;
    
    public ReadOnlyFileStore(ISteamSession session, IFileHashesService fileHashesService, IConnection connection)
    {
        _session = session;
        _hashesService = fileHashesService;
        _ = _session.Connect(CancellationToken.None);
        _knownHashes = connection.Topology.QueryNoWait(Queries.AvailableHashes);
        _availableFiles = connection.Topology.QueryNoWait(Queries.AvailableFiles);
    }
    
    public async ValueTask<bool> HaveFile(Hash hash)
    {
        return _knownHashes.Contains(hash);
    }

    public async Task<Stream?> GetFileStream(Hash hash, CancellationToken token = default)
    {
        var entry = _availableFiles.First(f => f.Item3 == hash);

        var hashesDb = await _hashesService.GetFileHashesDb();
        var manifestMetadata = SteamManifest.Load(hashesDb, entry.Item2);
        var file = manifestMetadata.Files.First(f => f.Hash.XxHash3 == hash);

        var info = await _session.GetManifestContents(manifestMetadata.AppId, manifestMetadata.DepotId, manifestMetadata.ManifestId, manifestMetadata.Name, token);
        return _session.GetFileStream(manifestMetadata.AppId, info, file.Path);
    }
}
