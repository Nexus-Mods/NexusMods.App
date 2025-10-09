using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Kernel;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.HttpDownloads;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Sdk.Jobs;
using NexusMods.UI.Sdk;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.Spine.Buttons.Download;

[UsedImplicitly]
public class SpineDownloadButtonViewModel : AViewModel<ISpineDownloadButtonViewModel>, ISpineDownloadButtonViewModel
{
    public SpineDownloadButtonViewModel(IJobMonitor jobMonitor, ILogger<SpineDownloadButtonViewModel> logger)
    {
        this.WhenActivated(disposables =>
        {

            jobMonitor.ObserveActiveJobs<IHttpDownloadJob>()
                .AverageProgressPercent()
                .OnUI()
                .Subscribe(rate => { Progress = rate.Value > 0 ? rate : Optional<Percent>.None; })
                .DisposeWith(disposables);
            
            jobMonitor.ObserveActiveJobs<IHttpDownloadJob>()
                .SumRateOfProgress()
                .OnUI()
                // Convert from bytes to MB/s
                // TODO: use Humanizer
                .Subscribe(rate => Number = rate / 1024 / 1024)
                .DisposeWith(disposables);
            
        });
    }

    [Reactive] public double Number { get; set; }

    [Reactive] public string Units { get; set; } = "MB/s";

    [Reactive] public Optional<Percent> Progress { get; set; }

    [Reactive] public ReactiveCommand<Unit,Unit> Click { get; set; } = Initializers.EmptyReactiveCommand;
    
    public IWorkspaceContext? WorkspaceContext { get; set; }

    [Reactive] public bool IsActive { get; set; }

    [Reactive] public string ToolTip { get; set; } = Language.SpineDownloadButton_ToolTip;
}
