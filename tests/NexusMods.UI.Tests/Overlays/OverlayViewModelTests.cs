using FluentAssertions;
using NexusMods.App.UI.Overlays;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.UI.Tests.Overlays;

public class OverlayViewModelTests
{
    [Fact]
    [Trait("FlakeyTest", "True")]

    public void SetOverlayContent_CanGetLastOverlayViewModel()
    {
        // Arrange
        var overlay = new MockOverlayViewModel();
        var controller = new OverlayController();
        
        // Act
        controller.Enqueue(overlay);
        
        // Assert that the last overlay is the one we just added
        controller.CurrentOverlay.Should().Be(overlay);
    }
    
    [Fact]
    [Trait("FlakeyTest", "True")]

    public void SetOverlayContent_WithTwoOverlays_CanGetLastOverlayViewModel()
    {
        // Arrange
        var overlay1 = new MockOverlayViewModel();
        var overlay2 = new MockOverlayViewModel();
        
        var controller = new OverlayController();

        // Act
        controller.Enqueue(overlay1);
        controller.Enqueue(overlay2);
        
        // Check if first overlay is loaded.
        controller.CurrentOverlay.Should().Be(overlay1);
        overlay1.Status.Should().Be(Status.Visible);
        overlay2.Status.Should().Be(Status.Hidden);
        
        overlay1.Close();
        
        // Assert next modal got loaded.
        controller.CurrentOverlay.Should().Be(overlay2);
        overlay2.Status.Should().Be(Status.Visible);
        
        // Assert last modal is closed
        overlay1.Status.Should().Be(Status.Closed);
    }
    
    [Fact]
    [Trait("FlakeyTest", "True")]

    public void SetOverlayContent_WithThreeOverlays_CanGetLastOverlayViewModel()
    {
        // Arrange
        var overlay1 = new MockOverlayViewModel();
        var overlay2 = new MockOverlayViewModel();
        var overlay3 = new MockOverlayViewModel();
        
        var controller = new OverlayController();

        // Act
        controller.Enqueue(overlay1);
        controller.Enqueue(overlay2);
        controller.Enqueue(overlay3);
        
        // Check if first overlay is loaded.
        controller.CurrentOverlay.Should().Be(overlay1);
        overlay1.Status.Should().Be(Status.Visible);
        overlay2.Status.Should().Be(Status.Hidden);
        overlay3.Status.Should().Be(Status.Hidden);
        controller.CurrentOverlay!.Close();
        
        // Assert next modal got loaded.
        controller.CurrentOverlay.Should().Be(overlay2);
        overlay1.Status.Should().Be(Status.Closed);
        overlay2.Status.Should().Be(Status.Visible);
        overlay3.Status.Should().Be(Status.Hidden);
        controller.CurrentOverlay!.Close();
        
        // Assert next modal got loaded.
        controller.CurrentOverlay.Should().Be(overlay3);
        overlay1.Status.Should().Be(Status.Closed);
        overlay2.Status.Should().Be(Status.Closed);
        overlay3.Status.Should().Be(Status.Visible);
    }
    
    [Fact]
    [Trait("FlakeyTest", "True")]
    public async Task SetOverlayViewModel_WhenActiveIsFalse_TaskIsCompleted()
    {
        // Arrange
        var overlay = new MockOverlayViewModel();
        var tcs = new TaskCompletionSource<bool>();
        
        var controller = new OverlayController();
        var updated = false;

        // Act
        using var sub = overlay.CompletionTask.ContinueWith(t =>
        {
            updated = true;
            tcs.SetResult(t.IsCompletedSuccessfully);
        });
        controller.Enqueue(overlay);
        overlay.Close();

        // Assert
        await tcs.Task;
        updated.Should().BeTrue();
        await sub;
    }
    
    public class MockOverlayViewModel : AOverlayViewModel<IOverlayViewModel>
    {
    }
}
