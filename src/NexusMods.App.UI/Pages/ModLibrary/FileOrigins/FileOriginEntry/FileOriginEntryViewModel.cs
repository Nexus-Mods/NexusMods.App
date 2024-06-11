using System.Reactive;
using System.Reactive.Linq;
using Humanizer;
using NexusMods.Abstractions.FileStore.Downloads;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.Downloaders.Tasks.State;
using NexusMods.Paths;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.ModLibrary.FileOriginEntry;

public class FileOriginEntryViewModel : AViewModel<IFileOriginEntryViewModel>, IFileOriginEntryViewModel
{
    public string Name { get; }
    public string Version { get; }
    public Size Size { get; }
    public DateTime ArchiveDate { get; }

    private readonly ObservableAsPropertyHelper<bool> _isModAddedToLoadout;
    public bool IsModAddedToLoadout => _isModAddedToLoadout.Value;

    private readonly ObservableAsPropertyHelper<string> _displayArchiveDate;
    public string DisplayArchiveDate => _displayArchiveDate.Value;

    private readonly ObservableAsPropertyHelper<DateTime> _lastInstalledDate;
    public DateTime LastInstalledDate => _lastInstalledDate.Value;

    private readonly ObservableAsPropertyHelper<string> _displayLastInstalledDate;
    public string DisplayLastInstalledDate => _displayLastInstalledDate.Value;
    public DownloadAnalysis.Model FileOrigin { get; }
    public ReactiveCommand<NavigationInformation, Unit> ViewModCommand { get; }
    public ReactiveCommand<Unit, Unit> AddToLoadoutCommand { get; }
    public ReactiveCommand<Unit, Unit> AddAdvancedToLoadoutCommand { get; }

    public FileOriginEntryViewModel(
        IConnection conn,
        LoadoutId loadoutId,
        DownloadAnalysis.Model fileOrigin,
        ReactiveCommand<NavigationInformation, Unit> viewModCommand,
        ReactiveCommand<Unit, Unit> addModToLoadoutCommand,
        ReactiveCommand<Unit, Unit> addAdvancedToLoadoutCommand)
    {
        FileOrigin = fileOrigin;
        ViewModCommand = viewModCommand;
        AddToLoadoutCommand = addModToLoadoutCommand;
        AddAdvancedToLoadoutCommand = addAdvancedToLoadoutCommand;
        Name = fileOrigin.TryGet(DownloaderState.FriendlyName, out var friendlyName) && friendlyName != "Unknown"
            ? friendlyName
            : fileOrigin.SuggestedName;
        
        Size = fileOrigin.TryGet(DownloadAnalysis.Size, out var analysisSize) 
            ? analysisSize 
            : fileOrigin.TryGet(DownloaderState.Size, out var dlStateSize) 
                ? dlStateSize 
                : Size.Zero;
        
        Version = fileOrigin.TryGet(DownloaderState.Version, out var version) && version != "Unknown"
            ? version
            : "-";
        
        ArchiveDate = fileOrigin.GetCreatedAt();
        
        var loadout = conn.Db.Get<Loadout.Model>(loadoutId.Value);

        _isModAddedToLoadout = conn.Revisions(loadoutId)
            .StartWith(loadout)
            .Select(rev => rev.Mods.Any(mod => mod.Contains(Mod.Source) && mod.SourceId.Equals(fileOrigin.Id)))
            .ToProperty(this, vm => vm.IsModAddedToLoadout, scheduler: RxApp.MainThreadScheduler);

        var interval = Observable.Interval(TimeSpan.FromSeconds(60)).StartWith(1);

        // Update the humanized Archive Date every minute
        _displayArchiveDate = interval.Select(_ => ArchiveDate)
            .Select(date => date.Equals(DateTime.MinValue) ? "-" : date.Humanize())
            .ToProperty(this, vm => vm.DisplayArchiveDate, scheduler: RxApp.MainThreadScheduler);

        // Update the LastInstalledDate every time the loadout is updated
        _lastInstalledDate = conn.Revisions(loadoutId)
            .StartWith(loadout)
            .Select(rev => rev.Mods.Where(mod => mod.Contains(Mod.Source)
                                                 && mod.SourceId.Equals(fileOrigin.Id))
                .Select(mod => mod.GetCreatedAt())
                .DefaultIfEmpty(DateTime.MinValue)
                .Max()
            )
            .ToProperty(this, vm => vm.LastInstalledDate, scheduler: RxApp.MainThreadScheduler);

        // Update the humanized LastInstalledDate every minute and when the LastInstalledDate changes
        _displayLastInstalledDate = this.WhenAnyValue(vm => vm.LastInstalledDate)
            .Merge(interval.Select(_ => LastInstalledDate))
            .Select(date => date.Equals(DateTime.MinValue) ? "-" : date.Humanize())
            .ToProperty(this, vm => vm.DisplayLastInstalledDate, scheduler: RxApp.MainThreadScheduler);
    }
}
