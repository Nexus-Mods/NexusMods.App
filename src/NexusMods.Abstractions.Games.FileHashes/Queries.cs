using NexusMods.Abstractions.Games.FileHashes.Models;
using NexusMods.Abstractions.GOG.Values;
using NexusMods.Abstractions.Steam.Values;
using NexusMods.Cascade;
using NexusMods.Cascade.Patterns;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Cascade;
using NexusMods.Paths;
using NexusMods.Sdk.Hashes;

namespace NexusMods.Abstractions.Games.FileHashes;

public static class Queries
{
    /// <summary>
    /// The currently loaded file hashes database.
    /// </summary>
    public static readonly Inlet<IDb> Db = new();
    
    /// <summary>
    /// A flow of all the hashes for a given steam app ID.
    /// </summary>
    public static readonly Flow<(AppId AppId, EntityId Manifest, Hash Hash)> HashesForAppId =
        Pattern.Create()
            .Db(Db, out var manifest, SteamManifest.AppId, out var appId)
            .Db(Db, manifest, SteamManifest.Files, out var file)
            .Db(Db, file, PathHashRelation.Hash, out var hashRelation)
            .Db(Db, hashRelation, HashRelation.XxHash3, out var xxHash3)
            .Return(appId, manifest, xxHash3);
    
    /// <summary>
    /// A flow of all the hashes for a given gog product ID.
    /// </summary>
    public static readonly Flow<(ProductId ProductId, Hash Hash)> HashesForProductId =
        Pattern.Create()
            .Db(Db, out var manifest, GogBuild.ProductId, out var productId)
            .Db(Db, manifest, GogBuild.Files, out var file)
            .Db(Db, file, PathHashRelation.Hash, out var hashRelation)
            .Db(Db, hashRelation, HashRelation.XxHash3, out var xxHash3)
            .Return(productId, xxHash3);
        
}
