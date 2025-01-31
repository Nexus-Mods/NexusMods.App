using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Humanizer;
using Humanizer.Bytes;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.Downloaders.Interfaces;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.Downloads.ViewModels;

public class DownloadTaskViewModel : AViewModel<IDownloadTaskViewModel>, IDownloadTaskViewModel
{
    private readonly IDownloadTask _task;

    public DownloadTaskViewModel(IDownloadTask task)
    {
        _task = task;

        var interval = Observable.Interval(TimeSpan.FromSeconds(60)).StartWith(1);
        
        this.WhenActivated(d =>
        {

            _task.WhenAnyValue(t => t.Downloaded)
                .Select(s => s.Value)
                .OnUI()
                .BindTo(this, x => x.DownloadedBytes)
                .DisposeWith(d);


            _task.WhenAnyValue(t => t.Bandwidth)
                .Select(b => b.Value)
                .OnUI()
                .BindTo(this, x => x.Throughput)
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

    public EntityId TaskId => EntityId.From(0);

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
