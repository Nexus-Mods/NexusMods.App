using System.Windows.Input;
using NexusMods.App.UI.Resources;
using NexusMods.DataModel.RateLimiting;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.Spine.Buttons.Download;

public class DownloadButtonViewModel : AViewModel<IDownloadButtonViewModel>, IDownloadButtonViewModel
{
    [Reactive]
    public float Number { get; set; } = 4.2f;

    [Reactive]
    public string Units { get; set; } = "MB/s";

    [Reactive]
    public Percent? Progress { get; set; }

    [Reactive]
    public ICommand Click { get; set; } = Initializers.ICommand;

    [Reactive]
    public bool IsActive { get; set; }

    [Reactive]
    public string IdleText { get; set; } = Language.DownloadStatus_Idle;

    [Reactive]
    public string ProgressText { get; set; } = Language.DownloadStatus_Progress;
}
