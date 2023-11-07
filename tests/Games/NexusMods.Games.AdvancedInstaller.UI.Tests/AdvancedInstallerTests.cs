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
    private readonly AdvancedInstallerHandlerUI _installerHandlerUi;
    private readonly GameInstallation _gameInstallation;
    private readonly ModId _baseModId;
    private readonly FileTreeNode<RelativePath, ModSourceFileEntry> _archiveFiles;

    public AdvancedInstallerTests(IServiceProvider provider)
    {
        // Executed once per method
        _installerHandlerUi = new AdvancedInstallerHandlerUI( null!);
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
        return await _installerHandlerUi.GetModsAsync(_gameInstallation, LoadoutId.Null, _baseModId, _archiveFiles);
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
    }
}

// Mock Factories
public class MockOverlayVMFactory
{
    public static IUnsupportedModOverlayViewModel VM = null!;
    public static bool CreateWasCalled;

    public static IUnsupportedModOverlayViewModel Create(string modName = "")
    {
        CreateWasCalled = true;
        return VM;
    }

    public static void Reset()
    {
        CreateWasCalled = false;
        VM = new UnsupportedModOverlayViewModel("Test Mod");
    }
}

public class MockInstallerVMFactory
{
    public static IAdvancedInstallerOverlayViewModel? VM;
    public static bool CreateWasCalled;

    public static IAdvancedInstallerOverlayViewModel Create(FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles,
        GameLocationsRegister register, string gameName = "", string modName = "")
    {
        CreateWasCalled = true;
        VM = new AdvancedInstallerOverlayViewModel(modName, archiveFiles, register, gameName);
        return VM;
    }

    public static void Reset()
    {
        CreateWasCalled = false;
        VM = null!;
    }
}
