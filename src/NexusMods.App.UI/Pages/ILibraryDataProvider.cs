using System.Reactive.Linq;
using DynamicData;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Query;

namespace NexusMods.App.UI.Pages;

public interface ILibraryDataProvider
{
    IObservable<IChangeSet<LibraryItemModel, EntityId>> ObserveFlatLibraryItems(LibraryFilter libraryFilter);

    IObservable<IChangeSet<LibraryItemModel, EntityId>> ObserveNestedLibraryItems(LibraryFilter libraryFilter);
}

public class LibraryFilter
{
    public IObservable<LoadoutId> LoadoutObservable { get; }

    public LibraryFilter(IObservable<LoadoutId> loadoutObservable)
    {
        LoadoutObservable = loadoutObservable;
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
