using System.ComponentModel;
using NexusMods.UI.Sdk;
using R3;

namespace NexusMods.App.UI.Pages.Sorting;

public interface IFileConflictsViewModel : IViewModelInterface
{
    R3.ReactiveCommand SwitchSortDirectionCommand { get; }
    
    FileConflictsTreeDataGridAdapter TreeDataGridAdapter { get; }

    IReadOnlyBindableReactiveProperty<ListSortDirection> SortDirectionCurrent { get; }

    IReadOnlyBindableReactiveProperty<bool> IsAscending { get; }
}
