using System.Windows.Input;
using NexusMods.DataModel.RateLimiting;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Spine.Buttons.Download;

public class DownloadButtonDesignerViewModel : AViewModel<IDownloadButtonViewModel>, IDownloadButtonViewModel
{
    public float Number { get; set; } = 4.2f;
    public string Units { get; set; } = "MB/s";
    public Percent? Progress { get; set; }
    public ICommand Command { get; }

    public DownloadButtonDesignerViewModel()
    {
        Command = ReactiveCommand.CreateFromTask(async () => StartProgress());
    }

    private async Task StartProgress()
    {
        foreach (var i in Enumerable.Range(0, 100))
        {
            Progress = new Percent(i / 100d);
            Number = Random.Shared.NextSingle() * 10f;
            await Task.Delay(100);
        }
        Progress = null;
    }
}
