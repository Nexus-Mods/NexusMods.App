using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.FileHashes.Models;
using NexusMods.Abstractions.Games.FileHashes.Rows;
using NexusMods.Cascade;
using NexusMods.Cascade.Patterns;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Cascade;

namespace NexusMods.Abstractions.Games.FileHashes;

public static class Queries
{
    public static readonly Inlet<IDb> FileHashesDb = new();
    
    /*
    public static readonly Flow<LocatorIdPathHash> GogFiles =
        Pattern.Create()
            .Db(FileHashesDb, out var buildEnt, GogBuild.BuildId, out var buildId)
            .Db(FileHashesDb, buildEnt, GogBuild.Files, out var file)
            .Db(FileHashesDb, file, PathHashRelation.Path, out var path)
            .Db(FileHashesDb, file, PathHashRelation.Hash, out var hash)
            .ReturnLocatorIdPathHash(GameStore.GOG, buildId, path, hash);
            */
    
}
