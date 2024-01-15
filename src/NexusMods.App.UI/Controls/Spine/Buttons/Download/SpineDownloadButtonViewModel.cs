using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData;
using NexusMods.Abstractions.Values;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Networking.Downloaders.Interfaces.Traits;
using NexusMods.Paths;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.Spine.Buttons.Download;

public class SpineDownloadButtonViewModel : AViewModel<ISpineDownloadButtonViewModel>, ISpineDownloadButtonViewModel
{
    internal const int PollTimeMilliseconds = 1000;

    internal IObservable<Unit> Tick { get; } = Observable.Interval(TimeSpan.FromMilliseconds(PollTimeMilliseconds))
        .Select(_ => Unit.Default);

    public SpineDownloadButtonViewModel(IDownloadService downloadService)
    {
        this.WhenActivated(disposables =>
        {
            Tick.Subscribe(_ =>
            {
                Number = downloadService.GetThroughput() / Size.MB;
                Units = "MB/s";
                Progress = downloadService.GetTotalProgress();

            }).DisposeWith(disposables);
        });
    }

    [Reactive] public double Number { get; set; } = 4.2f;

    [Reactive] public string Units { get; set; } = "MB/s";

    [Reactive] public Percent? Progress { get; set; }

    [Reactive] public ICommand Click { get; set; } = Initializers.ICommand;

    [Reactive] public bool IsActive { get; set; }
}
