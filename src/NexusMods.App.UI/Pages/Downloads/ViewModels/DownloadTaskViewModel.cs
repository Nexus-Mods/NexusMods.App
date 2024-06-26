using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Humanizer;
using Humanizer.Bytes;
using NexusMods.Abstractions.MnemonicDB.Attributes.Extensions;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Networking.Downloaders.Tasks.State;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.Downloads.ViewModels;

public class DownloadTaskViewModel : AViewModel<IDownloadTaskViewModel>, IDownloadTaskViewModel
{
    private readonly IDownloadTask _task;

    public DownloadTaskViewModel(IDownloadTask task)
    {
        _task = task;

        var isCompleted = task.PersistentState.TryGetAsCompletedDownloadState(out var completed);

        IsHidden = task.PersistentState.Status.Equals(DownloadTaskStatus.Completed)
                   && isCompleted && completed.Hidden;
        
        var interval = Observable.Interval(TimeSpan.FromSeconds(60)).StartWith(1);
        
        this.WhenActivated(d =>
        {
            _task.WhenAnyValue(t => t.PersistentState.FriendlyName)
                .OnUI()
                .Select(t => t)
                .BindTo(this, x => x.Name)
                .DisposeWith(d);
            
            _task.WhenAnyValue(t => t.PersistentState)
                .Select(state => DownloaderState.Version.TryGet(state, out var version) ? version : "-" )
                .OnUI()
                .BindTo(this, x => x.Version)
                .DisposeWith(d);

            _task.WhenAnyValue(t => t.PersistentState.Status)
                .OnUI()
                .BindTo(this, x => x.Status)
                .DisposeWith(d);

            _task.WhenAnyValue(t => t.Downloaded)
                .OnUI()
                .Select(s => s.Value)
                .BindTo(this, x => x.DownloadedBytes)
                .DisposeWith(d);

            _task.WhenAnyValue(t => t.PersistentState.Size)
                .OnUI()
                .Select(s => s.Value)
                .BindTo(this, x => x.SizeBytes)
                .DisposeWith(d);
            
            _task.WhenAnyValue(t => t.PersistentState.GameDomain)
                .OnUI()
                .Select(g => g.ToString())
                .BindTo(this, x => x.Game)
                .DisposeWith(d);

            _task.WhenAnyValue(t => t.Bandwidth)
                .OnUI()
                .Select(b => b.Value)
                .BindTo(this, x => x.Throughput)
                .DisposeWith(d);
            
            _task.WhenAnyValue(t => t.PersistentState.Status)
                .Where(s => s.Equals(DownloadTaskStatus.Completed))
                .Select(_ =>
                {
                	var dlIsCompleted = _task.PersistentState.TryGetAsCompletedDownloadState(out var completedDl);

                	return _task.PersistentState.Status.Equals(DownloadTaskStatus.Completed) && dlIsCompleted
                        ? completedDl.CompletedDateTime
                        : DateTime.MinValue;
                })
                .OnUI()
                .BindTo(this, x => x.CompletedTime)
                .DisposeWith(d);
            
            this.WhenAnyValue(vm => vm.CompletedTime)
                .CombineLatest(interval)
                .Select(tuple => tuple.First.Equals(DateTime.MinValue) ? "-" : tuple.First.Humanize())
                .OnUI()
                .BindTo(this, x => x.HumanizedCompletedTime)
                .DisposeWith(d);

        });
    }

    public IDownloadTask DlTask => _task;
    [Reactive] public string Name { get; set; } = "";

    [Reactive] public string Version { get; set; } = "";
    [Reactive] public string Game { get; set; } = "";

    public string HumanizedSize => ByteSize.FromBytes(SizeBytes).ToString();
    
    [Reactive] public DateTime CompletedTime { get; set; }
    
    [Reactive] public string HumanizedCompletedTime { get; set; } = "-";

    public EntityId TaskId => _task.PersistentState.Id;

    [Reactive] public DownloadTaskStatus Status { get; set; } 

    [Reactive] public long DownloadedBytes { get; set; }

    [Reactive] public long SizeBytes { get; set; }

    [Reactive] public long Throughput { get; set; }

    [Reactive] public bool IsHidden { get; set; }

    public ReactiveCommand<Unit, Unit> HideCommand { get; set; } = ReactiveCommand.Create(() => { });
    
    public ReactiveCommand<NavigationInformation, Unit> ViewInLibraryCommand { get; set; } = 
        ReactiveCommand.Create<NavigationInformation>(_ => { });
    
    public Task Cancel() => _task.Cancel();
    public Task Suspend() => _task.Suspend();
    public Task Resume() => _task.Resume();
    
}
