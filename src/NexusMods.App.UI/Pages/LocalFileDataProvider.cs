using DynamicData;
using DynamicData.Aggregation;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Query;
using Observable = System.Reactive.Linq.Observable;
using UIObservableExtensions = NexusMods.App.UI.Extensions.ObservableExtensions;

namespace NexusMods.App.UI.Pages;

[UsedImplicitly]
internal class LocalFileDataProvider : ILibraryDataProvider, ILoadoutDataProvider
{
    private readonly IConnection _connection;
    private readonly IServiceProvider _serviceProvider;

    public LocalFileDataProvider(IServiceProvider serviceProvider)
    {
        _connection = serviceProvider.GetRequiredService<IConnection>();
        _serviceProvider = serviceProvider;
    }

    public IObservable<IChangeSet<ILibraryItemModel, EntityId>> ObserveFlatLibraryItems(LibraryFilter libraryFilter)
    {
        // NOTE(erri120): For the flat library view, we just get all LocalFiles
        return _connection
            .ObserveDatoms(LocalFile.PrimaryAttribute)
            .AsEntityIds()
            .Transform((_, entityId) =>
            {
                var libraryFile = LibraryFile.Load(_connection.Db, entityId);
                return ToLibraryItemModel(libraryFile, libraryFilter);
            });
    }

    private ILibraryItemModel ToLibraryItemModel(LibraryFile.ReadOnly libraryFile, LibraryFilter libraryFilter)
    {
        var linkedLoadoutItemsObservable = QueryHelper.GetLinkedLoadoutItems(_connection, libraryFile.Id, libraryFilter);

        var model = new LocalFileLibraryItemModel(new LocalFile.ReadOnly(libraryFile.Db, libraryFile.IndexSegment, libraryFile.Id), _serviceProvider)
        {
            LinkedLoadoutItemsObservable = linkedLoadoutItemsObservable,
        };
        
        model.Name.Value = libraryFile.AsLibraryItem().Name;
        model.DownloadedDate.Value = libraryFile.GetCreatedAt();
        model.ItemSize.Value = libraryFile.Size;

        return model;
    }

    public IObservable<IChangeSet<ILibraryItemModel, EntityId>> ObserveNestedLibraryItems(LibraryFilter libraryFilter)
    {
        // NOTE(erri120): For the nested library view, design wanted to have a
        // parent for the LocalFile, we create a parent with one child that will
        // both be the same.
        return _connection
            .ObserveDatoms(LocalFile.PrimaryAttribute)
            .AsEntityIds()
            .Transform((_, entityId) =>
            {
                var libraryFile = LibraryFile.Load(_connection.Db, entityId);

                var childrenObservable = UIObservableExtensions.ReturnFactory(() => new ChangeSet<ILibraryItemModel, EntityId>([
                    new Change<ILibraryItemModel, EntityId>(
                        reason: ChangeReason.Add,
                        key: entityId,
                        current: ToLibraryItemModel(libraryFile, libraryFilter)
                    ),
                ]));

                var linkedLoadoutItemsObservable = QueryHelper.GetLinkedLoadoutItems(_connection, entityId, libraryFilter);

                var model = new LocalFileParentLibraryItemModel(new LocalFile.ReadOnly(libraryFile.Db, libraryFile.IndexSegment, libraryFile.Id), _serviceProvider)
                {
                    HasChildrenObservable = Observable.Return(true),
                    ChildrenObservable = childrenObservable,

                    LinkedLoadoutItemsObservable = linkedLoadoutItemsObservable,
                };

                model.Name.Value = libraryFile.AsLibraryItem().Name;
                model.DownloadedDate.Value = libraryFile.GetCreatedAt();
                model.ItemSize.Value = libraryFile.Size;

                return (ILibraryItemModel)model;
            });
    }

    public IObservable<IChangeSet<CompositeItemModel<EntityId>, EntityId>> ObserveLoadoutItems(LoadoutFilter loadoutFilter)
    {
        return LocalFile.ObserveAll(_connection)
            .FilterOnObservable((_, entityId) => _connection
                .ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItemId, entityId)
                .AsEntityIds()
                .FilterInStaticLoadout(_connection, loadoutFilter)
                .IsNotEmpty()
            )
            .Transform(localFile => ToLoadoutItemModel(loadoutFilter, localFile));
    }

    private CompositeItemModel<EntityId> ToLoadoutItemModel(LoadoutFilter loadoutFilter, LocalFile.ReadOnly localFile)
    {
        var linkedItemsObservable = _connection.ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItem, localFile)
            .AsEntityIds()
            .FilterInStaticLoadout(_connection, loadoutFilter)
            .Transform(datom => LoadoutItem.Load(_connection.Db, datom.E))
            .RefCount();

        var hasChildrenObservable = linkedItemsObservable.IsNotEmpty();
        var childrenObservable = linkedItemsObservable.Transform(loadoutItem => LoadoutDataProviderHelper.ToChildItemModel(_connection, loadoutItem));

        var parentItemModel = new CompositeItemModel<EntityId>(localFile.Id)
        {
            HasChildrenObservable = hasChildrenObservable,
            ChildrenObservable = childrenObservable,
        };

        parentItemModel.Add(SharedColumns.Name.StringComponentKey, new StringComponent(value: localFile.AsLibraryFile().AsLibraryItem().Name));
        parentItemModel.Add(SharedColumns.Name.ImageComponentKey, new ImageComponent(value: ImagePipelines.ModPageThumbnailFallback));

        LoadoutDataProviderHelper.AddDateComponent(parentItemModel, localFile.GetCreatedAt(), linkedItemsObservable);
        LoadoutDataProviderHelper.AddIsEnabled(_connection, parentItemModel, linkedItemsObservable);

        return parentItemModel;
    }
}
