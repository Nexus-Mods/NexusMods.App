using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Abstractions.Activities;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Paths;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.Spine.Buttons.Download;

[UsedImplicitly]
public class SpineDownloadButtonViewModel : AViewModel<ISpineDownloadButtonViewModel>, ISpineDownloadButtonViewModel
{
    private const int PollTimeMilliseconds = 1000;

    private IObservable<Unit> Tick { get; } = Observable.Defer(() =>
        Observable.Interval(TimeSpan.FromMilliseconds(PollTimeMilliseconds))
            .Select(_ => Unit.Default));

    public SpineDownloadButtonViewModel(IDownloadService downloadService)
    {
        this.WhenActivated(disposables =>
        {
            Tick.OnUI().Subscribe(_ =>
            {
                Number = downloadService.GetThroughput() / Size.MB;
                Units = "MB/s";
                Progress = downloadService.GetTotalProgress();
            }).DisposeWith(disposables);
        });
    }

    [Reactive] public double Number { get; set; }

    [Reactive] public string Units { get; set; } = "MB/s";

    [Reactive] public Optional<Percent> Progress { get; set; }

    [Reactive] public ICommand Click { get; set; } = Initializers.ICommand;

    [Reactive] public bool IsActive { get; set; }
}
