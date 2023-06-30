using FluentAssertions;
using NexusMods.App.UI;
using NexusMods.App.UI.Overlays;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.UI.Tests.Overlays;

public class OverlayViewModelTests
{
    [Fact]
    public void SetOverlayContent_CanGetLastOverlayViewModel()
    {
        // Arrange
        var overlay = new SetOverlayItem(new MockOverlayViewModel());
        var controller = new OverlayController();
        
        // Act
        controller.SetOverlayContent(overlay);
        
        // Assert that the last overlay is the one we just added
        controller.GetLastOverlay().Should().Be(overlay);
    }
    
    [Fact]
    public void SetOverlayContent_WithTwoOverlays_CanGetLastOverlayViewModel()
    {
        // Arrange
        var overlay = new SetOverlayItem(new MockOverlayViewModel(true));
        var overlay2 = new SetOverlayItem(new MockOverlayViewModel(true));
        
        var controller = new OverlayController();

        // Act
        controller.SetOverlayContent(overlay);
        controller.SetOverlayContent(overlay2);
        
        // Check if first overlay is loaded.
        controller.GetLastOverlay().Should().Be(overlay);
        overlay.vm.IsActive = false; // Unloads the overlay, causing queue item to be popped.
        
        // Assert next modal got loaded.
        controller.GetLastOverlay().Should().Be(overlay2);
        overlay2.vm.IsActive = false;
        
        // Assert last modal is null
        controller.GetLastOverlay().Should().BeNull();
    }
    
    [Fact]
    public void SetOverlayContent_WithThreeOverlays_CanGetLastOverlayViewModel()
    {
        // Arrange
        var overlay = new SetOverlayItem(new MockOverlayViewModel(true));
        var overlay2 = new SetOverlayItem(new MockOverlayViewModel(true));
        var overlay3 = new SetOverlayItem(new MockOverlayViewModel(true));
        
        var controller = new OverlayController();

        // Act
        controller.SetOverlayContent(overlay);
        controller.SetOverlayContent(overlay2);
        controller.SetOverlayContent(overlay3);
        
        // Check if first overlay is loaded.
        controller.GetLastOverlay().Should().Be(overlay);
        overlay.vm.IsActive = false; // Unloads the overlay, causing queue item to be popped.
        
        // Assert next modal got loaded.
        controller.GetLastOverlay().Should().Be(overlay2);
        overlay2.vm.IsActive = false;
        
        // Assert next modal got loaded.
        controller.GetLastOverlay().Should().Be(overlay3);
        overlay3.vm.IsActive = false;
    }
    
    [Fact]
    public void SetOverlayViewModel_ShouldUpdateViewModel()
    {
        // Arrange
        var overlay = new SetOverlayItem(new MockOverlayViewModel());

        var controller = new OverlayController();
        var updated = false;

        // Act
        using var sub = controller.ApplyNextOverlay.Subscribe(_ => { updated = true; });
        controller.SetOverlayContent(overlay);

        // Assert
        updated.Should().BeTrue();
    }
    
    [Fact]
    public void SetOverlayViewModel_WhenActiveIsFalse_TaskIsCompleted()
    {
        // Arrange
        var overlay = new SetOverlayItem(new MockOverlayViewModel(true));
        var tcs = new TaskCompletionSource<bool>();
        
        var controller = new OverlayController();
        var updated = false;

        // Act
        using var sub = controller.ApplyNextOverlay.Subscribe(_ => { updated = true; });
        controller.SetOverlayContent(overlay, tcs);
        overlay.vm.IsActive = false;

        // Assert
        updated.Should().BeTrue();
        tcs.Task.Result.Should().BeTrue();
    }
    
    public class MockOverlayViewModel : AViewModel<IOverlayViewModel>, IOverlayViewModel
    {
        [Reactive]
        public bool IsActive { get; set; }
        
        public MockOverlayViewModel() { }
        public MockOverlayViewModel(bool isActive)
        {
            IsActive = isActive;
        }
    }
}
