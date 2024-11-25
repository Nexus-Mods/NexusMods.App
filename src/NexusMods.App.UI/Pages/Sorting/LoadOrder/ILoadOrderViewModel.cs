using NexusMods.Abstractions.UI;

namespace NexusMods.App.UI.Pages.Sorting;

public interface ILoadOrderViewModel : IViewModelInterface
{
    string SortOrderName { get; }
    
    LoadOrderTreeDataGridAdapter Adapter { get; }
}
