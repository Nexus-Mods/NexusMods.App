using System.Windows.Input;
using NexusMods.DataModel.RateLimiting;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.Spine.Buttons.Download;

public class DownloadButtonDesignerViewModel : AViewModel<IDownloadButtonViewModel>, IDownloadButtonViewModel
{
    [Reactive]
    public float Number { get; set; } = 4.2f;
    
    [Reactive]
    public string Units { get; set; } = "MB/s";
    
    [Reactive]
    public Percent? Progress { get; set; }
    
    [Reactive]
    public ICommand Click { get; set; }

    [Reactive]
    public bool IsActive { get; set; }

    public DownloadButtonDesignerViewModel()
    {
        Click = ReactiveCommand.CreateFromTask(() => Task.FromResult(StartProgress()));
    }

    private async Task StartProgress()
    {
        if (Progress != null) return;
        foreach (var i in Enumerable.Range(0, 100))
        {
            Progress = new Percent(i / 100d);
            Number = Random.Shared.NextSingle() * 10f;
            await Task.Delay(100);
        }
        Progress = null;
    }
}
