using FluentAssertions;
using NexusMods.App.UI.Overlays;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;
using NexusMods.StandardGameLocators.TestHelpers.StubbedGames;

namespace NexusMods.Games.AdvancedInstaller.UI.Tests;

// note(s56) We use static types, so in case we write multiple tests, we want to avoid overwriting state.
// Static types are used so we can do things in zero overhead fashion at runtime.
[Collection("NonParallel")]
public class AdvancedInstallerTests
{
    private readonly AdvancedInstaller<MockOverlayVMFactory, MockInstallerVMFactory> _installer;
    private readonly GameInstallation _gameInstallation;
    private readonly ModId _baseModId;
    private readonly FileTreeNode<RelativePath, ModSourceFileEntry> _archiveFiles;

    public AdvancedInstallerTests(IServiceProvider provider)
    {
        // Executed once per method
        _installer = new AdvancedInstaller<MockOverlayVMFactory, MockInstallerVMFactory>(new OverlayController());
        _gameInstallation = new GameInstallation()
        {
            LocationsRegister = new GameLocationsRegister(new Dictionary<LocationId, AbsolutePath>()),
            Game = new StubbedGame(default!, default!, FileSystem.Shared, provider),
        };
        _baseModId = ModId.From(Guid.NewGuid());
        _archiveFiles = new FileTreeNode<RelativePath, ModSourceFileEntry>("", "", true, null);

        MockOverlayVMFactory.Reset();
        MockInstallerVMFactory.Reset();
    }

    private async ValueTask<IEnumerable<ModInstallerResult>> ExecuteInstaller()
    {
        return await _installer.GetModsAsync(_gameInstallation, _baseModId, _archiveFiles);
    }

    [Fact]
    public async Task GetModsAsync_Should_CallAdvancedInstaller_WhenDialogAccepted()
    {
        // Act
        var awaitable = ExecuteInstaller();
        while (!MockOverlayVMFactory.VM.IsActive) { } // Wait until new dialog is set.

        MockOverlayVMFactory.VM.Accept(); // Accept running advanced installer.

        while (MockInstallerVMFactory.VM == null) { } // Wait until VM created.

        while (!MockInstallerVMFactory.VM.IsActive) { } // Wait until new dialog is created.

        MockInstallerVMFactory.VM.IsActive = false; // Signal advanced installer is complete.
        var result = await awaitable;

        // Assert
        MockOverlayVMFactory.CreateWasCalled.Should().BeTrue();
        MockInstallerVMFactory.CreateWasCalled.Should().BeTrue();
        result.First().Id.Should().Be(_baseModId);
    }

    [Fact]
    public async Task GetModsAsync_Should_NotCallAdvancedInstaller_WhenDialogDeclined()
    {
        // Act
        var awaitable = ExecuteInstaller();
        while (!MockOverlayVMFactory.VM.IsActive) { } // Wait until new dialog is set.

        MockOverlayVMFactory.VM.Decline(); // Decline running advanced installer.
        var result = await awaitable;

        // Assert
        MockOverlayVMFactory.CreateWasCalled.Should().BeTrue();
        MockInstallerVMFactory.VM.Should().BeNull();
        MockInstallerVMFactory.CreateWasCalled.Should().BeFalse();
        result.First().Id.Should().Be(_baseModId);
    }
}

// Mock Factories
public class MockOverlayVMFactory : IUnsupportedModOverlayViewModelFactory
{
    public static IUnsupportedModOverlayViewModel VM = null!;
    public static bool CreateWasCalled;

    public static IUnsupportedModOverlayViewModel Create()
    {
        CreateWasCalled = true;
        return VM;
    }

    public static void Reset()
    {
        CreateWasCalled = false;
        VM = new UnsupportedModOverlayViewModel();
    }
}

public class MockInstallerVMFactory : IAdvancedInstallerOverlayViewModelFactory
{
    public static IAdvancedInstallerOverlayViewModel? VM;
    public static bool CreateWasCalled;

    public static IAdvancedInstallerOverlayViewModel Create(FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles,
        GameLocationsRegister register, string gameName = "")
    {
        CreateWasCalled = true;
        VM = new AdvancedInstallerOverlayViewModel(archiveFiles, register, gameName);
        return VM;
    }

    public static void Reset()
    {
        CreateWasCalled = false;
        VM = null!;
    }
}
