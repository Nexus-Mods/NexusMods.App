using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Aggregation;
using Humanizer;
using NexusMods.Abstractions.FileStore.Downloads;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.Downloaders.Tasks.State;
using NexusMods.Paths;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.ModLibrary.FileOriginEntry;

public class FileOriginEntryViewModel : AViewModel<IFileOriginEntryViewModel>, IFileOriginEntryViewModel
{
    public string Name { get; } 
    public string Version { get; }
    public Size Size { get; }
    public DateTime ArchiveDate { get; }
    
    [ObservableAsProperty]
    public string DisplayArchiveDate { get; } = "-";

    
    [ObservableAsProperty]
    public DateTime LastInstalledDate { get; set; } 
    
    [ObservableAsProperty]
    public string DisplayLastInstalledDate { get; } = "-";
    public ReactiveCommand<Unit, Unit> AddToLoadoutCommand { get; init; }
    
    public FileOriginEntryViewModel(
        IConnection conn, 
        IArchiveInstaller archiveInstaller, 
        LoadoutId loadoutId, 
        DownloadAnalysis.Model fileOrigin)
    {
        Name = fileOrigin.SuggestedName;
        Size = fileOrigin.Size;
        AddToLoadoutCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await archiveInstaller.AddMods(loadoutId, fileOrigin);
            }
        );
        Version = fileOrigin.TryGet(DownloaderState.Version, out var version) && version != "Unknown"
            ? version
            : "-";
        ArchiveDate = fileOrigin.CreatedAt;
        
        var interval = Observable.Interval(TimeSpan.FromSeconds(60)).StartWith(1);
        
        interval.Select(_ => ArchiveDate)
            .Select(date => date.Equals(DateTime.UnixEpoch) ? "-" : date.Humanize())
            .ToPropertyEx(this, vm => vm.DisplayArchiveDate);
        
        conn.Revisions(loadoutId)
            .StartWith(conn.Db.Get<Loadout.Model>(loadoutId.Value))
            .ToObservableChangeSet()
            .TransformMany(revision => revision.Mods)
            .Filter(mod =>  fileOrigin.Id.Equals(mod.SourceId))
            .Maximum(mod => mod.CreatedAt)
            .ToPropertyEx(this, vm => vm.LastInstalledDate);

        this.WhenAnyValue(vm => vm.LastInstalledDate)
            .Merge(interval.Select(_ => LastInstalledDate))
            .Select(date => date.Equals(DateTime.UnixEpoch) ? "-" : date.Humanize())
            .ToPropertyEx(this, vm => vm.DisplayLastInstalledDate);
        
    }
}
