using System.Collections.ObjectModel;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.Sorting;

public interface ILoadOrdersWIPPageViewModel : IPageViewModelInterface
{
    public ISortingSelectionViewModel SortingSelectionViewModel { get; }
}
