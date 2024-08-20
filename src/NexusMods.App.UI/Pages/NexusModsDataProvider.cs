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
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Query;

namespace NexusMods.App.UI.Pages;

[UsedImplicitly]
internal class NexusModsDataProvider : ILibraryDataProvider
{
    private readonly IConnection _connection;

    public NexusModsDataProvider(IServiceProvider serviceProvider)
    {
        _connection = serviceProvider.GetRequiredService<IConnection>();
    }

    public IObservable<IChangeSet<LibraryItemModel>> ObserveFlatLibraryItems()
    {
        return NexusModsLibraryFile
            .ObserveAll(_connection)
            .Transform(ToLibraryItemModel)
            .RemoveKey();
    }

    public IObservable<IChangeSet<LibraryItemModel>> ObserveNestedLibraryItems()
    {
        return NexusModsModPageMetadata
            .ObserveAll(_connection)
            .Filter(modPage => _connection.Db.Datoms(NexusModsFileMetadata.ModPageId, modPage.Id).Count > 0)
            .Transform(ToLibraryItemModel)
            .RemoveKey();
    }

    private LibraryItemModel ToLibraryItemModel(NexusModsLibraryFile.ReadOnly nexusModsLibraryFile)
    {
        var linkedLoadoutItemsObservable = _connection
            .ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItemId, nexusModsLibraryFile.Id)
            .Transform(datom => LibraryLinkedLoadoutItem.Load(_connection.Db, datom.E))
            .RemoveKey();

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
            .ObserveDatoms(NexusModsFileMetadata.ModPageId, modPageMetadata.Id)
            .MergeManyChangeSets(datom => _connection.ObserveDatoms(NexusModsLibraryFile.FileMetadataId, datom.E))
            .RemoveKey()
            .PublishWithFunc(initialValueFunc: () =>
            {
                var changeSet = new ChangeSet<Datom>();
                var list = new List<Datom>();

                var fileDatoms = _connection.Db.Datoms(NexusModsFileMetadata.ModPageId, modPageMetadata.Id);
                foreach (var entityIdDatom in fileDatoms)
                {
                    var libraryFileDatom = _connection.Db.Datoms(NexusModsLibraryFile.FileMetadataId, entityIdDatom.E);
                    foreach (var datom in libraryFileDatom)
                    {
                        list.Add(datom);
                    }
                }

                changeSet.Add(new Change<Datom>(ListChangeReason.AddRange, list));
                return changeSet;
            })
            .AutoConnect();

        var hasChildrenObservable = nexusModsLibraryFileObservable.IsNotEmpty();
        var childrenObservable = nexusModsLibraryFileObservable.Transform(libraryFileDatom =>
        {
            var libraryFile = NexusModsLibraryFile.Load(_connection.Db, libraryFileDatom.E);
            return ToLibraryItemModel(libraryFile);
        });

        var linkedLoadoutItemsObservable = nexusModsLibraryFileObservable
            .MergeManyChangeSets(libraryFileDatom => _connection.ObserveDatoms(LibraryLinkedLoadoutItem.LibraryItemId, libraryFileDatom.E))
            .Transform(datom => LibraryLinkedLoadoutItem.Load(_connection.Db, datom.E))
            .RemoveKey();

        var libraryFilesObservable = nexusModsLibraryFileObservable.Transform(datom => NexusModsLibraryFile.Load(_connection.Db, datom.E));

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
}
