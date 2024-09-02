using DynamicData;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.App.UI.Pages;

public interface ILibraryDataProvider
{
    IObservable<IChangeSet<LibraryItemModel, EntityId>> ObserveFlatLibraryItems();

    IObservable<IChangeSet<LibraryItemModel, EntityId>> ObserveNestedLibraryItems();
}
