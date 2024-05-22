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
    private IReadOnlyActivity? _currentLoginActivity;
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        activityMonitor.Activities
            .ToObservableChangeSet(x => x.Id)
            .Filter(x => x.Group.Value == Constants.OAuthActivityGroupName)
            .OnUI()
            .SubscribeWithErrorLogging(changeSet =>
            {
                if (changeSet.Removes > 0 && _currentLoginActivity is not null)
                {
                    if (changeSet.Any(x => x.Reason == ChangeReason.Remove && x.Current == _currentLoginActivity))
                    {
                        _currentLoginActivity = null;
                        _overlayViewModel?.Close();
                    }
                }

                if (changeSet.Adds > 0)
                {
                    if (_currentLoginActivity is not null) return;

                    _currentLoginActivity = changeSet.First(x => x.Reason == ChangeReason.Add).Current;
                    _overlayViewModel = new NexusLoginOverlayViewModel(_currentLoginActivity);

                    overlayController.Enqueue(_overlayViewModel);
                }
            })
            .DisposeWith(_compositeDisposable);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _currentLoginActivity = null;
        _overlayViewModel?.Close();

        _compositeDisposable.Dispose();
        return Task.CompletedTask;
    }
}
