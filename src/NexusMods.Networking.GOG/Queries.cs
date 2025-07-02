using NexusMods.Abstractions.GOG.Values;
using NexusMods.Cascade;
using NexusMods.Cascade.Patterns;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions.Cascade;
using NexusMods.Networking.GOG.Models;
using NexusMods.Paths;

namespace NexusMods.Networking.GOG;

public class Queries
{
    public static readonly Flow<(ProductId ProductId, BuildId BuildId, RelativePath Path, Hash Hash)> AvailableHashes =
        Pattern.Create()
            .Db(out _, GOGLicense.ProductId, out var productId)
            .Match(NexusMods.Abstractions.Games.FileHashes.Queries.HashesForProductId, productId, out var buildId, out var path, out var hash)
            .Return(productId, buildId, path, hash);
}
