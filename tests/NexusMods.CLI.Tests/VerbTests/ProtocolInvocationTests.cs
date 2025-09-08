using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.CLI.Types;
using NSubstitute;

namespace NexusMods.CLI.Tests.VerbTests;

public class ProtocolInvocationTests(IServiceProvider provider) : AVerbTest(provider)
{
    private readonly ILogger<ProtocolInvocationTests> _logger = provider.GetRequiredService<ILogger<ProtocolInvocationTests>>();

    [Theory]
    [InlineData("first://path", 1, 0)]
    [InlineData("second://path", 0, 1)]
    public async Task WillForwardToRightHandler(string url, int firstTimes, int secondTimes)
    {
        var firstHandler = Substitute.For<IIpcProtocolHandler>();
        firstHandler.Protocol.Returns("first");

        var secondHandler = Substitute.For<IIpcProtocolHandler>();
        secondHandler.Protocol.Returns("second");

        var loggingRenderer = new LoggingRenderer();
        var parsed = new Uri(url);
        var res = await RunDirectly("protocol-invoke", loggingRenderer, parsed, new List<IIpcProtocolHandler> { firstHandler, secondHandler }, CancellationToken.None);
        res.Should().Be(0, "the command should have succeeded");

        // We convert parsed back to string here because the Uri class will normalize the path (adding a `/` ), which will break the test
        await firstHandler.Received(firstTimes).Handle(parsed.ToString(), CancellationToken.None);
        await secondHandler.Received(secondTimes).Handle(parsed.ToString(), CancellationToken.None);
    }

    [Fact]
    public async Task WillThrowOnUnsupportedProtocol()
    {
        var loggingRenderer = new LoggingRenderer();
        var parsed = new Uri("test://foobar");
        var action = async () => await RunDirectly("protocol-invoke", loggingRenderer, parsed, new List<IIpcProtocolHandler> { }, CancellationToken.None);
        await action.Should().ThrowAsync<Exception>();
    }

}
