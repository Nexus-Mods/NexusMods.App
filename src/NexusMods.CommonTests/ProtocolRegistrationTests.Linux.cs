using FluentAssertions;
using NexusMods.Common.ProtocolRegistration;

namespace NexusMods.Common.Tests;

public partial class ProtocolRegistrationTests
{
    [Fact]
    public async Task ShouldWork_IsSelfHandler_Linux()
    {
        const string protocol = "foo";

        var processFactory = new FakeProcessFactory(0)
        {
            StandardOutput = $"nexusmods-app-{protocol}.desktop\n"
        };

        var protocolRegistration = new ProtocolRegistrationLinux(processFactory);

        var res = await protocolRegistration.IsSelfHandler(protocol);
        res.Should().BeTrue();
    }
}
