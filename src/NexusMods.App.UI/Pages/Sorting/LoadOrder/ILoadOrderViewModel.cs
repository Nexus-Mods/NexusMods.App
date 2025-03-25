using System.ComponentModel;
using System.Reactive;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Controls.Alerts;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Sorting;

public interface ILoadOrderViewModel : IViewModelInterface
{
    /// <summary>
    /// TreeDataGridAdapter for the Load Order, for setting up the TreeDataGrid
    /// </summary>
    TreeDataGridAdapter<CompositeItemModel<Guid>, Guid> Adapter { get; }
    
    /// <summary>
    /// Name of this sort order type
    /// </summary>
    string SortOrderName { get; }
    
    /// <summary>
    /// Heading displayed above data grid. Commonly used to describe the sort order behavior i.e. "First Loaded Wins"
    /// </summary>
    string SortOrderHeading { get; }
    
    /// <summary>
    /// The title of the alert message, only visible if the alert is visible
    /// </summary>
    string InfoAlertTitle { get; }
    
    /// <summary>
    /// The contents of the alert message, only visible if the alert is visible
    /// </summary>
    string InfoAlertBody { get; }
    
    /// <summary>
    /// Command to invoke when the info alert icon is pressed (either to show or hide the alert)
    /// </summary>
    ReactiveCommand<Unit, Unit> InfoAlertCommand { get; }
    
    /// <summary>
    /// Tooltip message contents for the trophy icon
    /// </summary>
    string TrophyToolTip { get; }
    
    /// <summary>
    /// The current ascending/descending direction in which the SortIndexes are sorted and displayed
    /// </summary>
    ListSortDirection SortDirectionCurrent { get; set; }
    
    /// <summary>
    /// Switch the current sort direction to the inverse
    /// </summary>
    ReactiveCommand<Unit, Unit> SwitchSortDirectionCommand { get; }
    
    /// <summary>
    /// True if current sort direction is ascending, false if descending
    /// </summary>
    bool IsAscending { get; }
    
    /// <summary>
    /// Whether the winning item is at the top or bottom of the list
    /// </summary>
    /// <remarks>
    /// Depends on the way the game load order works and can't be deduced exclusively from the sort direction
    /// </remarks>
    bool IsWinnerTop { get; }
    
    /// <summary>
    /// Title text for the empty state, in case there are no sortable items to display
    /// </summary>
    string EmptyStateMessageTitle { get; }
    
    /// <summary>
    /// Contents text for the empty state, in case there are no sortable items to display
    /// </summary>
    string EmptyStateMessageContents { get; }
    
    /// <summary>
    /// AlertSettings wrapper for the Alert so it can be shown\hidden based on the settings
    /// </summary>
    AlertSettingsWrapper AlertSettingsWrapper { get; }
}
