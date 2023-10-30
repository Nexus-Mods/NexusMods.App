using FluentAssertions;

namespace NexusMods.Games.AdvancedInstaller.UI.Tests.ViewModels;

public class UnsupportedModOverlayViewModelTests
{
    [Fact]
    public void After_Accepting_ShouldAdvancedInstall_ShouldBe_True()
    {
        // Arrange
        var viewModel = new UnsupportedModOverlayViewModel("Test Mod Name");

        // Act
        ((IUnsupportedModOverlayViewModel)viewModel).Accept();

        // Assert
        viewModel.ShouldAdvancedInstall.Should().BeTrue();
    }

    [Fact]
    public void After_Accepting_IsActive_ShouldBe_False()
    {
        // Arrange
        var viewModel = new UnsupportedModOverlayViewModel("Test Mod Name");

        // Act
        ((IUnsupportedModOverlayViewModel)viewModel).Accept();

        // Assert
        viewModel.IsActive.Should().BeFalse();
    }

    [Fact]
    public void After_Declining_ShouldAdvancedInstall_ShouldBe_False()
    {
        // Arrange
        var viewModel = new UnsupportedModOverlayViewModel("Test Mod Name");

        // Act
        ((IUnsupportedModOverlayViewModel)viewModel).Decline();

        // Assert
        viewModel.ShouldAdvancedInstall.Should().BeFalse();
    }

    [Fact]
    public void After_Declining_IsActive_ShouldBe_False()
    {
        // Arrange
        var viewModel = new UnsupportedModOverlayViewModel("Test Mod Name");

        // Act
        ((IUnsupportedModOverlayViewModel)viewModel).Decline();

        // Assert
        viewModel.IsActive.Should().BeFalse();
    }
}
