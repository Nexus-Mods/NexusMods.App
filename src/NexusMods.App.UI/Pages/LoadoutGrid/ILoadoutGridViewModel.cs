using System.Collections.ObjectModel;
using System.Reactive;
using DynamicData;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls.DataGrid;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.LoadoutGrid;

public interface ILoadoutGridViewModel : IPageViewModelInterface
{
    LoadoutId LoadoutId { get; set; }

    ReadOnlyObservableCollection<LoadoutItemGroupId> GroupIds { get; }
    ReadOnlyObservableCollection<IDataGridColumnFactory<LoadoutColumn>> Columns { get; }
    SourceList<LoadoutItemGroupId> SelectedGroupIds { get; }

    ReactiveCommand<NavigationInformation, Unit> ViewLibraryCommand { get; }
    ReactiveCommand<NavigationInformation, Unit> ViewFilesCommand { get; }
    ReactiveCommand<Unit, Unit> DeleteCommand { get; }

    string? EmptyStateTitle { get; }
}
