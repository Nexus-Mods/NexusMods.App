using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData;
using NexusMods.Abstractions.Values;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Networking.Downloaders.Interfaces.Traits;
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
            downloadService.Downloads
                .Bind(out var downloads)
                .Subscribe()
                .DisposeWith(disposables);

            Tick.Subscribe(_ =>
            {
                long totalDownloadedBytes = 0;
                long totalSizeBytes = 0;
                long totalThroughput = 0;

                foreach (var dl in downloads.Where(dl => dl.Status == DownloadTaskStatus.Downloading))
                {
                    totalThroughput += dl.CalculateThroughput();
                    // Only compute percent for downloads that have a known size
                    if (dl is not IHaveFileSize size) continue;

                    totalSizeBytes += size.SizeBytes;
                    totalDownloadedBytes += dl.DownloadedSizeBytes;
                }

                Number = totalThroughput / (1024.0 * 1024.0);
                Units = "MB/s";

                if (totalSizeBytes == 0 || totalSizeBytes <= totalDownloadedBytes)
                {
                    Progress = null;
                    return;
                }

                Progress = new Percent(totalDownloadedBytes / (double)totalSizeBytes);
            }).DisposeWith(disposables);
        });
    }

    [Reactive] public double Number { get; set; } = 4.2f;

    [Reactive] public string Units { get; set; } = "MB/s";

    [Reactive] public Percent? Progress { get; set; }

    [Reactive] public ICommand Click { get; set; } = Initializers.ICommand;

    [Reactive] public bool IsActive { get; set; }
}
