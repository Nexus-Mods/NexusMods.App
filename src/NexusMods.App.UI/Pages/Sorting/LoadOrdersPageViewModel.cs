using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Pages.Sorting.Prototype;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.App.UI.Pages.Sorting;

public class LoadOrdersPageViewModel : APageViewModel<ILoadOrdersPageViewModel>, ILoadOrdersPageViewModel
{
    private readonly LoadoutId _loadoutId;
    private readonly IConnection _connection;

    public ReadOnlyObservableCollection<ILoadOrderViewModel> LoadOrderViewModels { get; }

    public LoadOrdersPageViewModel(IWindowManager windowManager, IServiceProvider serviceProvider, LoadoutId loadutId) : base(windowManager)
    {
        _loadoutId = loadutId;
        _connection = serviceProvider.GetRequiredService<IConnection>();

        var loadout = Loadout.Load(_connection.Db, _loadoutId);
        var sortableItemProviders = loadout.InstallationInstance.GetGame().SortableItemProviderFactories;
        
        LoadOrderViewModels = new ReadOnlyObservableCollection<ILoadOrderViewModel>(
            new ObservableCollection<ILoadOrderViewModel>(
                sortableItemProviders.Select(provider => new LoadOrderViewModel(_loadoutId, provider))
            )
        );
    }
}
