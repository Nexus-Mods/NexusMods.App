using NexusMods.Abstractions.UI;

namespace NexusMods.App.UI.Pages.Sorting.Prototype;

public interface ILoadOrderViewModel : IViewModelInterface
{
    string SortOrderName { get; }
    
    LoadOrderTreeDataGridAdapter Adapter { get; }
}
