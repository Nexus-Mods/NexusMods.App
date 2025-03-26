using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.UI;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.App.UI.Pages.Sorting;

public class SortingSelectionViewModel : AViewModel<ISortingSelectionViewModel>, ISortingSelectionViewModel
{
    private readonly LoadoutId _loadoutId;
    private readonly IConnection _connection;
    public ReadOnlyObservableCollection<ILoadOrderViewModel> LoadOrderViewModels { get; }

    public SortingSelectionViewModel(IServiceProvider serviceProvider, LoadoutId loadutId)
    {
        _loadoutId = loadutId;
        _connection = serviceProvider.GetRequiredService<IConnection>();

        var loadout = Loadout.Load(_connection.Db, _loadoutId);
        var sortableItemProviders = loadout
            .InstallationInstance
            .GetGame()
            .SortableItemProviderFactories;

        LoadOrderViewModels = new ReadOnlyObservableCollection<ILoadOrderViewModel>(
            new ObservableCollection<ILoadOrderViewModel>(
                sortableItemProviders.Select(provider => new LoadOrderViewModel(_loadoutId, provider, serviceProvider))
            )
        );
    }
}
