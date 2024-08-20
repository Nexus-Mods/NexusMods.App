using System.Reactive.Linq;
using DynamicData;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.MnemonicDB.Attributes.Extensions;
using NexusMods.App.UI.Pages.LoadoutPage;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;

namespace NexusMods.App.UI.Pages;

public interface ILoadoutDataProvider
{
    IObservable<IChangeSet<LoadoutItemModel>> ObserveNestedLoadoutItems();
}

public static class LoadoutDataProviderHelper
{
    public static LoadoutItemModel[] ToLoadoutItemModels(IConnection connection, LibraryLinkedLoadoutItem.ReadOnly libraryLinkedLoadoutItem)
    {
        var db = libraryLinkedLoadoutItem.Db;

        // NOTE(erri120): We provide the installer with a "parent" LoadoutItemGroup. The installer
        // has two options: 1) they add all files to this group, 2) they add more groups to the group.
        // The LibraryLinkedLoadoutItem should only contain all files or all groups and this merge
        // figures out what case we have.
        // Heterogeneous data where the group has files and more groups is forbidden but currently not enforced.
        var childDatoms = db.Datoms(LoadoutItem.ParentId, libraryLinkedLoadoutItem.Id);
        var groupDatoms = db.Datoms(LoadoutItemGroup.Group, Null.Instance);
        var groupIds = groupDatoms.MergeByEntityId(childDatoms);
        var onlyHasFiles = groupIds.Count == 0;

        if (onlyHasFiles)
        {
            return [ToLoadoutItemModel(connection, libraryLinkedLoadoutItem.AsLoadoutItemGroup())];
        }

        var arr = GC.AllocateUninitializedArray<LoadoutItemModel>(length: groupIds.Count);
        for (var i = 0; i < groupIds.Count; i++)
        {
            arr[i] = ToLoadoutItemModel(connection, LoadoutItemGroup.Load(db, groupIds[i]));
        }

        return arr;
    }

    private static LoadoutItemModel ToLoadoutItemModel(IConnection connection, LoadoutItemGroup.ReadOnly loadoutItemGroup)
    {
        var observable = LoadoutItemGroup
            .Observe(connection, loadoutItemGroup.Id)
            .Replay(bufferSize: 1)
            .AutoConnect();

        var nameObservable = observable.Select(static item => item.AsLoadoutItem().Name);
        var isEnabledObservable = observable.Select(static item => !item.AsLoadoutItem().IsDisabled);

        // TODO: version (need to ask the game extension)
        // TODO: size (probably with RevisionsWithChildUpdates)

        return new LoadoutItemModel
        {
            LoadoutItemId = loadoutItemGroup.Id,
            InstalledAt = loadoutItemGroup.GetCreatedAt(),
            Name = loadoutItemGroup.AsLoadoutItem().Name,
            IsEnabled = !loadoutItemGroup.AsLoadoutItem().IsDisabled,

            NameObservable = nameObservable,
            IsEnabledObservable = isEnabledObservable,
        };
    }
}
