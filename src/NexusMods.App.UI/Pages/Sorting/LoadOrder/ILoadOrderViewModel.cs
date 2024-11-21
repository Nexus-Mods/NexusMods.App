using System.ComponentModel;
using System.Reactive;
using NexusMods.Abstractions.UI;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Sorting;

public interface ILoadOrderViewModel : IViewModelInterface
{
    LoadOrderTreeDataGridAdapter Adapter { get; }
    
    string SortOrderName { get; }
    string InfoAlertTitle { get; }
    string InfoAlertHeading { get; }
    string InfoAlertMessage { get; }
    bool InfoAlertIsVisible { get; set; }
    ReactiveCommand<Unit, Unit> InfoAlertCommand { get; }
    string TrophyToolTip { get; }
    ListSortDirection SortDirectionCurrent { get; set; }
}
