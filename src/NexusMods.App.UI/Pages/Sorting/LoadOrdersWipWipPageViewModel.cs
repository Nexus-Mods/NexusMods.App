using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.App.UI.Pages.Sorting;

public class LoadOrdersWipWipPageViewModel : APageViewModel<ILoadOrdersWIPPageViewModel>, ILoadOrdersWIPPageViewModel
{
    private readonly LoadoutId _loadoutId;

    public ISortingSelectionViewModel SortingSelectionViewModel { get; }


    public LoadOrdersWipWipPageViewModel(IWindowManager windowManager, IServiceProvider serviceProvider, LoadoutId loadutId) : base(windowManager)
    {
        _loadoutId = loadutId;

        SortingSelectionViewModel = new SortingSelectionViewModel(serviceProvider, _loadoutId);
    }

}
