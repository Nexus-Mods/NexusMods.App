using System.Reactive.Disposables;
using NexusMods.App.UI.Resources;
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
        this.WhenActivated(d =>
        {
            _task.WhenAnyValue(t => t.PersistentState.FriendlyName)
                .BindTo(this, x => x.Name)
                .DisposeWith(d);
            
            _task.WhenAnyValue(t => t.PersistentState.Version)
                .BindTo(this, x => x.Version)
                .DisposeWith(d);

            _task.WhenAnyValue(t => t.PersistentState.Status)
                .BindTo(this, x => x.Status)
                .DisposeWith(d);

        });
    }

    [Reactive] public string Name { get; set; } = "";

    [Reactive] public string Version { get; set; } = "";
    [Reactive] public string Game { get; set; } = "";

    [Reactive] public DownloadTaskStatus Status { get; set; } 

    [Reactive] public long DownloadedBytes { get; set; }

    [Reactive] public long SizeBytes { get; set; }

    [Reactive] public long Throughput { get; set; }

    public void Cancel() => _task.Cancel();
    public void Suspend() => _task.Suspend();
    public void Resume() => _task.Resume();
    
}
