using System.Collections.ObjectModel;
using NexusMods.App.UI.Pages.Sorting.Prototype;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.Sorting;

public interface ILoadOrdersPageViewModel : IPageViewModelInterface
{
    public ReadOnlyObservableCollection<ILoadOrderViewModel> LoadOrderViewModels { get; }
}
