using System.Runtime.Versioning;
using FluentAssertions;
using NexusMods.CrossPlatform.Process;
using NexusMods.CrossPlatform.ProtocolRegistration;
using NexusMods.Paths;

namespace NexusMods.CrossPlatform.Tests;

public class ProtocolRegistrationTests(IOSInterop interop)
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

        var protocolRegistration = new ProtocolRegistrationLinux(processFactory, FileSystem.Shared, interop);
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
            StandardOutput = "no\n",
        };

        var protocolRegistration = new ProtocolRegistrationLinux(processFactory, FileSystem.Shared, interop);
        var res = await protocolRegistration.IsSelfHandler(protocol);
        res.Should().BeFalse();
    }
}
