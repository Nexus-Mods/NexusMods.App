using Microsoft.Extensions.DependencyInjection;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Sdk;
using NexusMods.Sdk.FileStore;
using NexusMods.Sdk.Games;

namespace NexusMods.DataModel;

/// <summary>
/// A readonly stream source that reads files from game files on-disk. This is useful for when
/// you need to read a game file during a diagnostic, but don't care if the file is backed up or
/// not.
/// </summary>
public class GameFileStreamSource : IReadOnlyStreamSource
{
    private readonly IConnection _conn;
    private readonly IGameRegistry _gameRegistry;

    public GameFileStreamSource(IConnection conn, IServiceProvider provider)
    {
        _conn = conn;
        _gameRegistry = provider.GetRequiredService<IGameRegistry>();
    }

    public async ValueTask<Stream?> OpenAsync(Hash hash, CancellationToken cancellationToken = default)
    {
        var path = Resolve(hash);
        if (path == null)
            return null;
        return path!.Value.Open(mode: FileMode.Open, access: FileAccess.Read, share: FileShare.Read);
    }

    public bool Exists(Hash hash)
    {
        return Resolve(hash) != null;
    }

    public AbsolutePath? Resolve(Hash hash)
    {
        var options = _conn.Query<(EntityId Game, LocationId Location, RelativePath Path, DateTimeOffset LastModified, Hash Hash, Size Size)>(
            $"select Game, Path.Item1, Path.Item2, Path.Item3, LastModified, Hash, Size from mdb_DiskStateEntry(Db=>{_conn}) WHERE Hash = {hash} ORDER BY LastModified DESC");
        foreach (var option in options)
        {
            var metadata = GameInstallMetadata.Load(_conn.Db, option.Game);
            if (!metadata.IsValid()) continue;
            if (!_gameRegistry.TryGetGameInstallation(metadata.LastSyncedLoadout, out var installation)) continue;

            var resolvedPath = installation.Locations.ToAbsolutePath(new GamePath(option.Location, option.Path));
            if (!resolvedPath.FileExists) continue;
            try
            {
                var info = resolvedPath.FileInfo;
                if (info.LastWriteTimeUtc != option.LastModified || info.Size != option.Size)
                    continue;

                return resolvedPath;
            }
            catch (Exception)
            {
                continue;
            }
        }

        return null;
    }

    public SourcePriority Priority => SourcePriority.Local;
}
