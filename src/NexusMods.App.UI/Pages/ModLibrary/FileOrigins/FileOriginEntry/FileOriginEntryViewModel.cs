using System.Reactive;
using System.Reactive.Linq;
using Humanizer;
using NexusMods.Paths;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.ModLibrary.FileOriginEntry;

public class FileOriginEntryViewModel : AViewModel<IFileOriginEntryViewModel>, IFileOriginEntryViewModel
{
    public required string Name { get; init; } 
    public string Version { get; init; } = "-";
    public required Size Size { get; init; }
    public DateTime ArchiveDate { get; set; } = DateTime.UnixEpoch;

    [ObservableAsProperty]
    public string DisplayArchiveDate { get; } = "-";

    
    [Reactive]
    public DateTime LastInstalledDate { get; set; } = DateTime.UnixEpoch;
    
    [ObservableAsProperty]
    public string DisplayLastInstalledDate { get; } = "-";
    public required ReactiveCommand<Unit, Unit> AddToLoadoutCommand { get; init; }
    
    public FileOriginEntryViewModel()
    {
        var interval = Observable.Interval(TimeSpan.FromSeconds(60)).StartWith(1);
        
        interval.Select(_ => ArchiveDate)
            .Select(date => date.Equals(DateTime.UnixEpoch) ? "-" : date.Humanize())
            .ToPropertyEx(this, vm => vm.DisplayArchiveDate);

        this.WhenAnyValue(vm => vm.LastInstalledDate)
            .Merge(interval.Select(_ => LastInstalledDate))
            .Select(date => date.Equals(DateTime.UnixEpoch) ? "-" : date.Humanize())
            .ToPropertyEx(this, vm => vm.DisplayLastInstalledDate);
    }
}
