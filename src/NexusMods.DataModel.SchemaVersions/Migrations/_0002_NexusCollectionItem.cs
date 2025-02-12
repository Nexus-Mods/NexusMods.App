using Avalonia.Media.TextFormatting;
using NexusMods.Abstractions.Collections;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Collections;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.DataModel.SchemaVersions.Migrations;

/// <summary>
/// Migration to add a reference to collection downloads on loadout items.
/// </summary>
internal class _0002_NexusCollectionItem : ITransactionalMigration
{
    public static (MigrationId Id, string Name) IdAndName { get; } = MigrationId.ParseNameAndId(nameof(_0002_NexusCollectionItem));

    private Data[] _data = [];
    public async Task Prepare(IDb db)
    {
        await Task.Yield();
        _data = NexusCollectionLoadoutGroup
            .All(db)
            .Select(collectionGroup =>
            {
                var items = LoadoutItem
                    .FindByParent(db, collectionGroup)
                    .Where(static loadoutItem => !NexusCollectionItemLoadoutGroup.IsRequired.IsIn(loadoutItem))
                    .ToDictionary(static item => item.Id, static item => item);

                var downloadStatusArray = collectionGroup.Revision.Downloads
                    .Select(download => new KeyValuePair<CollectionDownload.ReadOnly, CollectionDownloadStatus>(download, CollectionDownloader.GetStatus(download, collectionGroup.AsCollectionGroup(), db)))
                    .ToArray();

                return new Data(collectionGroup, items, downloadStatusArray);
            }).ToArray();
    }

    public void Migrate(ITransaction tx, IDb db)
    {
        foreach (var data in _data)
        {
            var (group, itemsToUpdate, downloadStatusArray) = data;

            foreach (var tuple in downloadStatusArray)
            {
                var (downloadEntity, downloadStatus) = tuple;
                if (downloadStatus.IsInstalled(out var loadoutItem))
                {
                    tx.Add(loadoutItem.Id, NexusCollectionItemLoadoutGroup.Download, downloadEntity);
                    tx.Add(loadoutItem.Id, NexusCollectionItemLoadoutGroup.IsRequired, downloadEntity.IsRequired);
                    itemsToUpdate.Remove(loadoutItem.Id);
                }
            }

            // NOTE(erri120): We couldn't find associated downloads for these remaining items,
            // as such, we'll set a default value and hope for the best.
            foreach (var loadoutItem in itemsToUpdate.Values)
            {
                tx.Add(loadoutItem.Id, NexusCollectionItemLoadoutGroup.IsRequired, true);
            }
        }
    }

    private record struct Data(
        NexusCollectionLoadoutGroup.ReadOnly Group,
        Dictionary<EntityId, LoadoutItem.ReadOnly> ItemsToUpdate,
        KeyValuePair<CollectionDownload.ReadOnly, CollectionDownloadStatus>[] Downloads
    );
}

