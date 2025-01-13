using System.Reactive.Linq;
using DynamicData;
using DynamicData.Kernel;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.App.UI.Pages.LoadoutPage;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;

namespace NexusMods.App.UI.Pages;

public interface ILoadoutDataProvider
{
    IObservable<IChangeSet<LoadoutItemModel, EntityId>> ObserveNestedLoadoutItems(LoadoutFilter loadoutFilter);
}

public class LoadoutFilter
{
    public required LoadoutId LoadoutId { get; init; }
    public required Optional<LoadoutItemGroupId> CollectionGroupId { get; init; }
}

public static class LoadoutDataProviderHelper
{
    public static IObservable<IChangeSet<Datom, EntityId>> FilterInStaticLoadout(
        this IObservable<IChangeSet<Datom, EntityId>> source,
        IConnection connection,
        LoadoutFilter loadoutFilter)
    {
        var filterByCollection = loadoutFilter.CollectionGroupId.HasValue;
        return source.Filter(datom =>
        {
            var item = LoadoutItem.Load(connection.Db, datom.E);
            if (!item.LoadoutId.Equals(loadoutFilter.LoadoutId)) return false;
            if (filterByCollection) 
                return item.IsChildOf(loadoutFilter.CollectionGroupId.Value);
            return true;
        });
    }

    public static LoadoutItemModel ToLoadoutItemModel(IConnection connection, LibraryLinkedLoadoutItem.ReadOnly libraryLinkedLoadoutItem, IServiceProvider serviceProvider, bool loadThumbnail)
    {
        // NOTE(erri120): We'll only show the library linked loadout item group for now.
        // Showing sub-groups, like SMAPI mods for Stardew Valley, will not be shown for now.
        // We'll probably have a setting or something that the game extension can control.

        return ToLoadoutItemModel(connection, libraryLinkedLoadoutItem.AsLoadoutItemGroup(), serviceProvider, loadThumbnail);

        // var db = libraryLinkedLoadoutItem.Db;

        // NOTE(erri120): We provide the installer with a "parent" LoadoutItemGroup. The installer
        // has two options: 1) they add all files to this group, 2) they add more groups to the group.
        // The LibraryLinkedLoadoutItem should only contain all files or all groups and this merge
        // figures out what case we have.
        // Heterogeneous data where the group has files and more groups is forbidden but currently not enforced.
        // var childDatoms = db.Datoms(LoadoutItem.ParentId, libraryLinkedLoadoutItem.Id);
        // var groupDatoms = db.Datoms(LoadoutItemGroup.Group, Null.Instance);
        // var groupIds = groupDatoms.MergeByEntityId(childDatoms);
        // var onlyHasFiles = groupIds.Count == 0;
        //
        // return [ToLoadoutItemModel(connection, libraryLinkedLoadoutItem.AsLoadoutItemGroup())];
        // if (onlyHasFiles)
        // {
        //     return [ToLoadoutItemModel(connection, libraryLinkedLoadoutItem.AsLoadoutItemGroup())];
        // }
        //
        // var arr = GC.AllocateUninitializedArray<LoadoutItemModel>(length: groupIds.Count);
        // for (var i = 0; i < groupIds.Count; i++)
        // {
        //     arr[i] = ToLoadoutItemModel(connection, LoadoutItemGroup.Load(db, groupIds[i]));
        // }

        // return arr;
    }

    private static LoadoutItemModel ToLoadoutItemModel(IConnection connection, LoadoutItemGroup.ReadOnly loadoutItemGroup, IServiceProvider serviceProvider, bool loadThumbnail)
    {
        var observable = LoadoutItemGroup
            .Observe(connection, loadoutItemGroup.Id)
            .Replay(bufferSize: 1)
            .AutoConnect();

        var nameObservable = observable.Select(static item => item.AsLoadoutItem().Name);
        var isEnabledObservable = observable.Select<LoadoutItemGroup.ReadOnly, bool?>(static item => !item.AsLoadoutItem().IsDisabled);

        // TODO: version (need to ask the game extension)
        // TODO: size (probably with RevisionsWithChildUpdates)

        var model = new LoadoutItemModel(loadoutItemGroup.Id, serviceProvider, connection, loadThumbnail, loadThumbnail)
        {
            NameObservable = nameObservable,
            IsEnabledObservable = isEnabledObservable,
        };

        model.InstalledAt.Value = loadoutItemGroup.GetCreatedAt();
        return model;
    }
}
