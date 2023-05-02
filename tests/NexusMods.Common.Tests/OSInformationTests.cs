using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using FluentAssertions;

namespace NexusMods.Common.Tests;

public class OSInformationTests
{
    [Theory]
    [MemberData(nameof(PlatformsMemberData))]
    public void Test_IsWindows(OSPlatform platform)
    {
        IOSInformation info = new OSInformation(platform);
        if (platform == OSPlatform.Windows)
            info.IsWindows.Should().BeTrue();
        else
            info.IsWindows.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(PlatformsMemberData))]
    public void Test_IsLinux(OSPlatform platform)
    {
        IOSInformation info = new OSInformation(platform);
        if (platform == OSPlatform.Linux)
            info.IsLinux.Should().BeTrue();
        else
            info.IsLinux.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(PlatformsMemberData))]
    public void Test_IsOSX(OSPlatform platform)
    {
        IOSInformation info = new OSInformation(platform);
        if (platform == OSPlatform.OSX)
            info.IsOSX.Should().BeTrue();
        else
            info.IsOSX.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(PlatformsMemberData))]
    public void Test_MatchPlatform(OSPlatform platform)
    {
        IOSInformation info = new OSInformation(platform);

        var res = info.MatchPlatform(
            () => OSPlatform.Windows,
            () => OSPlatform.Linux,
            () => OSPlatform.OSX
        );

        res.Should().Be(platform);
    }

    [Theory]
    [MemberData(nameof(PlatformsMemberData))]
    [SuppressMessage("ReSharper", "RedundantAssignment")]
    public void Test_MatchPlatform_WithState(OSPlatform platform)
    {
        IOSInformation info = new OSInformation(platform);

        var count = 0;
        var res = info.MatchPlatform(
            (ref int x) =>
            {
                x = OSPlatform.Windows.ToString().Length;
                return OSPlatform.Windows;
            },
            (ref int x) =>
            {
                x = OSPlatform.Linux.ToString().Length;
                return OSPlatform.Linux;
            },
            (ref int x) =>
            {
                x = OSPlatform.OSX.ToString().Length;
                return OSPlatform.OSX;
            },
            ref count
        );

        res.Should().Be(platform);
        count.Should().Be(platform.ToString().Length);
    }

    [Theory]
    [MemberData(nameof(PlatformsMemberData))]
    public void Test_Switch(OSPlatform platform)
    {
        IOSInformation info = new OSInformation(platform);

        OSPlatform res = default;
        info.SwitchPlatform(
            () => res = OSPlatform.Windows,
            () => res = OSPlatform.Linux,
            () => res = OSPlatform.OSX
        );

        res.Should().Be(platform);
    }

    [Theory]
    [MemberData(nameof(PlatformsMemberData))]
    [SuppressMessage("ReSharper", "RedundantAssignment")]
    public void Test_Switch_WithState(OSPlatform platform)
    {
        IOSInformation info = new OSInformation(platform);

        OSPlatform res = default;
        info.SwitchPlatform(
            (ref OSPlatform x) => x = OSPlatform.Windows,
            (ref OSPlatform x) => x = OSPlatform.Linux,
            (ref OSPlatform x) => x = OSPlatform.OSX,
            ref res
        );

        res.Should().Be(platform);
    }

    public static IEnumerable<object[]> PlatformsMemberData => new[]
    {
        new object[] { OSPlatform.Windows },
        new object[] { OSPlatform.Linux },
        new object[] { OSPlatform.OSX },
    };
}
