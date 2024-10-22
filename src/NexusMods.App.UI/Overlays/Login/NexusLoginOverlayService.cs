using System.Reactive.Disposables;
using DynamicData;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.NexusWebApi;

namespace NexusMods.App.UI.Overlays.Login;

[UsedImplicitly]
public class NexusLoginOverlayService : IHostedService
{
    private readonly IOverlayController _overlayController;
    private readonly IJobMonitor _jobMonitor;
    private readonly CompositeDisposable _compositeDisposable;

    private NexusLoginOverlayViewModel? _overlayViewModel;
    private IJob? _currentJob;

    public NexusLoginOverlayService(IOverlayController overlayController, IJobMonitor jobMonitor)
    {
        _overlayController = overlayController;
        _jobMonitor = jobMonitor;
        _compositeDisposable = new CompositeDisposable();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _jobMonitor
            .ObserveActiveJobs<IOAuthJob>()
            .OnUI()
            .SubscribeWithErrorLogging(changeSet =>
            {
                if (changeSet.Removes > 0 && _currentJob is not null)
                {
                    if (changeSet.Any(x => x.Reason == ChangeReason.Remove && x.Current == _currentJob))
                    {
                        _currentJob = null;
                        _overlayViewModel?.Close();
                    }
                }

                if (changeSet.Adds > 0)
                {
                    if (_currentJob is not null) return;

                    _currentJob = changeSet.First(x => x.Reason == ChangeReason.Add).Current;
                    _overlayViewModel = new NexusLoginOverlayViewModel(_currentJob);

                    _overlayController.Enqueue(_overlayViewModel);
                }
            })
            .DisposeWith(_compositeDisposable);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _currentJob = null;
        _overlayViewModel?.Close();

        _compositeDisposable.Dispose();
        return Task.CompletedTask;
    }
}
