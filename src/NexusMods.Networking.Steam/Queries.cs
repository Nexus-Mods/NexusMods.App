using NexusMods.Abstractions.Steam.Models;
using NexusMods.Abstractions.Steam.Values;
using NexusMods.Cascade;
using NexusMods.Cascade.Patterns;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Cascade;
using NexusMods.Sdk.Hashes;

namespace NexusMods.Networking.Steam;

public class Queries
{
    public static readonly Flow<(AppId, EntityId, Hash)> AvailableFiles =
        Pattern.Create()
            .Db(out var license, SteamLicenses.AppIds, out var appId)
            .Match(NexusMods.Abstractions.Games.FileHashes.Queries.HashesForAppId, appId, out var manifest, out var hash)
            .Return(appId, manifest, hash);

    public static readonly Flow<Hash> AvailableHashes = AvailableFiles.Select(f => f.Item3);

}
