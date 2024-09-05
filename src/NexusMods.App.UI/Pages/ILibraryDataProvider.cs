using DynamicData;
using DynamicData.Kernel;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.MnemonicDB.Abstractions;
using R3;

namespace NexusMods.App.UI.Pages;

public interface ILibraryDataProvider
{
    IObservable<IChangeSet<LibraryItemModel, EntityId>> ObserveFlatLibraryItems(IObservable<LibraryFilter> filterObservable);

    IObservable<IChangeSet<LibraryItemModel, EntityId>> ObserveNestedLibraryItems();
}

public record LibraryFilter
{
    public Optional<LoadoutId> Loadout { get; init; }
}
