using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.UI;
using NexusMods.MnemonicDB.Abstractions;
using BindingFlags = System.Reflection.BindingFlags;

namespace NexusMods.App.UI.Pages.Sorting;

public class SortingSelectionViewModel : AViewModel<ISortingSelectionViewModel>, ISortingSelectionViewModel
{
    private readonly LoadoutId _loadoutId;
    private readonly IConnection _connection;
    public ReadOnlyObservableCollection<ILoadOrderViewModel> LoadOrderViewModels { get; }

    public SortingSelectionViewModel(IServiceProvider serviceProvider, LoadoutId loadoutId)
    {
        _loadoutId = loadoutId;
        _connection = serviceProvider.GetRequiredService<IConnection>();

        var loadout = Loadout.Load(_connection.Db, _loadoutId);
        var sortableItemProviders = loadout
            .InstallationInstance
            .GetGame()
            .SortableItemProviderFactories;

        var enumerable = sortableItemProviders.Select(ILoadOrderViewModel (providerFactory) => new LoadOrderViewModel(serviceProvider, providerFactory, providerFactory.GetLoadoutSortableItemProvider(loadout)));
        LoadOrderViewModels = new ReadOnlyObservableCollection<ILoadOrderViewModel>(new ObservableCollection<ILoadOrderViewModel>(enumerable));
    }
}
