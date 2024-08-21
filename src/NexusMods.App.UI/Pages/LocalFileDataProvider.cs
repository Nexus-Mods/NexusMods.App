using System.Reactive.Linq;
using DynamicData;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.MnemonicDB.Attributes.Extensions;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.App.UI.Pages.LoadoutPage;
using NexusMods.Extensions.BCL;
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

    public IObservable<IChangeSet<LibraryItemModel>> ObserveFlatLibraryItems()
    {
        // NOTE(erri120): For the flat library view, we just get all LocalFiles
        return _connection
            .ObserveDatoms(LocalFile.PrimaryAttribute)
            .Transform(datom =>
            {
                var libraryFile = LibraryFile.Load(_connection.Db, datom.E);
                return ToLibraryItemModel(libraryFile);
            })
            .RemoveKey();
    }

    private LibraryItemModel ToLibraryItemModel(LibraryFile.ReadOnly libraryFile)
    {
        var linkedLoadoutItemsObservable = _connection
            .ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItemId, libraryFile.Id)
            .Transform(datom => LibraryLinkedLoadoutItem.Load(_connection.Db, datom.E))
            .RemoveKey();

        return new LibraryItemModel
        {
            LibraryItemId = libraryFile.AsLibraryItem().LibraryItemId,
            Name = libraryFile.AsLibraryItem().Name,
            CreatedAt = libraryFile.GetCreatedAt(),
            Size = libraryFile.Size,
            LinkedLoadoutItemsObservable = linkedLoadoutItemsObservable,
        };
    }

    public IObservable<IChangeSet<LibraryItemModel>> ObserveNestedLibraryItems()
    {
        // NOTE(erri120): For the nested library view, design wanted to have a
        // parent for the LocalFile, we create a parent with one child that will
        // both be the same.
        return _connection
            .ObserveDatoms(LocalFile.PrimaryAttribute)
            .Transform(datom =>
            {
                var libraryFile = LibraryFile.Load(_connection.Db, datom.E);

                var hasChildrenObservable = Observable.Return(true);
                var childrenObservable = UIObservableExtensions.ReturnFactory(() => new ChangeSet<LibraryItemModel>([new Change<LibraryItemModel>(ListChangeReason.Add, ToLibraryItemModel(libraryFile))]));

                var linkedLoadoutItemsObservable = _connection
                    .ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItemId, libraryFile.Id)
                    .Transform(d => LibraryLinkedLoadoutItem.Load(_connection.Db, d.E))
                    .RemoveKey();

                return new LibraryItemModel
                {
                    LibraryItemId = libraryFile.AsLibraryItem().LibraryItemId,
                    Name = libraryFile.AsLibraryItem().Name,
                    CreatedAt = libraryFile.GetCreatedAt(),
                    Size = libraryFile.Size,
                    HasChildrenObservable = hasChildrenObservable,
                    ChildrenObservable = childrenObservable,
                    LinkedLoadoutItemsObservable = linkedLoadoutItemsObservable,
                };
            })
            .RemoveKey();
    }

    public IObservable<IChangeSet<LoadoutItemModel>> ObserveNestedLoadoutItems()
    {
        // NOTE(erri120): For the nested loadout view, the parent will be a "fake" loadout model
        // created from a LocalFile where the children are the LibraryLinkedLoadoutItems that link
        // back to the LocalFile
        return _connection
            .ObserveDatoms(LocalFile.PrimaryAttribute)
            // TODO: observable filter
            .Filter(datom => _connection.Db.Datoms(LibraryLinkedLoadoutItem.LibraryItemId, datom.E).Count > 0)
            .Transform(datom =>
            {
                var libraryFile = LibraryFile.Load(_connection.Db, datom.E);

                var observable = _connection
                    .ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItemId, datom.E)
                    .Transform(d => LibraryLinkedLoadoutItem.Load(_connection.Db, d.E))
                    .ChangeKey(static entity => entity.Id)
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

                var childrenObservable = observable
                    .Transform(libraryLinkedLoadoutItem => LoadoutDataProviderHelper.ToLoadoutItemModel(_connection, libraryLinkedLoadoutItem))
                    .RemoveKey();

                var installedAtObservable = observable
                    .Transform(item => item.GetCreatedAt())
                    .RemoveKey()
                    .QueryWhenChanged(collection => collection.FirstOrDefault());

                var loadoutItemIdsObservable = observable
                    .Transform(item => item.AsLoadoutItemGroup().AsLoadoutItem().LoadoutItemId)
                    .RemoveKey();

                var isEnabledObservable = observable
                    .TransformOnObservable(item => LoadoutItem.Observe(_connection, item.Id))
                    .Transform(item => !item.IsDisabled)
                    .RemoveKey()
                    .QueryWhenChanged(collection => collection.All(x => x));

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
            })
            .RemoveKey();
    }
}
