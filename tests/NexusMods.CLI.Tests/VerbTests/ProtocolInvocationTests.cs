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
    public async void WillForwardToRightHandler(string url, int firstTimes, int secondTimes)
    {
        var firstHandler = Substitute.For<IIpcProtocolHandler>();
        firstHandler.Protocol.Returns("first");

        var secondHandler = Substitute.For<IIpcProtocolHandler>();
        secondHandler.Protocol.Returns("second");
/* TODO Fix this
        var invoke = new ProtocolInvoke(_logger, new List<IIpcProtocolHandler> { firstHandler, secondHandler });
        var res = await invoke.Run("protocol-invoke", url, CancellationToken.None);

        res.Should().Be(0);
        await firstHandler.Received(firstTimes).Handle(url, CancellationToken.None);
        await secondHandler.Received(secondTimes).Handle(url, CancellationToken.None);
        */
    }

    [Fact]
    public async void WillThrowOnUnsupportedProtocol()
    {
        /* Fix this
        var invok = new ProtocolInvoke(_logger, new List<IIpcProtocolHandler>());
        Func<Task<int>> act = async () => await invok.Run("test://foobar", CancellationToken.None);
        await act.Should().ThrowAsync<Exception>();
        */
    }

}
