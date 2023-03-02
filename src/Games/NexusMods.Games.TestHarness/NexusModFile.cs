using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.JsonConverters;
using NexusMods.Hashing.xxHash64;
using NexusMods.Networking.NexusWebApi.Types;

namespace NexusMods.Games.TestHarness;

[JsonName("NexusMods.Games.TestHarness.NexusModFile")]
public record NexusModFile : Entity
{
    public required GameDomain Domain { get; init; }
    public required ModId ModId { get; init; }
    public required FileId FileId { get; init; }
    public required string FileName { get; init; }
    public required Hash Hash { get; init; }
    public override EntityCategory Category => EntityCategory.TestData;

    public required DateTime LastUpdated { get; init; }


    protected override Id Persist()
    {
        var id = MakeId(Domain, ModId, FileId);
        Store.Put<Entity>(id, this);
        return id;
    }

    private static Id MakeId(GameDomain gameDomain, ModId modId, FileId fileId)
    {
        return new IdVariableLength(EntityCategory.TestData, $"TestHarness.NexusModFile_{gameDomain.Value}_{modId.Value}_{fileId.Value}");
    }

    public static NexusModFile? Load(IDataStore store, GameDomain gameDomain, ModId modId, FileId fileId)
    {
        var id = MakeId(gameDomain, modId, fileId);
        return store.Get<NexusModFile>(id);
    }

    public static IEnumerable<NexusModFile> LoadAll(IDataStore store, GameDomain gameDomain)
    {
        return store.GetByPrefix<NexusModFile>(new IdVariableLength(EntityCategory.TestData, $"TestHarness.NexusModFile_{gameDomain.Value}_"));
    }
}
