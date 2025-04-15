using System.Runtime.InteropServices;
using FluentAssertions;
using NexusMods.App.BuildInfo;
using NexusMods.App.UI.Overlays.Updater;
using NexusMods.Paths;

namespace NexusMods.UI.Tests;

public class UpdateCheckerTests
{
    private static readonly OSPlatform[] Platforms = [OSPlatform.Linux, OSPlatform.Windows];
    private static readonly InstallationMethod[] InstallationMethods = Enum.GetValues<InstallationMethod>();

    [Theory]
    [MemberData(nameof(TestData_Matches))]
    public void Test_Matches(string fileName, OSPlatform platform, InstallationMethod installationMethod)
    {
        // NOTE(erri120): tests all permutations and makes sure that the givenFile only matches the provided platform and method
        foreach (var otherPlatform in Platforms)
        {
            var os = new OSInformation(otherPlatform);
            foreach (var otherMethod in InstallationMethods)
            {
                var expected = otherMethod == installationMethod && otherPlatform.Equals(platform);
                var actual = UpdateChecker.Matches(fileName, os, otherMethod);
                actual.Should().Be(expected, because: $"fileName={fileName} platform={otherPlatform} method={otherMethod}");
            }
        }
    }

    public static TheoryData<string, OSPlatform, InstallationMethod> TestData_Matches()
    {
        return new TheoryData<string, OSPlatform, InstallationMethod>
        {
            { "App.linux-x64.zip", OSPlatform.Linux, InstallationMethod.Archive },
            { "App.win-x64.zip", OSPlatform.Windows, InstallationMethod.Archive },
            { "App.exe", OSPlatform.Windows, InstallationMethod.InnoSetup },
            { "App.AppImage", OSPlatform.Linux, InstallationMethod.AppImage},
        };
    }
}
