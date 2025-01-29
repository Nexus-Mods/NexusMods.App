using System.Reactive.Linq;
using DynamicData;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Query;

namespace NexusMods.App.UI.Pages;

public interface ILibraryDataProvider
{
    IObservable<IChangeSet<ILibraryItemModel, EntityId>> ObserveFlatLibraryItems(LibraryFilter libraryFilter);

    IObservable<IChangeSet<ILibraryItemModel, EntityId>> ObserveNestedLibraryItems(LibraryFilter libraryFilter);

    IObservable<IChangeSet<CompositeItemModel<EntityId>, EntityId>> ObserveLibraryItems(LibraryFilter libraryFilter);
}

public class LibraryFilter
{
    public IObservable<LoadoutId> LoadoutObservable { get; }

    public IObservable<ILocatableGame> GameObservable { get; }

    public LibraryFilter(IObservable<LoadoutId> loadoutObservable, IObservable<ILocatableGame> gameObservable)
    {
        LoadoutObservable = loadoutObservable;
        GameObservable = gameObservable;
    }
}

public static class LibraryDataProviderHelper
{
    public static IObservable<IChangeSet<LoadoutItem.ReadOnly, EntityId>> GetLinkedLoadoutItems(
        IConnection connection,
        LibraryFilter libraryFilter,
        LibraryItemId libraryItemId)
    {
        return connection
            .ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItemId, libraryItemId)
            .AsEntityIds()
            .Transform(datom => LoadoutItem.Load(connection.Db, datom.E))
            .FilterOnObservable(loadoutItem =>
                libraryFilter.LoadoutObservable.Select(loadoutId =>
                    loadoutItem.LoadoutId.Equals(loadoutId)
                )
            );
    }

    public static void AddDateComponent(
        CompositeItemModel<EntityId> parentItemModel,
        DateTimeOffset initialValue,
        IObservable<IChangeSet<LoadoutItem.ReadOnly, EntityId>> linkedItemsObservable)
    {
        var dateObservable = linkedItemsObservable
            .QueryWhenChanged(query => query.Items
                .Select(static item => item.GetCreatedAt())
                .Min()
            );

        parentItemModel.Add(SharedColumns.InstalledDate.ComponentKey, new DateComponent(
            initialValue: initialValue,
            valueObservable: dateObservable
        ));
    }
}

public static class QueryHelper
{
    /// <summary>
    /// Filters the source of <see cref="LibraryLinkedLoadoutItem"/> to only contain items
    /// that are installed in the loadout using <see cref="LibraryFilter.LoadoutObservable"/>.
    /// </summary>
    public static IObservable<IChangeSet<Datom, EntityId>> FilterInObservableLoadout(
        this IObservable<IChangeSet<Datom, EntityId>> source,
        IConnection connection,
        LibraryFilter libraryFilter)
    {
        return source.FilterOnObservable((_, e) => libraryFilter.LoadoutObservable.Select(loadoutId =>
            LoadoutItem.Load(connection.Db, e).LoadoutId.Equals(loadoutId))
        );
    }

    public static IObservable<IChangeSet<LibraryLinkedLoadoutItem.ReadOnly, EntityId>> GetLinkedLoadoutItems(
        IConnection connection,
        LibraryItemId libraryItemId,
        LibraryFilter libraryFilter)
    {
        return connection
            .ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItemId, libraryItemId)
            .AsEntityIds()
            .FilterInObservableLoadout(connection, libraryFilter)
            .Transform((_, entityId) => LibraryLinkedLoadoutItem.Load(connection.Db, entityId));
    }
}
