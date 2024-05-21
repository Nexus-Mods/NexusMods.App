using System.Reactive.Disposables;
using DynamicData;
using DynamicData.Binding;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using NexusMods.Abstractions.Activities;
using NexusMods.Abstractions.NexusWebApi;

namespace NexusMods.App.UI.Overlays.Login;


/// <summary>
/// Very simple service that connects the activity monitor to the overlay controller. Looks for OAuth login activities and
/// displays an overlay for them. We have to do it this way as the overlay controller doesn't know about the login overlays,
/// and the activity monitory and login manager are way on the backend. So this is a bit of a UI/Backend bridge.
/// </summary>
[UsedImplicitly]
public class NexusLoginOverlayService(IActivityMonitor activityMonitor, IOverlayController overlayController) : IHostedService
{
    private readonly CompositeDisposable _compositeDisposable = new();

    private NexusLoginOverlayViewModel? _overlayViewModel;
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        activityMonitor.Activities
            .ToObservableChangeSet(x => x.Id)
            .Filter(x => x.Group.Value == Constants.OAuthActivityGroupName)
            .QueryWhenChanged()
            .OnUI()
            .Subscribe(job =>
            {
                if (job.Count == 0) 
                {
                    _overlayViewModel?.Close();
                    return;
                }
                
                _overlayViewModel = new NexusLoginOverlayViewModel(job.Items.First());
                overlayController.Enqueue(_overlayViewModel);
            })
            .DisposeWith(_compositeDisposable);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _compositeDisposable.Dispose();
        return Task.CompletedTask;
    }
}
