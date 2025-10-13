using NexusMods.UI.Sdk;

namespace NexusMods.App.UI.Pages.Sorting;

public interface IFileConflictsViewModel : IViewModelInterface
{
    FileConflictsTreeDataGridAdapter TreeDataGridAdapter { get; }
}
