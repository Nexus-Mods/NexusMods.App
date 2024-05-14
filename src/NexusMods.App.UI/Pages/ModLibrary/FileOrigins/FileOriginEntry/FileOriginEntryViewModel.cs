using System.Reactive;
using System.Reactive.Linq;
using Humanizer;
using NexusMods.Abstractions.FileStore.Downloads;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
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
    public ReactiveCommand<Unit, Unit> AddToLoadoutCommand { get; init; }

    [ObservableAsProperty] public string DisplayArchiveDate { get; } = "-";

    [ObservableAsProperty] public DateTime LastInstalledDate { get; set; }

    [ObservableAsProperty] public string DisplayLastInstalledDate { get; } = "-";
    
    public FileOriginEntryViewModel(
        IConnection conn,
        IArchiveInstaller archiveInstaller,
        LoadoutId loadoutId,
        DownloadAnalysis.Model fileOrigin)
    {
        Name = fileOrigin.SuggestedName;
        Size = fileOrigin.Size;
        AddToLoadoutCommand = ReactiveCommand.CreateFromTask(async () => { await archiveInstaller.AddMods(loadoutId, fileOrigin); }
        );
        Version = fileOrigin.TryGet(DownloaderState.Version, out var version) && version != "Unknown"
            ? version
            : "-";
        ArchiveDate = fileOrigin.CreatedAt;

        var loadout = conn.Db.Get<Loadout.Model>(loadoutId.Value);

        var interval = Observable.Interval(TimeSpan.FromSeconds(60)).StartWith(1);

        // Update the humanized Archive Date every minute
        interval.Select(_ => ArchiveDate)
            .Select(date => date.Equals(DateTime.MinValue) ? "-" : date.Humanize())
            .ToPropertyEx(this, vm => vm.DisplayArchiveDate);

        // Update the LastInstalledDate every time the loadout is updated
        conn.Revisions(loadoutId)
            .StartWith(loadout)
            .Select(rev => rev.Mods.Where(mod => mod.Contains(Mod.Source)
                                                 && mod.SourceId.Equals(fileOrigin.Id)
                )
                .Select(mod => mod.CreatedAt)
                .DefaultIfEmpty(DateTime.MinValue)
                .Max()
            )
            .ToPropertyEx(this, vm => vm.LastInstalledDate);

        // Update the humanized LastInstalledDate every minute and when the LastInstalledDate changes
        this.WhenAnyValue(vm => vm.LastInstalledDate)
            .Merge(interval.Select(_ => LastInstalledDate))
            .Select(date => date.Equals(DateTime.MinValue) ? "-" : date.Humanize())
            .ToPropertyEx(this, vm => vm.DisplayLastInstalledDate);
    }
}
