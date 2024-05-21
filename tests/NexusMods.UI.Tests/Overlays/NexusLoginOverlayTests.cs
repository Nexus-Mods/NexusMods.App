using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using FluentAssertions;
using NexusMods.Abstractions.Activities;
using NexusMods.Activities;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.Overlays.Login;
using NexusMods.Networking.NexusWebApi.Auth;

namespace NexusMods.UI.Tests.Overlays;

public class NexusLoginOverlayTests
{

    [Fact]
    public async Task LoginTasksCreateOverlays()
    {
        var overlayController = new OverlayController();
        var activityMonitor = new ActivityMonitor();
        var nexusLoginService = new NexusLoginOverlayService(activityMonitor, overlayController);

        await nexusLoginService.StartAsync(CancellationToken.None);
        
        var url = new Uri("http://foo/bar");

        var listOfJobs = new List<IChangeSet<IReadOnlyActivity>>();
        using var subbed = activityMonitor.Activities.ToObservableChangeSet()
            .Subscribe(change => listOfJobs.Add(change));
            
        
        
        var job = activityMonitor.CreateWithPayload(OAuth.Group, url, "Logging into Nexus Mods, redirecting to {Url}", url);
        activityMonitor.Activities.Should().Contain((IReadOnlyActivity)job);
        overlayController.CurrentOverlay.Should().BeOfType<NexusLoginOverlayViewModel>();
        
    }
}
