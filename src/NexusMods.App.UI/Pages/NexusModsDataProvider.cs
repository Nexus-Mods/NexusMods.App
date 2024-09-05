using System.Reactive.Linq;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Kernel;
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

    public IObservable<IChangeSet<LibraryItemModel, EntityId>> ObserveFlatLibraryItems(LibraryFilter libraryFilter)
    {
        // NOTE(erri120): For the flat library view, we display each NexusModsLibraryFile
        return NexusModsLibraryFile
            .ObserveAll(_connection)
            .Transform((file, _) => ToLibraryItemModel(file, libraryFilter));
    }

    public IObservable<IChangeSet<LibraryItemModel, EntityId>> ObserveNestedLibraryItems(LibraryFilter libraryFilter)
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
            .Transform((modPage, _) => ToLibraryItemModel(modPage, libraryFilter));
    }

    private LibraryItemModel ToLibraryItemModel(NexusModsLibraryFile.ReadOnly nexusModsLibraryFile, LibraryFilter libraryFilter)
    {
        var linkedLoadoutItemsObservable = QueryHelper.GetLinkedLoadoutItems(_connection, nexusModsLibraryFile.Id, libraryFilter);

        var model = new LibraryItemModel(nexusModsLibraryFile.Id)
        {
            CreatedAt = nexusModsLibraryFile.GetCreatedAt(),
            Name = nexusModsLibraryFile.FileMetadata.Name,
            LinkedLoadoutItemsObservable = linkedLoadoutItemsObservable,
        };

        model.ItemSize.Value = nexusModsLibraryFile.AsDownloadedFile().AsLibraryFile().Size.ToString();
        model.Version.Value = nexusModsLibraryFile.FileMetadata.Version;

        return model;
    }

    private LibraryItemModel ToLibraryItemModel(NexusModsModPageMetadata.ReadOnly modPageMetadata, LibraryFilter libraryFilter)
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
            return ToLibraryItemModel(libraryFile, libraryFilter);
        });

        var linkedLoadoutItemsObservable = nexusModsLibraryFileObservable
            // NOTE(erri120): DynamicData 9.0.4 is broken for value types because it uses ReferenceEquals. Temporary workaround is a custom equality comparer.
            .MergeManyChangeSets((_, e) => _connection.ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItemId, e).AsEntityIds(), equalityComparer: DatomEntityIdEqualityComparer.Instance)
            .FilterInObservableLoadout(_connection, libraryFilter)
            .Transform((_, e) => LibraryLinkedLoadoutItem.Load(_connection.Db, e));

        var libraryFilesObservable = nexusModsLibraryFileObservable
            .Transform((_, e) => NexusModsLibraryFile.Load(_connection.Db, e).AsDownloadedFile().AsLibraryFile().AsLibraryItem());

        var numInstalledObservable = nexusModsLibraryFileObservable.TransformOnObservable((_, e) => _connection
            .ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItemId, e)
            .AsEntityIds()
            .FilterInObservableLoadout(_connection, libraryFilter)
            .QueryWhenChanged(query => query.Count > 0)
            .Prepend(false)
        ).QueryWhenChanged(static query => query.Items.Count(static b => b));

        return new NexusModsModPageLibraryItemModel
        {
            Name = modPageMetadata.Name,
            CreatedAt = modPageMetadata.GetCreatedAt(),
            HasChildrenObservable = hasChildrenObservable,
            ChildrenObservable = childrenObservable,
            LinkedLoadoutItemsObservable = linkedLoadoutItemsObservable,
            NumInstalledObservable = numInstalledObservable,
            LibraryItemsObservable = libraryFilesObservable,
        };
    }

    public IObservable<IChangeSet<LoadoutItemModel, EntityId>> ObserveNestedLoadoutItems(LoadoutFilter loadoutFilter)
    {
        // NOTE(erri120): For the nested loadout view, we create "fake" models for
        // the mod pages as parents. Each child will be a LibraryLinkedLoadoutItem
        // that links to a NexusModsLibraryFile that links to the mod page.
        return NexusModsModPageMetadata
            .ObserveAll(_connection)
            .FilterOnObservable((_, modPageEntityId) => _connection
                .ObserveDatoms(NexusModsLibraryFile.ModPageMetadataId, modPageEntityId)
                .FilterOnObservable((d, _) => _connection
                    .ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItemId, d.E)
                    .AsEntityIds()
                    .FilterInStaticLoadout(_connection, loadoutFilter)
                    .IsNotEmpty())
                .IsNotEmpty()
            )
            .Transform(modPage =>
            {
                var observable = _connection
                    .ObserveDatoms(NexusModsLibraryFile.ModPageMetadataId, modPage.Id).AsEntityIds()
                    .FilterOnObservable((_, e) => _connection.ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItemId, e).IsNotEmpty())
                    // NOTE(erri120): DynamicData 9.0.4 is broken for value types because it uses ReferenceEquals. Temporary workaround is a custom equality comparer.
                    .MergeManyChangeSets((_, e) => _connection.ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItemId, e).AsEntityIds(), equalityComparer: DatomEntityIdEqualityComparer.Instance)
                    .FilterInStaticLoadout(_connection, loadoutFilter)
                    .PublishWithFunc(() =>
                    {
                        var changeSet = new ChangeSet<Datom, EntityId>();

                        var libraryFileDatoms = _connection.Db.Datoms(NexusModsLibraryFile.ModPageMetadataId, modPage.Id);
                        foreach (var entityIdDatom in libraryFileDatoms)
                        {
                            var libraryLinkedLoadoutItemDatoms = _connection.Db.Datoms(LibraryLinkedLoadoutItem.LibraryItemId, entityIdDatom.E);
                            foreach (var datom in libraryLinkedLoadoutItemDatoms)
                            {
                                if (!LoadoutItem.Load(_connection.Db, datom.E).LoadoutId.Equals(loadoutFilter.LoadoutId)) continue;
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
                    .Transform((_, e) => LibraryLinkedLoadoutItem.Load(_connection.Db, e).GetCreatedAt())
                    .QueryWhenChanged(query => query.Items.FirstOrDefault());

                var loadoutItemIdsObservable = observable.Transform((_, e) => (LoadoutItemId) e);

                var isEnabledObservable = observable
                    .TransformOnObservable(datom => LoadoutItem.Observe(_connection, datom.E).Select(item => !item.IsDisabled))
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
