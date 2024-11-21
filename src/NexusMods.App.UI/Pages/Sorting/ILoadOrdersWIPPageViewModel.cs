using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.Sorting;

/// <summary>
/// Temporary page for hosting the WIP Load Order views
/// </summary>
public interface ILoadOrdersWIPPageViewModel : IPageViewModelInterface
{
    public ISortingSelectionViewModel SortingSelectionViewModel { get; }
}
