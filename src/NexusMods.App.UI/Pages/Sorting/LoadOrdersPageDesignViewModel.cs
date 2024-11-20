using System.Collections.ObjectModel;
using NexusMods.App.UI.Pages.Sorting.Prototype;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.Sorting;

public class LoadOrdersPageDesignViewModel : APageViewModel<ILoadOrdersPageViewModel>, ILoadOrdersPageViewModel
{
    public LoadOrdersPageDesignViewModel() : base(new DesignWindowManager())
    {
    }

    public ReadOnlyObservableCollection<ILoadOrderViewModel> LoadOrderViewModels { get; } = new([]);
}
