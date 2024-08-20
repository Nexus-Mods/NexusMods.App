using DynamicData;
using NexusMods.App.UI.Pages.LibraryPage;

namespace NexusMods.App.UI.Pages;

public interface ILibraryDataProvider
{
    IObservable<IChangeSet<LibraryItemModel>> ObserveFlatLibraryItems();

    IObservable<IChangeSet<LibraryItemModel>> ObserveNestedLibraryItems();
}
