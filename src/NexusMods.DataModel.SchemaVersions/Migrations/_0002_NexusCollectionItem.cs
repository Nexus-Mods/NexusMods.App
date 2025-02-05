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
public class _0002_NexusCollectionItem
{
    private static (NexusCollectionLoadoutGroup.ReadOnly, LoadoutItem.ReadOnly[])[] GetItemsToUpdate(IDb db)
    {
        return NexusCollectionLoadoutGroup
            .All(db)
            .Select(collectionGroup =>
            {
                var items = LoadoutItem
                    .FindByParent(db, collectionGroup)
                    .Where(static loadoutItem => !NexusCollectionItemLoadoutGroup.IsRequired.IsIn(loadoutItem))
                    .ToArray();

                return (collectionGroup, items);
            }).ToArray();
    }

    private static void Update(IDb db, ITransaction tx, NexusCollectionLoadoutGroup.ReadOnly group, LoadoutItem.ReadOnly[] loadoutItems)
    {
        var itemsToUpdate = loadoutItems.ToDictionary(static item => item.Id, static item => item);

        var downloadStatusArray = group.Revision.Downloads
            .Select(download => (download, CollectionDownloader.GetStatus(download, group.AsCollectionGroup(), db)))
            .ToArray();

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
        foreach (var loadoutItem in loadoutItems)
        {
            tx.Add(loadoutItem.Id, NexusCollectionItemLoadoutGroup.IsRequired, true);
        }
    }
}
