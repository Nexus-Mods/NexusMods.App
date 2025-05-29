using System.Reactive.Joins;
using NexusMods.Abstractions.GOG.Values;
using NexusMods.Cascade;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.GOG.Models;

namespace NexusMods.Networking.GOG;

public class Queries
{
    public static readonly Flow<(ProductId, EntityId, Hash)> AvailableFiles =
        Pattern.Create()
            .Db(out _, GOGLicense.ProductId, out var appId)
            .Match(NexusMods.Abstractions.Games.FileHashes.Queries.HashesForAppId, appId, out var manifest, out var hash)
            .Return(appId, manifest, hash);
}
