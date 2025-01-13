using System.Reactive.Linq;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Kernel;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.App.UI.Pages.LoadoutPage;
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

    public IObservable<IChangeSet<LoadoutItemModel, EntityId>> ObserveNestedLoadoutItems(LoadoutFilter loadoutFilter)
    {
        // NOTE(erri120): For the nested loadout view, the parent will be a "fake" loadout model
        // created from a LocalFile where the children are the LibraryLinkedLoadoutItems that link
        // back to the LocalFile
        return _connection
            .ObserveDatoms(LocalFile.PrimaryAttribute)
            .AsEntityIds()
            .FilterOnObservable((_, e) => _connection
                .ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItemId, e)
                .AsEntityIds()
                .FilterInStaticLoadout(_connection, loadoutFilter)
                .IsNotEmpty()
            )
            .Transform((_, entityId) =>
            {
                var libraryFile = LibraryFile.Load(_connection.Db, entityId);

                // TODO: dispose
                var cache = new SourceCache<LibraryLinkedLoadoutItem.ReadOnly, EntityId>(static item => item.Id);
                var disposable = _connection
                    .ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItemId, entityId)
                    .AsEntityIds()
                    .FilterInStaticLoadout(_connection, loadoutFilter)
                    .Transform((_, e) => LibraryLinkedLoadoutItem.Load(_connection.Db, e))
                    .Adapt(new SourceCacheAdapter<LibraryLinkedLoadoutItem.ReadOnly, EntityId>(cache))
                    .SubscribeWithErrorLogging();

                var childrenObservable = cache.Connect().Transform(libraryLinkedLoadoutItem => LoadoutDataProviderHelper.ToLoadoutItemModel(_connection, libraryLinkedLoadoutItem, _serviceProvider, false));

                var installedAtObservable = cache.Connect()
                    .Transform(item => item.GetCreatedAt())
                    .QueryWhenChanged(query =>
                    {
                        if (query.Count == 0) return DateTimeOffset.MinValue;
                        return query.Items.Max();
                    });

                var loadoutItemIdsObservable = cache.Connect().Transform(item => item.AsLoadoutItemGroup().AsLoadoutItem().LoadoutItemId);

                var isEnabledObservable = cache.Connect()
                    .TransformOnObservable(x => LoadoutItem.Observe(_connection, x.Id).Select(item => !item.IsDisabled))
                    .QueryWhenChanged(query =>
                    {
                        var isEnabled = Optional<bool>.None;
                        foreach (var isItemEnabled in query.Items)
                        {
                            if (!isEnabled.HasValue)
                            {
                                isEnabled = isItemEnabled;
                            }
                            else
                            {
                                if (isEnabled.Value != isItemEnabled) return (bool?)null;
                            }
                        }

                        return isEnabled.HasValue ? isEnabled.Value : null;
                    }).DistinctUntilChanged(x => x is null ? -1 : x.Value ? 1 : 0);

                LoadoutItemModel model = new FakeParentLoadoutItemModel(loadoutItemIdsObservable, 
                    _serviceProvider, Observable.Return(true), childrenObservable, bitmap: null)
                {
                    NameObservable = Observable.Return(libraryFile.AsLibraryItem().Name),
                    InstalledAtObservable = installedAtObservable,
                    IsEnabledObservable = isEnabledObservable,
                };

                return model;
            });
    }
}
