using System.ComponentModel;
using System.Reactive;
using NexusMods.Abstractions.UI;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Sorting;

public class LoadOrderDesignViewModel : AViewModel<ILoadOrderViewModel>, ILoadOrderViewModel
{
    public LoadOrderTreeDataGridAdapter Adapter { get; set; }
    public string SortOrderName { get; set; } = "Sort Order Name";
    public string InfoAlertTitle { get; set;} = "Info Alert Title";
    public string InfoAlertHeading { get; set;} = "Info Alert Heading";
    public string InfoAlertMessage { get; set;} = "Info Alert Message";
    public bool InfoAlertIsVisible { get; set; } = true;
    public ReactiveCommand<Unit, Unit> InfoAlertCommand { get; } = ReactiveCommand.Create(() => { Console.WriteLine("InfoAlertCommand"); });
    public string TrophyToolTip { get; set;} = "Trophy Tool Tip";
    public ListSortDirection SortDirectionCurrent { get; set; }
    public bool IsWinnerTop { get; set;}
    
    public LoadOrderDesignViewModel()
    {
        Adapter = new LoadOrderTreeDataGridAdapter(new MockLoadoutSortableItemProvider());
    }
}
