using DynamicData;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.MnemonicDB.Attributes.Extensions;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Query;
using Observable = System.Reactive.Linq.Observable;
using UIObservableExtensions = NexusMods.App.UI.Extensions.ObservableExtensions;

namespace NexusMods.App.UI.Pages;

[UsedImplicitly]
internal class LocalFileDataProvider : ILibraryDataProvider
{
    private readonly IConnection _connection;

    public LocalFileDataProvider(IServiceProvider serviceProvider)
    {
        _connection = serviceProvider.GetRequiredService<IConnection>();
    }

    public IObservable<IChangeSet<LibraryItemModel>> ObserveFlatLibraryItems()
    {
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
}
