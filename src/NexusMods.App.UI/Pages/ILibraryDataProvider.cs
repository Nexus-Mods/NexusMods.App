using System.Reactive.Linq;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Networking.NexusWebApi.UpdateFilters;
using R3;
using Observable = R3.Observable;

namespace NexusMods.App.UI.Pages;

public interface ILibraryDataProvider
{
    IObservable<IChangeSet<CompositeItemModel<EntityId>, EntityId>> ObserveLibraryItems(LibraryFilter libraryFilter);

    IObservable<int> CountLibraryItems(LibraryFilter libraryFilter);

    /// <summary>
    /// Returns all library files for the given game.
    /// </summary>
    LibraryFile.ReadOnly[] GetAllFiles(GameId gameId, IDb? db = null);
}

public record LibraryFilter(LoadoutId LoadoutId, ILocatableGame Game);

public static class LibraryDataProviderHelper
{
    public static IObservable<int> CountAllLibraryItems(IServiceProvider serviceProvider, LoadoutId loadoutId)
    {
        var connection = serviceProvider.GetRequiredService<IConnection>();
        var loadout = Loadout.Load(connection.Db, loadoutId);

        var libraryFilter = new LibraryFilter(loadout, loadout.InstallationInstance.Game);
        return CountAllLibraryItems(serviceProvider, libraryFilter);
    }

    public static IObservable<int> CountAllLibraryItems(IServiceProvider serviceProvider, LibraryFilter libraryFilter)
    {
        var libraryDataProviders = serviceProvider.GetServices<ILibraryDataProvider>();
        return libraryDataProviders
            .Select(provider => provider.CountLibraryItems(libraryFilter))
            .CombineLatest(static counts => counts.Sum());
    }

    public static IObservable<IChangeSet<LoadoutItem.ReadOnly, EntityId>> GetLinkedLoadoutItems(
        IConnection connection,
        LibraryFilter libraryFilter,
        LibraryItemId libraryItemId)
    {
        return connection
            .ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItemId, libraryItemId)
            .AsEntityIds()
            .Transform(datom => LoadoutItem.Load(connection.Db, datom.E))
            .FilterImmutable(loadoutItem => loadoutItem.LoadoutId.Equals(libraryFilter.LoadoutId));
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

    public static void AddRelatedCollectionsComponent(
        CompositeItemModel<EntityId> itemModel,
        IObservable<IChangeSet<LoadoutItem.ReadOnly, EntityId>> linkedLoadoutItemsObservable)
    {
        AddRelatedCollectionsComponent(itemModel, linkedLoadoutItemsObservable, 
            System.Reactive.Linq.Observable.Return(ChangeSet<CollectionMetadata.ReadOnly, EntityId>.Empty));
    }
    
    public static void AddRelatedCollectionsComponent(
        CompositeItemModel<EntityId> itemModel,
        IObservable<IChangeSet<LoadoutItem.ReadOnly, EntityId>> linkedLoadoutItemsObservable,
        IObservable<IChangeSet<CollectionMetadata.ReadOnly, EntityId>> relatedDownloadedCollectionsObservable)
    {
        var downloadedNamesObservable = relatedDownloadedCollectionsObservable
            .Distinct()
            .Transform(collection => collection.Name)
            .SortBy(static name => name)
            .RemoveKey()
            .StartWithEmpty();
        
        var installedCollectionsObservable = linkedLoadoutItemsObservable
            .Transform(item => item.Parent.AsLoadoutItem())
            .ChangeKey(coll => coll.Id)
            .Distinct()
            .Transform(collection => collection.Name)
            .SortBy(static name => name)
            .RemoveKey()
            .StartWithEmpty();

        // Only take downloaded entries that are not already installed
        var filteredDownloadedCollectionsObservable = downloadedNamesObservable.Except(installedCollectionsObservable);

        var hasCollectionsObservable = installedCollectionsObservable.IsEmpty()
            .CombineLatest(filteredDownloadedCollectionsObservable.IsEmpty(), 
                (installedIsEmpty, relatedIsEmpty) => !installedIsEmpty || !relatedIsEmpty)
            .DistinctUntilChanged()
            .ToObservable();

        itemModel.AddObservable(LibraryColumns.Collections.RelatedCollectionsComponentKey,
            hasCollectionsObservable,
            componentFactory: () => new LibraryComponents.RelatedCollectionsComponent(
                installedCollectionsObservable,
                filteredDownloadedCollectionsObservable
            )
        );
    }

    public static void AddInstallActionComponent(
        CompositeItemModel<EntityId> itemModel,
        IObservable<IChangeSet<LoadoutItem.ReadOnly, EntityId>> linkedItemsObservable)
    {
        itemModel.Add(LibraryColumns.Actions.InstallComponentKey, new LibraryComponents.InstallAction(
            isInstalled: new ValueComponent<bool>(
                initialValue: false,
                valueObservable: linkedItemsObservable.IsNotEmpty(),
                subscribeWhenCreated: true
            )
        ));
    }

    public static void AddInstallActionComponent(
        CompositeItemModel<EntityId> parentItemModel,
        IObservable<MatchesData> matchesObservable)
    {
        parentItemModel.Add(LibraryColumns.Actions.InstallComponentKey, new LibraryComponents.InstallAction(
            matches: new ValueComponent<MatchesData>(
                initialValue: default(MatchesData),
                valueObservable: matchesObservable,
                subscribeWhenCreated: true
            ))
        );
    }

    public static void AddViewChangelogActionComponent(
        CompositeItemModel<EntityId> itemModel,
        bool isEnabled = true)
    {
        itemModel.Add(LibraryColumns.Actions.ViewChangelogComponentKey, new LibraryComponents.ViewChangelogAction(isEnabled));
    }

    public static void AddViewModPageActionComponent(
        CompositeItemModel<EntityId> itemModel,
        bool isEnabled = true)
    {
        itemModel.Add(LibraryColumns.Actions.ViewModPageComponentKey, new SharedComponents.ViewModPageAction(isEnabled));
    }
    
    public static void AddDeleteItemActionComponent(
        CompositeItemModel<EntityId> itemModel,
        bool isEnabled = true)
    {
        itemModel.Add(LibraryColumns.Actions.DeleteItemComponentKey, new LibraryComponents.DeleteItemAction(isEnabled));
    }

    public static void AddHideUpdatesActionComponent(
        CompositeItemModel<EntityId> itemModel,
        bool isEnabled = true,
        bool isVisible = true)
    {
        // Default the observables to simple static values for now
        var isHiddenObservable = R3.Observable.Return(false);
        var itemCountObservable = R3.Observable.Return(1);
        
        itemModel.Add(LibraryColumns.Actions.HideUpdatesComponentKey, new LibraryComponents.HideUpdatesAction(isHiddenObservable, itemCountObservable, Observable.Return(isEnabled), Observable.Return(isVisible)));
    }

    public static void AddHideUpdatesActionComponent(
        CompositeItemModel<EntityId> itemModel,
        IObservable<Optional<ModUpdateOnPage>> unfilteredFileUpdateObservable,
        IModUpdateFilterService filterService,
        Observable<bool> isVisibleObservable)
    {
        // Note(sewer):
        // Determine if the current file is hidden by checking if any of the newer
        // files are hidden in an unfiltered state.
        //
        // The reason we use the unfiltered observable is that we also need to capture
        // cases where *there are no updates*; so we can appropriately disable the button.
        // With a filtered view we *can't do that*, because we may not be seeing updates
        
        // The 'newest' in this context is unfiltered, and is the target we would update to,
        // so this is the correct file to check.
        var isHiddenObservable = unfilteredFileUpdateObservable
            // Because our view is unfiltered, we must listen to filter changes ourselves.
            .CombineLatest(filterService.FilterTrigger.StartWith(System.Reactive.Unit.Default), (optional, _) => optional)
            .Select(optional =>
            {
                if (!optional.HasValue)
                    return false;
                    
                // Check if all newer files are hidden to determine if this update is hidden.
                return optional.Value.NewerFiles.All(x => filterService.IsFileHidden(x.Uid));
            })
            .ToObservable();
        
        // For individual files: count is always 1
        var itemCountObservable = Observable.Return(1);
        
        // Disable the button if there are no newer files (when unfiltered).
        // This means there are no updates, so show/hide is meaningless.
        var isEnabledObservable = unfilteredFileUpdateObservable
            .Select(optional => optional.HasValue)
            .ToObservable();

        itemModel.Add(LibraryColumns.Actions.HideUpdatesComponentKey, new LibraryComponents.HideUpdatesAction(isHiddenObservable, itemCountObservable, isEnabledObservable, isVisibleObservable));
    }

    private record struct FileUpdateDetails(int NumUpdatesHidden, int NumUpdatesShown);
    public static void AddHideUpdatesActionComponent(
        CompositeItemModel<EntityId> itemModel,
        IObservable<Optional<ModUpdatesOnModPage>> unfilteredUpdatedOnModPage,
        IModUpdateFilterService modUpdateFilterService,
        Observable<bool> isVisibleObservable)
    {
        // Note(sewer):
        //
        // Same note as above.
        //
        // The reason we use the unfiltered observable is that we also need to capture
        // cases where *there are no updates*; so we can appropriately disable the button.
        // With a filtered view we *can't do that*, because we may not be seeing updates
        //
        // In addition, we must also be able to capture cases where *all* updates are hidden,
        // not just where they are visible, so we can show proper counters in the UI, as per
        // the design spec. Therefore, we must do the filtering ourselves 😉
        
        // Trigger refresh either if mods on page change, or filter is updated.
        var currentDetails = unfilteredUpdatedOnModPage
            .CombineLatest(modUpdateFilterService.FilterTrigger.StartWith(System.Reactive.Unit.Default), (files, _) => files)
            .Select(files =>
            {
                if (!files.HasValue)
                    return new FileUpdateDetails(0, 0);
                
                var numHidden = 0;
                var numShown = 0;
                foreach (var updateOnPage in files.Value.FileMappings)
                {
                    var areAllUpdatesHidden = updateOnPage.NewerFiles.All(newer => modUpdateFilterService.IsFileHidden(newer.Uid));
                    if (areAllUpdatesHidden)
                        numHidden++;
                    else
                        numShown++;
                }

                return new FileUpdateDetails(numHidden, numShown);
            });

        // Note(sewer):
        // Behaviour per captainsandypants (Slack).
        // 'If any children have updates set to hidden, then the parent should have "Show updates" as the menu item.
        // When selected, this will set all children to show updates.'
        var isHiddenObservable = currentDetails.Select(details => details.NumUpdatesHidden > 0)
            .ToObservable();
        
        // Always show the number of files under the 'Mod Page' item, as this is the number of items
        // affected by the 'Show updates' action.
        var itemCountObservable = currentDetails.Select(details => details.NumUpdatesHidden > 0 ? details.NumUpdatesHidden : details.NumUpdatesShown)
            .ToObservable();
        
        // ReSharper disable once MergeIntoPattern
        var isEnabledObservable = unfilteredUpdatedOnModPage
            .Select(optional => optional.HasValue && optional.Value.HasAnyUpdates)
            .ToObservable();
        
        itemModel.Add(LibraryColumns.Actions.HideUpdatesComponentKey, new LibraryComponents.HideUpdatesAction(isHiddenObservable, itemCountObservable, isEnabledObservable, isVisibleObservable));
    }
}
