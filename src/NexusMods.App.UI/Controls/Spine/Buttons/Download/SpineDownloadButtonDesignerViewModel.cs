using System.Reactive;
using DynamicData.Kernel;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Sdk.Jobs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.Spine.Buttons.Download;

public class SpineDownloadButtonDesignerViewModel : AViewModel<ISpineDownloadButtonViewModel>, ISpineDownloadButtonViewModel
{
    [Reactive]
    public double Number { get; set; } = 4.2f;

    [Reactive]
    public string Units { get; set; } = "MB/s";

    [Reactive]
    public Optional<Percent> Progress { get; set; }

    [Reactive]
    public ReactiveCommand<Unit,Unit> Click { get; set; }

    public IWorkspaceContext? WorkspaceContext { get; set; }

    [Reactive]
    public bool IsActive { get; set; }

    public string ToolTip { get; set; } = Language.SpineDownloadButton_ToolTip;

    public SpineDownloadButtonDesignerViewModel()
    {
        Click = ReactiveCommand.CreateFromTask(StartProgress);
    }

    private async Task StartProgress()
    {
        if (Progress.HasValue) return;
        foreach (var i in Enumerable.Range(0, 100))
        {
            Progress = new Percent(i / 100d);
            Number = Random.Shared.NextSingle() * 10f;
            await Task.Delay(100);
        }
        Progress = Optional<Percent>.None;
    }
}
