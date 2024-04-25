using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.App.UI.Resources;
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
        
        this.WhenActivated(d =>
        {
            _task.WhenAnyValue(t => t.PersistentState.FriendlyName)
                .OnUI()
                .Select(t => t)
                .BindTo(this, x => x.Name)
                .DisposeWith(d);
            
            _task.WhenAnyValue(t => t.PersistentState.Version)
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

        });
    }

    [Reactive] public string Name { get; set; } = "";

    [Reactive] public string Version { get; set; } = "";
    [Reactive] public string Game { get; set; } = "";
    
    public EntityId TaskId => _task.PersistentState.Id;

    [Reactive] public DownloadTaskStatus Status { get; set; } 

    [Reactive] public long DownloadedBytes { get; set; }

    [Reactive] public long SizeBytes { get; set; }

    [Reactive] public long Throughput { get; set; }

    public void Cancel() => _task.Cancel();
    public void Suspend() => _task.Suspend();
    public void Resume() => _task.Resume();
    
}
