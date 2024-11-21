using System.Collections.ObjectModel;
using NexusMods.Abstractions.UI;

namespace NexusMods.App.UI.Pages.Sorting;

public interface ISortingSelectionViewModel : IViewModelInterface
{
    public ReadOnlyObservableCollection<ILoadOrderViewModel> LoadOrderViewModels { get; }
}
