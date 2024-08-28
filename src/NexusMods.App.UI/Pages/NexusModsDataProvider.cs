using System.Reactive.Linq;
using DynamicData;
using DynamicData.Aggregation;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.MnemonicDB.Attributes.Extensions;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.App.UI.Pages.LoadoutPage;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Query;

namespace NexusMods.App.UI.Pages;

[UsedImplicitly]
internal class NexusModsDataProvider : ILibraryDataProvider, ILoadoutDataProvider
{
    private readonly IConnection _connection;

    public NexusModsDataProvider(IServiceProvider serviceProvider)
    {
        _connection = serviceProvider.GetRequiredService<IConnection>();
    }

    public IObservable<IChangeSet<LibraryItemModel, EntityId>> ObserveFlatLibraryItems()
    {
        // NOTE(erri120): For the flat library view, we display each NexusModsLibraryFile
        return NexusModsLibraryFile
            .ObserveAll(_connection)
            .Transform(ToLibraryItemModel);
    }

    public IObservable<IChangeSet<LibraryItemModel, EntityId>> ObserveNestedLibraryItems()
    {
        // NOTE(erri120): For the nested library view, the parents are "fake" library
        // models that represent the Nexus Mods mod page, with each child being a
        // NexusModsLibraryFile that links to the mod page.
        return NexusModsModPageMetadata
            .ObserveAll(_connection)
            .FilterOnObservable((_, e) => _connection
                .ObserveDatoms(NexusModsLibraryFile.ModPageMetadataId, e)
                .IsNotEmpty()
            )
            .Transform(ToLibraryItemModel);
    }

    private LibraryItemModel ToLibraryItemModel(NexusModsLibraryFile.ReadOnly nexusModsLibraryFile)
    {
        var linkedLoadoutItemsObservable = _connection
            .ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItemId, nexusModsLibraryFile.Id)
            .AsEntityIds()
            .Transform((_, e) => LibraryLinkedLoadoutItem.Load(_connection.Db, e));

        return new LibraryItemModel
        {
            LibraryItemId = nexusModsLibraryFile.AsDownloadedFile().AsLibraryFile().AsLibraryItem().LibraryItemId,
            CreatedAt = nexusModsLibraryFile.GetCreatedAt(),
            Name = nexusModsLibraryFile.FileMetadata.Name,
            Size = nexusModsLibraryFile.AsDownloadedFile().AsLibraryFile().Size,
            Version = nexusModsLibraryFile.FileMetadata.Version,
            LinkedLoadoutItemsObservable = linkedLoadoutItemsObservable,
        };
    }

    private LibraryItemModel ToLibraryItemModel(NexusModsModPageMetadata.ReadOnly modPageMetadata)
    {
        var nexusModsLibraryFileObservable = _connection
            .ObserveDatoms(NexusModsLibraryFile.ModPageMetadataId, modPageMetadata.Id)
            .AsEntityIds()
            .PublishWithFunc(initialValueFunc: () =>
            {
                var changeSet = new ChangeSet<Datom, EntityId>();
                var datoms = _connection.Db.Datoms(NexusModsLibraryFile.ModPageMetadataId, modPageMetadata.Id);
                foreach (var datom in datoms)
                {
                    changeSet.Add(new Change<Datom, EntityId>(ChangeReason.Add, datom.E, datom));
                }

                return changeSet;
            })
            .AutoConnect();

        var hasChildrenObservable = nexusModsLibraryFileObservable.IsNotEmpty();
        var childrenObservable = nexusModsLibraryFileObservable.Transform((_, e) =>
        {
            var libraryFile = NexusModsLibraryFile.Load(_connection.Db, e);
            return ToLibraryItemModel(libraryFile);
        });

        var linkedLoadoutItemsObservable = nexusModsLibraryFileObservable
            .MergeManyChangeSets((_, e) => _connection.ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItemId, e).AsEntityIds())
            .Transform((_, e) => LibraryLinkedLoadoutItem.Load(_connection.Db, e));

        var libraryFilesObservable = nexusModsLibraryFileObservable.Transform((_, e) => NexusModsLibraryFile.Load(_connection.Db, e));

        return new NexusModsModPageItemModel
        {
            Name = modPageMetadata.Name,
            CreatedAt = modPageMetadata.GetCreatedAt(),
            HasChildrenObservable = hasChildrenObservable,
            ChildrenObservable = childrenObservable,
            LinkedLoadoutItemsObservable = linkedLoadoutItemsObservable,
            LibraryFilesObservable = libraryFilesObservable,
        };
    }

    public IObservable<IChangeSet<LoadoutItemModel, EntityId>> ObserveNestedLoadoutItems()
    {
        // NOTE(erri120): For the nested loadout view, we create "fake" models for
        // the mod pages as parents. Each child will be a LibraryLinkedLoadoutItem
        // that links to a NexusModsLibraryFile that links to the mod page.
        return NexusModsModPageMetadata
            .ObserveAll(_connection)
            .FilterOnObservable((_, modPageEntityId) => _connection
                .ObserveDatoms(NexusModsLibraryFile.ModPageMetadataId, modPageEntityId).AsEntityIds()
                .MergeManyChangeSets((_, libraryFileEntityId) => _connection.ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItemId, libraryFileEntityId).AsEntityIds())
                .IsNotEmpty()
            )
            .Transform(modPage =>
            {
                var observable = _connection
                    .ObserveDatoms(NexusModsLibraryFile.ModPageMetadataId, modPage.Id)
                    .MergeManyChangeSets(libraryFileDatom => _connection.ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItemId, libraryFileDatom.E))
                    .AsEntityIds()
                    .PublishWithFunc(() =>
                    {
                        var changeSet = new ChangeSet<Datom, EntityId>();

                        var libraryFileDatoms = _connection.Db.Datoms(NexusModsLibraryFile.ModPageMetadataId, modPage.Id);
                        foreach (var entityIdDatom in libraryFileDatoms)
                        {
                            var libraryLinkedLoadoutItemDatoms = _connection.Db.Datoms(LibraryLinkedLoadoutItem.LibraryItemId, entityIdDatom.E);
                            foreach (var datom in libraryLinkedLoadoutItemDatoms)
                            {
                                changeSet.Add(new Change<Datom, EntityId>(ChangeReason.Add, datom.E, datom));
                            }
                        }

                        return changeSet;
                    })
                    .AutoConnect();

                var hasChildrenObservable = observable.IsNotEmpty();
                var childrenObservable = observable.Transform(libraryLinkedLoadoutItemDatom =>
                {
                    var libraryLinkedLoadoutItem = LibraryLinkedLoadoutItem.Load(_connection.Db, libraryLinkedLoadoutItemDatom.E);
                    return LoadoutDataProviderHelper.ToLoadoutItemModel(_connection, libraryLinkedLoadoutItem);
                });

                var installedAtObservable = observable
                    .Transform(datom => LibraryLinkedLoadoutItem.Load(_connection.Db, datom.E).GetCreatedAt())
                    .RemoveKey()
                    .QueryWhenChanged(collection => collection.FirstOrDefault());

                var loadoutItemIdsObservable = observable.Transform(datom => (LoadoutItemId) datom.E).RemoveKey();

                var isEnabledObservable = observable
                    .TransformOnObservable(d => LoadoutItem.Observe(_connection, d.E))
                    .Transform(item => !item.IsDisabled)
                    .RemoveKey()
                    .QueryWhenChanged(collection => collection.All(x => x));

                LoadoutItemModel model = new FakeParentLoadoutItemModel
                {
                    NameObservable = Observable.Return(modPage.Name),
                    InstalledAtObservable = installedAtObservable,
                    LoadoutItemIdsObservable = loadoutItemIdsObservable,
                    IsEnabledObservable = isEnabledObservable,

                    HasChildrenObservable = hasChildrenObservable,
                    ChildrenObservable = childrenObservable,
                };

                return model;
            });
    }
}
