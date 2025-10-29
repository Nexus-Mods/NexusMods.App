using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.NexusWebApi;
using StrawberryShake;

namespace NexusMods.DataModel.SchemaVersions.Migrations;

internal class _0008_AddCollectionId : TransactionalMigration
{
    public static (MigrationId Id, string Name) IdAndName => MigrationId.ParseNameAndId(nameof(_0008_AddCollectionId));

    private readonly ILogger _logger;
    private readonly IGraphQlClient _graphQlClient;
    private List<(CollectionMetadataId, CollectionId)> _entitiesToUpdate = [];

    public _0008_AddCollectionId(IServiceProvider serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<_0008_AddCollectionId>>();
        _graphQlClient = serviceProvider.GetRequiredService<IGraphQlClient>();
    }

    public async Task Prepare(IDb db)
    {
        var entitiesToUpdate = db
            .Datoms(CollectionMetadata.Slug)
            .AsModels<CollectionMetadata.ReadOnly>(db)
            .Where(x => !CollectionMetadata.CollectionId.Contains(x))
            .ToArray();

        if (entitiesToUpdate.Length == 0) return;

        var duplicateSlugs = entitiesToUpdate
            .GroupBy(x => x.Slug)
            .Select(x => (x.Key, x.Count()))
            .Where(x => x.Item2 > 1)
            .ToArray();

        foreach (var tuple in duplicateSlugs)
        {
            var (slug, count) = tuple;
            _logger.LogError("{Count} collections use the same slug `{Slug}`", count, slug);
        }

        ulong id = 0;
        foreach (var entity in entitiesToUpdate)
        {
            if (duplicateSlugs.Any(x => x.Key == entity.Slug))
            {
                _entitiesToUpdate.Add((entity, CollectionId.From(id++)));
                continue;
            }

            var result = await _graphQlClient.QueryCollectionId(entity.Slug);
            if (result.HasErrors)
            {
                _logger.LogError("Unable to get collection ID for collection `{Slug}`", entity.Slug);
                continue;
            }

            var collectionId = result.AssertHasData();
            _entitiesToUpdate.Add((entity, collectionId));
        }
    }

    public void Migrate(Transaction tx, IDb db)
    {
        foreach (var tuple in _entitiesToUpdate)
        {
            var (entityId, collectionId) = tuple;
            tx.Add(entityId, CollectionMetadata.CollectionId, collectionId, isRetract: false);
        }
    }
}
