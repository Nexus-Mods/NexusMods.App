using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.NexusWebApi;
using StrawberryShake;

namespace NexusMods.DataModel.SchemaVersions.Migrations;

internal class _0008_AddCollectionId : ITransactionalMigration
{
    public static (MigrationId Id, string Name) IdAndName => MigrationId.ParseNameAndId(nameof(_0008_AddCollectionId));

    private readonly INexusGraphQLClient _graphQlClient;
    private List<(CollectionMetadataId, CollectionId)> _entitiesToUpdate = [];

    public _0008_AddCollectionId(IServiceProvider serviceProvider)
    {
        _graphQlClient = serviceProvider.GetRequiredService<INexusGraphQLClient>();
    }

    public async Task Prepare(IDb db)
    {
        var entitiesToUpdate = db
            .Datoms(CollectionMetadata.Slug)
            .AsModels<CollectionMetadata.ReadOnly>(db)
            .Where(x => !CollectionMetadata.CollectionId.Contains(x))
            .ToArray();

        foreach (var entity in entitiesToUpdate)
        {
            var result = await _graphQlClient.CollectionSlugToId.ExecuteAsync(slug: entity.Slug.Value);
            result.EnsureNoErrors();

            var data = result.Data!.Collection;
            if (data.Slug != entity.Slug.Value) throw new NotSupportedException();

            _entitiesToUpdate.Add((entity, CollectionId.From((ulong)data.Id)));
        }
    }

    public void Migrate(ITransaction tx, IDb db)
    {
        foreach (var tuple in _entitiesToUpdate)
        {
            var (entityId, collectionId) = tuple;
            tx.Add(entityId, CollectionMetadata.CollectionId, collectionId, isRetract: false);
        }
    }
}
