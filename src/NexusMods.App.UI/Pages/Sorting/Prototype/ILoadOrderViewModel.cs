using System.Collections.ObjectModel;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.UI;

namespace NexusMods.App.UI.Pages.Sorting.Prototype;

public interface ILoadOrderViewModel : IViewModelInterface
{
    string SortOrderName { get; }
    
    ReadOnlyObservableCollection<ISortableItemViewModel> SortableItems { get; }
    
    LoadOrderTreeDataGridAdapter Adapter { get; }
}
