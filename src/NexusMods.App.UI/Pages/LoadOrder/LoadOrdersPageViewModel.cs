using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Pages.LoadOrder.Prototype;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.LoadOrder;

public class LoadOrdersPageViewModel : APageViewModel<ILoadOrdersPageViewModel>, ILoadOrdersPageViewModel
{
    private readonly LoadoutId _loadoutId;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnection _connection;
    
    public ILoadOrderViewModel? LoadOrderViewModel { get; }
    
    public LoadOrdersPageViewModel(IWindowManager windowManager, IServiceProvider serviceProvider, LoadoutId loadutId) : base(windowManager)
    {
        _loadoutId = loadutId;
        _serviceProvider = serviceProvider;
        _connection = serviceProvider.GetRequiredService<IConnection>();
        var loadout = Loadout.Load(_connection.Db, _loadoutId);
        var sortableItemProviders = loadout.InstallationInstance.GetGame().SortableItemProviders;
        if (sortableItemProviders.Length > 0)
        {
            LoadOrderViewModel = new LoadOrderViewModel(serviceProvider, _loadoutId, sortableItemProviders[0]);
        }
    }


   
}
