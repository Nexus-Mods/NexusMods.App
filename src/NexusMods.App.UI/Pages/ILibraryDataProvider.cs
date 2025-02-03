using System.Reactive.Linq;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Kernel;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Query;

namespace NexusMods.App.UI.Pages;

public interface ILibraryDataProvider
{
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

    public static void AddInstalledDateComponent(
        CompositeItemModel<EntityId> itemModel,
        IObservable<IChangeSet<LoadoutItem.ReadOnly, EntityId>> linkedItemsObservable)
    {
        var dateObservable = linkedItemsObservable
            .QueryWhenChanged(query =>
            {
                if (query.Count == 0) return Optional<DateTimeOffset>.None;

                return query.Items
                        .Select(static item => item.GetCreatedAt())
                        .OptionalMinBy(item => item);
            });

        itemModel.AddObservable(
            key: SharedColumns.InstalledDate.ComponentKey,
            observable: dateObservable,
            componentFactory: (valueObservable, initialValue) => new DateComponent(
                initialValue,
                valueObservable
            )
        );
    }

    public static void AddInstallActionComponent(
        CompositeItemModel<EntityId> itemModel,
        LibraryItem.ReadOnly libraryItem,
        IObservable<IChangeSet<LoadoutItem.ReadOnly, EntityId>> linkedItemsObservable)
    {
        itemModel.Add(LibraryColumns.Actions.InstallComponentKey, new LibraryComponents.InstallAction(
            isInstalled: new ValueComponent<bool>(
                initialValue: false,
                valueObservable: linkedItemsObservable.IsNotEmpty(),
                subscribeWhenCreated: true
            ),
            itemId: libraryItem
        ));
    }

    public static void AddInstallActionComponent(
        CompositeItemModel<EntityId> parentItemModel,
        IObservable<MatchesData> matchesObservable,
        IObservable<IChangeSet<LibraryItem.ReadOnly, EntityId>> libraryItemsObservable)
    {
        parentItemModel.Add(LibraryColumns.Actions.InstallComponentKey, new LibraryComponents.InstallAction(
            matches: new ValueComponent<MatchesData>(
                initialValue: default(MatchesData),
                valueObservable: matchesObservable,
                subscribeWhenCreated: true
            ),
            childrenItemIdsObservable: libraryItemsObservable.TransformImmutable(static x => x.LibraryItemId)
        ));
    }
}
