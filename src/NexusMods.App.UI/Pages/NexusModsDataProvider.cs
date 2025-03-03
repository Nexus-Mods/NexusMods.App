using System.Reactive.Linq;
using Avalonia.Media.Imaging;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.Resources;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.Networking.NexusWebApi;
using NuGet.Versioning;
using NexusMods.Paths;

namespace NexusMods.App.UI.Pages;

public class NexusModsDataProvider : ILibraryDataProvider, ILoadoutDataProvider
{
    private readonly IConnection _connection;
    private readonly IModUpdateService _modUpdateService;
    private readonly Lazy<IResourceLoader<EntityId, Bitmap>> _thumbnailLoader;

    public NexusModsDataProvider(IServiceProvider serviceProvider)
    {
        _connection = serviceProvider.GetRequiredService<IConnection>();
        _modUpdateService = serviceProvider.GetRequiredService<IModUpdateService>();

        _thumbnailLoader = new Lazy<IResourceLoader<EntityId, Bitmap>>(() => ImagePipelines.GetModPageThumbnailPipeline(serviceProvider));
    }

    public IObservable<IChangeSet<CompositeItemModel<EntityId>, EntityId>> ObserveLibraryItems(LibraryFilter libraryFilter)
    {
        return NexusModsModPageMetadata
            .ObserveAll(_connection)
            // only show mod pages for the currently selected game
            .FilterImmutable(modPage => modPage.Uid.GameId.Equals(libraryFilter.Game.GameId))
            // only show mod pages that have library files
            .FilterOnObservable(modPage => _connection
                .ObserveDatoms(NexusModsLibraryItem.ModPageMetadata, modPage)
                .IsNotEmpty()
            )
            .Transform(modPage => ToLibraryItemModel(modPage, libraryFilter));
    }

    private CompositeItemModel<EntityId> ToLibraryItemModel(NexusModsModPageMetadata.ReadOnly modPage, LibraryFilter libraryFilter)
    {
        var libraryItems = _connection
            .ObserveDatoms(NexusModsLibraryItem.ModPageMetadata, modPage)
            .AsEntityIds()
            .Transform(datom => NexusModsLibraryItem.Load(_connection.Db, datom.E))
            .RefCount();

        var linkedLoadoutItemsObservable = libraryItems.MergeManyChangeSets(libraryItem => LibraryDataProviderHelper.GetLinkedLoadoutItems(_connection, libraryFilter, libraryItem.Id));

        var hasChildrenObservable = libraryItems.IsNotEmpty();
        var childrenObservable = libraryItems.Transform(libraryItem => ToLibraryItemModel(libraryItem, libraryFilter));

        var parentItemModel = new CompositeItemModel<EntityId>(modPage.Id)
        {
            HasChildrenObservable = hasChildrenObservable,
            ChildrenObservable = childrenObservable,
        };

        parentItemModel.Add(SharedColumns.Name.StringComponentKey, new StringComponent(value: modPage.Name));
        parentItemModel.Add(SharedColumns.Name.ImageComponentKey, ImageComponent.FromPipeline(_thumbnailLoader.Value, modPage.Id, initialValue: ImagePipelines.ModPageThumbnailFallback));

        // Size: sum of library files
        var sizeObservable = libraryItems
            .TransformImmutable(static item => LibraryFile.Size.GetOptional(item).ValueOr(() => Size.Zero))
            .ForAggregation()
            .Sum(static size => (long)size.Value)
            .Select(static size => Size.FromLong(size));

        parentItemModel.Add(LibraryColumns.ItemSize.ComponentKey, new SizeComponent(
            initialValue: Size.Zero,
            valueObservable: sizeObservable
        ));

        // Downloaded date: most recent downloaded file date
        var downloadedDateObservable = libraryItems
            .TransformImmutable(static item => item.GetCreatedAt())
            .QueryWhenChanged(query => query.Items.OptionalMaxBy(item => item).ValueOr(DateTimeOffset.MinValue));

        parentItemModel.Add(LibraryColumns.DownloadedDate.ComponentKey, new DateComponent(
            initialValue: modPage.GetCreatedAt(),
            valueObservable: downloadedDateObservable
        ));

        // Version: highest version number
        var currentVersionObservable = libraryItems
            .TransformImmutable(static item =>
            {
                // NOTE(erri120, sewer): simplest version parsing for now
                var rawVersion = item.FileMetadata.Version;
                _ = NuGetVersion.TryParse(rawVersion, out var parsedVersion);

                return (rawVersion, parsedVersion: Optional<NuGetVersion>.Create(parsedVersion));
            })
            .QueryWhenChanged(static query =>
            {
                var max = query.Items.OptionalMaxBy(static tuple => tuple.parsedVersion.ValueOr(new NuGetVersion(0, 0, 0)));
                if (!max.HasValue) return string.Empty;
                return max.Value.rawVersion;
            });

        parentItemModel.Add(LibraryColumns.ItemVersion.CurrentVersionComponentKey, new StringComponent(
            initialValue: string.Empty,
            valueObservable: currentVersionObservable
        ));

        // Update available
        var newestModPageObservable = _modUpdateService.GetNewestModPageVersionObservable(modPage);
        var currentUpdateVersionObservable = newestModPageObservable
            .Select(static optional => !optional.HasValue ? "" : optional.Value.MappingWithNewestFile().File.Version)
            .OnUI();
        var newestVersionObservable = newestModPageObservable
            .Select(static optional => optional.Convert(static updatesOnPage => updatesOnPage.NewestFile().Version))
            .OnUI();

        parentItemModel.AddObservable(
            key: LibraryColumns.ItemVersion.NewVersionComponentKey,
            observable: newestVersionObservable,
            componentFactory: (valueObservable, initialValue) => new LibraryComponents.NewVersionAvailable(
                currentVersion: new StringComponent(
                    initialValue: string.Empty,
                    valueObservable: currentUpdateVersionObservable
                ),
                newVersion: initialValue,
                newVersionObservable: valueObservable
            )
        );

        // Update button
        var newestFiles = _modUpdateService.GetNewestModPageVersionObservable(modPage);

        parentItemModel.AddObservable(
            key: LibraryColumns.Actions.UpdateComponentKey,
            observable: newestFiles,
            componentFactory: static (valueObservable, initialValue) => new LibraryComponents.UpdateAction(
                initialValue,
                valueObservable
            )
        );

        LibraryDataProviderHelper.AddInstalledDateComponent(parentItemModel, linkedLoadoutItemsObservable);

        var matchesObservable = libraryItems
            .TransformOnObservable(libraryItem => LibraryDataProviderHelper.GetLinkedLoadoutItems(_connection, libraryFilter, libraryItem.Id).IsNotEmpty())
            .QueryWhenChanged(query =>
            {
                var (numInstalled, numTotal) = (0, 0);
                foreach (var isInstalled in query.Items)
                {
                    numInstalled += isInstalled ? 1 : 0;
                    numTotal++;
                }

                return new MatchesData(numInstalled, numTotal);
            });

        LibraryDataProviderHelper.AddInstallActionComponent(parentItemModel, matchesObservable, libraryItems.TransformImmutable(static x => x.AsLibraryItem()));

        return parentItemModel;
    }

    private CompositeItemModel<EntityId> ToLibraryItemModel(NexusModsLibraryItem.ReadOnly libraryItem, LibraryFilter libraryFilter)
    {
        var linkedLoadoutItemsObservable = LibraryDataProviderHelper
            .GetLinkedLoadoutItems(_connection, libraryFilter, libraryItem.Id)
            .RefCount();

        var fileMetadata = libraryItem.FileMetadata;

        var itemModel = new CompositeItemModel<EntityId>(libraryItem.Id);

        itemModel.Add(SharedColumns.Name.StringComponentKey, new StringComponent(value: fileMetadata.Name));
        itemModel.Add(LibraryColumns.DownloadedDate.ComponentKey, new DateComponent(value: libraryItem.GetCreatedAt()));
        itemModel.Add(LibraryColumns.ItemVersion.CurrentVersionComponentKey, new StringComponent(value: fileMetadata.Version));

        if (libraryItem.FileMetadata.Size.TryGet(out var size))
            itemModel.Add(LibraryColumns.ItemSize.ComponentKey, new SizeComponent(value: size));

        LibraryDataProviderHelper.AddInstalledDateComponent(itemModel, linkedLoadoutItemsObservable);
        LibraryDataProviderHelper.AddInstallActionComponent(itemModel, libraryItem.AsLibraryItem(), linkedLoadoutItemsObservable);

        // Update available
        var newestVersionObservable = _modUpdateService
            .GetNewestFileVersionObservable(fileMetadata)
            .Select(static optional => optional.Convert(static updateOnPage => updateOnPage.NewestFile.Version))
            .OnUI();

        itemModel.AddObservable(
            key: LibraryColumns.ItemVersion.NewVersionComponentKey,
            observable: newestVersionObservable,
            componentFactory: (valueObservable, initialValue) => new LibraryComponents.NewVersionAvailable(
                currentVersion: new StringComponent(value: libraryItem.FileMetadata.Version),
                newVersion: initialValue,
                newVersionObservable: valueObservable
            )
        );

        // Update button
        var newestFile = _modUpdateService.GetNewestFileVersionObservable(fileMetadata);

        itemModel.AddObservable(
            key: LibraryColumns.Actions.UpdateComponentKey,
            observable: newestFile,
            componentFactory: static (valueObservable, initialValue) => new LibraryComponents.UpdateAction(
                initialValue,
                valueObservable
            )
        );

        return itemModel;
    }

    private IObservable<IChangeSet<NexusModsModPageMetadata.ReadOnly, EntityId>> FilterLoadoutItems(LoadoutFilter loadoutFilter)
    {
        return NexusModsModPageMetadata
            .ObserveAll(_connection)
            .FilterOnObservable((_, modPageEntityId) => _connection
                .ObserveDatoms(NexusModsLibraryItem.ModPageMetadataId, modPageEntityId)
                .FilterOnObservable((d, _) => _connection
                    .ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItemId, d.E)
                    .AsEntityIds()
                    .FilterInStaticLoadout(_connection, loadoutFilter)
                    .IsNotEmpty())
                .IsNotEmpty()
            );
    }

    public IObservable<IChangeSet<CompositeItemModel<EntityId>, EntityId>> ObserveLoadoutItems(LoadoutFilter loadoutFilter)
    {
        return FilterLoadoutItems(loadoutFilter).Transform(modPage =>
        {
            var linkedItemsObservable = _connection
                .ObserveDatoms(NexusModsLibraryItem.ModPageMetadataId, modPage.Id).AsEntityIds()
                .FilterOnObservable((_, e) => _connection.ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItemId, e).IsNotEmpty())
                // NOTE(erri120): DynamicData 9.0.4 is broken for value types because it uses ReferenceEquals. Temporary workaround is a custom equality comparer.
                .MergeManyChangeSets((_, e) => _connection.ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItemId, e).AsEntityIds(), equalityComparer: DatomEntityIdEqualityComparer.Instance)
                .FilterInStaticLoadout(_connection, loadoutFilter)
                .Transform(datom => LoadoutItem.Load(_connection.Db, datom.E))
                .RefCount();

            var hasChildrenObservable = linkedItemsObservable.IsNotEmpty();
            var childrenObservable = linkedItemsObservable.Transform(loadoutItem => LoadoutDataProviderHelper.ToChildItemModel(_connection, loadoutItem));

            var parentItemModel = new CompositeItemModel<EntityId>(modPage.Id)
            {
                HasChildrenObservable = hasChildrenObservable,
                ChildrenObservable = childrenObservable,
            };

            parentItemModel.Add(SharedColumns.Name.StringComponentKey, new StringComponent(value: modPage.Name));
            parentItemModel.Add(SharedColumns.Name.ImageComponentKey, ImageComponent.FromPipeline(_thumbnailLoader.Value, modPage.Id, initialValue: ImagePipelines.ModPageThumbnailFallback));

            LoadoutDataProviderHelper.AddDateComponent(parentItemModel, modPage.GetCreatedAt(), linkedItemsObservable);
            LoadoutDataProviderHelper.AddCollections(parentItemModel, linkedItemsObservable);
            LoadoutDataProviderHelper.AddIsEnabled(_connection, parentItemModel, linkedItemsObservable);

            return parentItemModel;
        });
    }

    public IObservable<int> CountLoadoutItems(LoadoutFilter loadoutFilter)
    {
        return FilterLoadoutItems(loadoutFilter).QueryWhenChanged(static query => query.Count).Prepend(0);
    }
}

file class DatomEntityIdEqualityComparer : IEqualityComparer<Datom>
{
    public static readonly IEqualityComparer<Datom> Instance = new DatomEntityIdEqualityComparer();

    public bool Equals(Datom x, Datom y)
    {
        return x.E == y.E;
    }

    public int GetHashCode(Datom obj)
    {
        return obj.E.GetHashCode();
    }
}
