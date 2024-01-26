using System.Runtime.InteropServices;
using FluentAssertions;
using NexusMods.App.BuildInfo;
using NexusMods.App.UI.Overlays.Updater;

namespace NexusMods.UI.Tests.Overlays;

public class UpdaterViewModelTests : AVmTest<UpdaterViewModel, IUpdaterViewModel>
{
    public UpdaterViewModelTests(IServiceProvider provider) : base(provider) { }

    [Fact]
    public async Task CanCheckForArchiveReleases()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            // Can't test this until we actually have a release
            return;

        ConcreteVm.Method = InstallationMethod.Archive;
        ConcreteVm.OldVersion = Version.Parse("0.0.0.0");
        (await ConcreteVm.ShouldShow()).Should().BeTrue();
        ConcreteVm.NewVersion.Should().BeGreaterThan(ConcreteVm.OldVersion);
        ConcreteVm.ChangelogUrl.Should().NotBeNull();
        ConcreteVm.UpdateUrl.Should().NotBeNull();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            ConcreteVm.UpdateUrl.ToString().Should().EndWith(".win-x64.zip");
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            ConcreteVm.UpdateUrl.ToString().Should().EndWith(".linux-x64.zip");
        else
            throw new NotImplementedException("Unsupported OS");
    }

    [Fact]
    public async Task CanCheckForInnoSetupReleases()
    {
        ConcreteVm.Method = InstallationMethod.InnoSetup;
        ConcreteVm.OldVersion = Version.Parse("0.0.0.0");
        (await ConcreteVm.ShouldShow()).Should().BeTrue();
        ConcreteVm.NewVersion.Should().BeGreaterThan(ConcreteVm.OldVersion);
        ConcreteVm.ChangelogUrl.Should().NotBeNull();
        ConcreteVm.UpdateUrl.Should().NotBeNull();

        ConcreteVm.UpdateUrl.ToString().Should().EndWith(".exe");
    }

    // App Images are broken so this fails for now
    /*
    [Fact]
    public async Task CanCheckForAppImageSetupReleases()
    {
        ConcreteVm.Method = InstallationMethod.AppImage;
        ConcreteVm.OldVersion = Version.Parse("0.0.0.0");
        (await ConcreteVm.ShouldShow()).Should().BeTrue();
        ConcreteVm.NewVersion.Should().BeGreaterThan(ConcreteVm.OldVersion);
        ConcreteVm.ChangelogUrl.Should().NotBeNull();
        ConcreteVm.UpdateUrl.Should().NotBeNull();

        ConcreteVm.UpdateUrl.ToString().Should().EndWith(".AppImage");
    }
    */

    [Fact]
    public async Task DontShowDialogWhenThereIsNoNewVersion()
    {
        ConcreteVm.Method = InstallationMethod.Archive;
        ConcreteVm.OldVersion = Version.Parse("9999.9.9.9");
        (await ConcreteVm.ShouldShow()).Should().BeFalse();
    }

    [Fact]
    public async Task ClickingTheLaterButtonClosesTheWindow()
    {
        ConcreteVm.IsActive = true;
        ConcreteVm.IsActive.Should().BeTrue();

        ConcreteVm.LaterCommand.Execute(null);
        ConcreteVm.IsActive.Should().BeFalse();
    }
}
