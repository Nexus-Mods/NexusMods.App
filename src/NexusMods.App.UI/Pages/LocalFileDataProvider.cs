using System.Reactive.Linq;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Kernel;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.MnemonicDB.Attributes.Extensions;
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

    public LocalFileDataProvider(IServiceProvider serviceProvider)
    {
        _connection = serviceProvider.GetRequiredService<IConnection>();
    }

    public IObservable<IChangeSet<LibraryItemModel, EntityId>> ObserveFlatLibraryItems()
    {
        // NOTE(erri120): For the flat library view, we just get all LocalFiles
        return _connection
            .ObserveDatoms(LocalFile.PrimaryAttribute)
            .AsEntityIds()
            .Transform((_, entityId) =>
            {
                var libraryFile = LibraryFile.Load(_connection.Db, entityId);
                return ToLibraryItemModel(libraryFile);
            });
    }

    private LibraryItemModel ToLibraryItemModel(LibraryFile.ReadOnly libraryFile)
    {
        var linkedLoadoutItemsObservable = _connection
            .ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItemId, libraryFile.Id)
            .AsEntityIds()
            .Transform((_, entityId) => LibraryLinkedLoadoutItem.Load(_connection.Db, entityId));

        var model = new LibraryItemModel(libraryFile.Id)
        {
            Name = libraryFile.AsLibraryItem().Name,
            CreatedAt = libraryFile.GetCreatedAt(),
            LinkedLoadoutItemsObservable = linkedLoadoutItemsObservable,
        };

        model.ItemSize.Value = libraryFile.Size.ToString();
        return model;
    }

    public IObservable<IChangeSet<LibraryItemModel, EntityId>> ObserveNestedLibraryItems()
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

                var hasChildrenObservable = Observable.Return(true);
                var childrenObservable = UIObservableExtensions.ReturnFactory(() =>
                {
                    return new ChangeSet<LibraryItemModel, EntityId>([new Change<LibraryItemModel, EntityId>(ChangeReason.Add, entityId, ToLibraryItemModel(libraryFile))]);
                });

                var linkedLoadoutItemsObservable = _connection
                    .ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItemId, entityId)
                    .AsEntityIds()
                    .Transform((_, e) => LibraryLinkedLoadoutItem.Load(_connection.Db, e));

                var numInstalledObservable = _connection
                    .ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItemId, entityId)
                    .Count();

                var model = new FakeParentLibraryItemModel(libraryFile.Id)
                {
                    Name = libraryFile.AsLibraryItem().Name,
                    CreatedAt = libraryFile.GetCreatedAt(),
                    HasChildrenObservable = hasChildrenObservable,
                    ChildrenObservable = childrenObservable,
                    LinkedLoadoutItemsObservable = linkedLoadoutItemsObservable,
                    NumInstalledObservable = numInstalledObservable,
                    LibraryItemsObservable = UIObservableExtensions.ReturnFactory(() => new ChangeSet<LibraryItem.ReadOnly, EntityId>([new Change<LibraryItem.ReadOnly, EntityId>(ChangeReason.Add, entityId, LibraryItem.Load(_connection.Db, entityId))])),
                };

                model.ItemSize.Value = libraryFile.Size.ToString();
                return (LibraryItemModel)model;
            });
    }

    public IObservable<IChangeSet<LoadoutItemModel, EntityId>> ObserveNestedLoadoutItems()
    {
        // NOTE(erri120): For the nested loadout view, the parent will be a "fake" loadout model
        // created from a LocalFile where the children are the LibraryLinkedLoadoutItems that link
        // back to the LocalFile
        return _connection
            .ObserveDatoms(LocalFile.PrimaryAttribute)
            .AsEntityIds()
            .FilterOnObservable((_, e) => _connection
                .ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItemId, e)
                .IsNotEmpty()
            )
            .Transform((_, entityId) =>
            {
                var libraryFile = LibraryFile.Load(_connection.Db, entityId);

                var observable = _connection
                    .ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItemId, entityId)
                    .AsEntityIds()
                    .Transform((_, e) => LibraryLinkedLoadoutItem.Load(_connection.Db, e))
                    .PublishWithFunc(() =>
                    {
                        var changeSet = new ChangeSet<LibraryLinkedLoadoutItem.ReadOnly, EntityId>();
                        var entities = LibraryLinkedLoadoutItem.FindByLibraryItem(_connection.Db, libraryFile.Id);

                        foreach (var entity in entities)
                        {
                            changeSet.Add(new Change<LibraryLinkedLoadoutItem.ReadOnly, EntityId>(ChangeReason.Add, entity.Id, entity));
                        }

                        return changeSet;
                    })
                    .AutoConnect();

                var childrenObservable = observable.Transform(libraryLinkedLoadoutItem => LoadoutDataProviderHelper.ToLoadoutItemModel(_connection, libraryLinkedLoadoutItem));

                var installedAtObservable = observable
                    .Transform(item => item.GetCreatedAt())
                    .QueryWhenChanged(query => query.Items.FirstOrDefault());

                var loadoutItemIdsObservable = observable.Transform(item => item.AsLoadoutItemGroup().AsLoadoutItem().LoadoutItemId);

                var isEnabledObservable = observable
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

                LoadoutItemModel model = new FakeParentLoadoutItemModel
                {
                    NameObservable = Observable.Return(libraryFile.AsLibraryItem().Name),
                    InstalledAtObservable = installedAtObservable,
                    LoadoutItemIdsObservable = loadoutItemIdsObservable,
                    IsEnabledObservable = isEnabledObservable,

                    HasChildrenObservable = Observable.Return(true),
                    ChildrenObservable = childrenObservable,
                };

                return model;
            });
    }
}
