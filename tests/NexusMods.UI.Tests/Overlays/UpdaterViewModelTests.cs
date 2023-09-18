using System.Runtime.InteropServices;
using FluentAssertions;
using NexusMods.App.UI.Overlays.Updater;
using NexusMods.Common;

namespace NexusMods.UI.Tests.Overlays;

public class UpdaterViewModelTests : AVmTest<UpdaterViewModel, IUpdaterViewModel>
{
    public UpdaterViewModelTests(IServiceProvider provider) : base(provider) { }

    [Fact]
    public async Task CanCheckForArchiveReleases()
    {
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

    [Fact]
    public async Task CanCheckForAppImageSetupReleases()
    {
        ConcreteVm.Method = InstallationMethod.AppImage;
        ConcreteVm.OldVersion = Version.Parse("0.0.0.0");
        (await ConcreteVm.ShouldShow()).Should().BeFalse();
        ConcreteVm.NewVersion.Should().BeGreaterThan(ConcreteVm.OldVersion);
        ConcreteVm.ChangelogUrl.Should().NotBeNull();
        ConcreteVm.UpdateUrl.Should().NotBeNull();

        ConcreteVm.UpdateUrl.ToString().Should().EndWith(".AppImage");
    }
}
