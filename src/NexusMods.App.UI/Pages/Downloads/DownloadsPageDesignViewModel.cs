using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using NexusMods.Abstractions.Downloads;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Paths;
using NexusMods.UI.Sdk.Icons;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Downloads;

public class DownloadsPageDesignViewModel : APageViewModel<IDownloadsPageViewModel>, IDownloadsPageViewModel
{
    public DownloadsTreeDataGridAdapter Adapter { get; }
    public int SelectionCount => 0;
    public Observable<bool> HasRunningItems { get; } = R3.Observable.Return(false);
    public Observable<bool> HasPausedItems { get; } = R3.Observable.Return(false);
    public Observable<bool> HasActiveItems { get; } = R3.Observable.Return(false);

    public bool IsEmptyStateActive { get; set; } = false;
    public string HeaderTitle { get; } = Language.DownloadsLeftMenu_AllDownloads;
    public string HeaderDescription { get; } = Language.DownloadsPage_AllDownloads_Description;

    public ReactiveCommand<Unit> PauseAllCommand { get; } = new R3.ReactiveCommand();
    public ReactiveCommand<Unit> ResumeAllCommand { get; } = new R3.ReactiveCommand();
    public ReactiveCommand<Unit> CancelSelectedCommand { get; } = new R3.ReactiveCommand();
    public ReactiveCommand<Unit> PauseSelectedCommand { get; } = new R3.ReactiveCommand();
    public ReactiveCommand<Unit> ResumeSelectedCommand { get; } = new R3.ReactiveCommand();

    public DownloadsPageDesignViewModel() : base(new DesignWindowManager())
    {
        Adapter = new DownloadsTreeDataGridAdapter(new DesignTimeDownloadsDataProvider(), new DownloadsFilter { Scope = DownloadsScope.All });
        this.WhenActivated(d => { Adapter.Activate().DisposeWith(d); });
        
        TabTitle = Language.Downloads_WorkspaceTitle;
        TabIcon = IconValues.PictogramDownload;
    }
}

/// <summary>
/// Design-time data provider that returns mock data for XAML designer.
/// </summary>
public class DesignTimeDownloadsDataProvider : IDownloadsDataProvider
{
    public IObservable<IChangeSet<CompositeItemModel<DownloadId>, DownloadId>> ObserveDownloads(DownloadsFilter filter)
    {
        // Create some dummy download items for design-time
        var dummyDownloads = CreateDummyDownloads();
        var source = new SourceCache<CompositeItemModel<DownloadId>, DownloadId>(item => item.Key);
        
        // Add dummy data to the source cache
        source.AddOrUpdate(dummyDownloads);
        
        return source.Connect();
    }

    public IObservable<int> CountDownloads(DownloadsFilter filter)
    {
        return ObserveDownloads(filter)
            .QueryWhenChanged(q => q.Count)
            .Prepend(0);
    }
    
    public string ResolveGameName(GameId gameId)
    {
        return "Design Game";
    }
    
    private static CompositeItemModel<DownloadId>[] CreateDummyDownloads()
    {
        var downloads = new CompositeItemModel<DownloadId>[3];
        
        // Dummy download 1 - In Progress
        var id1 = DownloadId.From(Guid.NewGuid());
        var model1 = new CompositeItemModel<DownloadId>(id1);
        
        // Add Name component
        model1.Add(DownloadColumns.Name.NameComponentKey, new NameComponent(
            initialValue: "Example Mod v1.2.3",
            valueObservable: R3.Observable.Return("Example Mod v1.2.3")));
        
        // Add Game component
        model1.Add(DownloadColumns.Game.ComponentKey, new DownloadComponents.GameComponent(
            gameName: "Skyrim Special Edition"));
        
        // Add Size component
        model1.Add(DownloadColumns.Size.ComponentKey, new DownloadComponents.SizeProgressComponent(
            initialDownloaded: Size.FromLong(1024 * 1024 * 50), // 50MB
            initialTotal: Size.FromLong(1024 * 1024 * 100), // 100MB
            downloadedObservable: R3.Observable.Return(Size.FromLong(1024 * 1024 * 50)),
            totalObservable: R3.Observable.Return(Size.FromLong(1024 * 1024 * 100))));
        
        // Add Speed component
        model1.Add(DownloadColumns.Speed.ComponentKey, new DownloadComponents.SpeedComponent(
            initialTransferRate: Size.FromLong(1024 * 1024 * 2), // 2MB/s
            transferRateObservable: R3.Observable.Return(Size.FromLong(1024 * 1024 * 2))));
        
        downloads[0] = model1;
        
        // Dummy download 2 - Completed
        var id2 = DownloadId.From(Guid.NewGuid());
        var model2 = new CompositeItemModel<DownloadId>(id2);
        
        model2.Add(DownloadColumns.Name.NameComponentKey, new NameComponent(
            initialValue: "Graphics Enhancement Pack",
            valueObservable: R3.Observable.Return("Graphics Enhancement Pack")));
        
        model2.Add(DownloadColumns.Game.ComponentKey, new DownloadComponents.GameComponent(
            gameName: "Cyberpunk 2077"));
        
        model2.Add(DownloadColumns.Size.ComponentKey, new DownloadComponents.SizeProgressComponent(
            initialDownloaded: Size.FromLong(1024 * 1024 * 250), // 250MB
            initialTotal: Size.FromLong(1024 * 1024 * 250), // 250MB
            downloadedObservable: R3.Observable.Return(Size.FromLong(1024 * 1024 * 250)),
            totalObservable: R3.Observable.Return(Size.FromLong(1024 * 1024 * 250))));
        
        model2.Add(DownloadColumns.Speed.ComponentKey, new DownloadComponents.SpeedComponent(
            initialTransferRate: Size.FromLong(0),
            transferRateObservable: R3.Observable.Return(Size.FromLong(0))));
        
        downloads[1] = model2;
        
        // Dummy download 3 - Paused
        var id3 = DownloadId.From(Guid.NewGuid());
        var model3 = new CompositeItemModel<DownloadId>(id3);
        
        model3.Add(DownloadColumns.Name.NameComponentKey, new NameComponent(
            initialValue: "Ultimate Texture Pack",
            valueObservable: R3.Observable.Return("Ultimate Texture Pack")));
        
        model3.Add(DownloadColumns.Game.ComponentKey, new DownloadComponents.GameComponent(
            gameName: "Fallout 4"));
        
        model3.Add(DownloadColumns.Size.ComponentKey, new DownloadComponents.SizeProgressComponent(
            initialDownloaded: Size.FromLong(1024 * 1024 * 75), // 75MB
            initialTotal: Size.FromLong(1024 * 1024 * 300), // 300MB
            downloadedObservable: R3.Observable.Return(Size.FromLong(1024 * 1024 * 75)),
            totalObservable: R3.Observable.Return(Size.FromLong(1024 * 1024 * 300))));
        
        model3.Add(DownloadColumns.Speed.ComponentKey, new DownloadComponents.SpeedComponent(
            initialTransferRate: Size.FromLong(0),
            transferRateObservable: R3.Observable.Return(Size.FromLong(0))));
        
        downloads[2] = model3;
        
        return downloads;
    }
}
