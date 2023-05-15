using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using AutoFixture.Xunit2;
using FluentAssertions;

namespace NexusMods.Common.Tests;

public class OSInformationTests
{
    [Theory]
    [MemberData(nameof(PlatformsMemberData))]
    public void Test_IsPlatformSupported_True(OSPlatform platform)
    {
        IOSInformation info = new OSInformation(platform);
        info.IsPlatformSupported().Should().BeTrue();

        info.Invoking(x => x.PlatformSupportedGuard())
            .Should().NotThrow<PlatformNotSupportedException>();
    }

    [Theory, AutoData]
    public void Test_IsPlatformSupported_False(string platformName)
    {
        IOSInformation info = new OSInformation(OSPlatform.Create(platformName));
        info.IsPlatformSupported().Should().BeFalse();

        info.Invoking(x => x.PlatformSupportedGuard())
            .Should().ThrowExactly<PlatformNotSupportedException>();
    }

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
    [MemberData(nameof(PlatformsMemberDataWithUnsupported))]
    public void Test_MatchPlatform_Exception(OSPlatform platform)
    {
        IOSInformation info = new OSInformation(platform);

        info.Invoking(x => x.MatchPlatform<int>(
            onWindows: platform == OSPlatform.Windows ? null : () => 0,
            onLinux: platform == OSPlatform.Linux ? null : () => 0,
            onOSX: platform == OSPlatform.OSX ? null : () => 0
        )).Should().ThrowExactly<PlatformNotSupportedException>();
    }

    [Theory]
    [MemberData(nameof(PlatformsMemberData))]
    [SuppressMessage("ReSharper", "RedundantAssignment")]
    public void Test_MatchPlatform_WithState(OSPlatform platform)
    {
        IOSInformation info = new OSInformation(platform);

        var count = 0;
        var res = info.MatchPlatform(
            ref count,
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
            }
        );

        res.Should().Be(platform);
        count.Should().Be(platform.ToString().Length);
    }

    [Theory]
    [MemberData(nameof(PlatformsMemberDataWithUnsupported))]
    public void Test_MatchPlatform_WithState_Exception(OSPlatform platform)
    {
        IOSInformation info = new OSInformation(platform);

        var i = 0;
        info.Invoking(x => x.MatchPlatform<int, int>(
            ref i,
            onWindows: platform == OSPlatform.Windows ? null : (ref int _) => 0,
            onLinux: platform == OSPlatform.Linux ? null : (ref int _) => 0,
            onOSX: platform == OSPlatform.OSX ? null : (ref int _) => 0
        )).Should().ThrowExactly<PlatformNotSupportedException>();
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
    [MemberData(nameof(PlatformsMemberDataWithUnsupported))]
    public void Test_Switch_Exception(OSPlatform platform)
    {
        IOSInformation info = new OSInformation(platform);

        info.Invoking(x => x.SwitchPlatform(
            onWindows: platform == OSPlatform.Windows ? null : () => { },
            onLinux: platform == OSPlatform.Linux ? null : () => { },
            onOSX: platform == OSPlatform.OSX ? null : () => { })
        ).Should().ThrowExactly<PlatformNotSupportedException>();
    }

    [Theory]
    [MemberData(nameof(PlatformsMemberData))]
    [SuppressMessage("ReSharper", "RedundantAssignment")]
    public void Test_Switch_WithState(OSPlatform platform)
    {
        IOSInformation info = new OSInformation(platform);

        OSPlatform res = default;
        info.SwitchPlatform(
            ref res,
            (ref OSPlatform x) => x = OSPlatform.Windows,
            (ref OSPlatform x) => x = OSPlatform.Linux,
            (ref OSPlatform x) => x = OSPlatform.OSX
        );

        res.Should().Be(platform);
    }

    [Theory]
    [MemberData(nameof(PlatformsMemberDataWithUnsupported))]
    public void Test_Switch_WithState_Exception(OSPlatform platform)
    {
        IOSInformation info = new OSInformation(platform);

        var i = 0;
        info.Invoking(x => x.SwitchPlatform(
            ref i,
            onWindows: platform == OSPlatform.Windows ? null : (ref int _) => { },
            onLinux: platform == OSPlatform.Linux ? null : (ref int _) => { },
            onOSX: platform == OSPlatform.OSX ? null : (ref int _) => { })
        ).Should().ThrowExactly<PlatformNotSupportedException>();
    }

    public static IEnumerable<object[]> PlatformsMemberData => new[]
    {
        new object[] { OSPlatform.Windows },
        new object[] { OSPlatform.Linux },
        new object[] { OSPlatform.OSX },
    };

    public static IEnumerable<object[]> PlatformsMemberDataWithUnsupported => new[]
    {
        new object[] { OSPlatform.Windows },
        new object[] { OSPlatform.Linux },
        new object[] { OSPlatform.OSX },
        new object[] { OSPlatform.FreeBSD },
    };
}
