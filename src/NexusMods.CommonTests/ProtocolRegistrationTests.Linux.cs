using FluentAssertions;
using NexusMods.Common.ProtocolRegistration;

namespace NexusMods.Common.Tests;

public class ProtocolRegistrationTests
{
    [Fact]
    public async Task ShouldWork_IsSelfHandler_Linux()
    {
        const string protocol = "foo";

        var processFactory = new FakeProcessFactory(0)
        {
            StandardOutput = "yes\n"
        };

        var protocolRegistration = new ProtocolRegistrationLinux(processFactory);

        var res = await protocolRegistration.IsSelfHandler(protocol);
        res.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldError_IsSelfHandler_Linux()
    {
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
