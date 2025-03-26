using System.Runtime.InteropServices;
using FluentAssertions;
using NexusMods.App.BuildInfo;
using NexusMods.App.UI.Overlays.Updater;
using NexusMods.Paths;

namespace NexusMods.UI.Tests;

public class UpdateCheckerTests
{
    [Theory]
    [MemberData(nameof(TestData_Matches))]
    public void Test_Matches(string fileName, OSPlatform platform, InstallationMethod installationMethod, bool expected)
    {
        var os = new OSInformation(platform);

        var actual = UpdateChecker.Matches(fileName, os, installationMethod);
        actual.Should().Be(expected);
    }

    public static TheoryData<string, OSPlatform, InstallationMethod, bool> TestData_Matches()
    {
        return new TheoryData<string, OSPlatform, InstallationMethod, bool>
        {
            { "App.linux-x64.zip", OSPlatform.Linux, InstallationMethod.Archive, true },
            { "App.win-x64.zip", OSPlatform.Windows, InstallationMethod.Archive, true },
            { "App.exe", OSPlatform.Windows, InstallationMethod.InnoSetup, true },
            { "App.AppImage", OSPlatform.Linux, InstallationMethod.AppImage, true },
        };
    }
}
