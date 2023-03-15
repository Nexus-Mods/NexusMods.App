using System.Runtime.Versioning;
using FluentAssertions;
using NexusMods.Common.ProtocolRegistration;

namespace NexusMods.Common.Tests;

public class ProtocolRegistrationTests
{
    [SkippableFact]
    [SupportedOSPlatform("linux")]
    public async Task ShouldWork_IsSelfHandler_Linux()
    {
        Skip.If(!OperatingSystem.IsLinux());

        const string protocol = "foo";
        var processFactory = new FakeProcessFactory(0)
        {
            StandardOutput = "yes\n"
        };

        var protocolRegistration = new ProtocolRegistrationLinux(processFactory);
        var res = await protocolRegistration.IsSelfHandler(protocol);
        res.Should().BeTrue();
    }

    [SkippableFact]
    [SupportedOSPlatform("linux")]
    public async Task ShouldError_IsSelfHandler_Linux()
    {
        Skip.If(!OperatingSystem.IsLinux());

        const string protocol = "foo";
        var processFactory = new FakeProcessFactory(0)
        {
            StandardOutput = "no\n"
        };

        var protocolRegistration = new ProtocolRegistrationLinux(processFactory);
        var res = await protocolRegistration.IsSelfHandler(protocol);
        res.Should().BeFalse();
    }
}
