using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NexusMods.CLI.Types;
using NexusMods.CLI.Verbs;

namespace NexusMods.CLI.Tests.VerbTests;

public class ProtocolInvocationTests
{
    private ILogger<ProtocolInvocationTests> _logger;

    public ProtocolInvocationTests(ILogger<ProtocolInvocationTests> logger)
    {
        _logger = logger;
    }

    [Theory()]
    [InlineData("first://path", 1, 0)]
    [InlineData("second://path", 0, 1)]
    public async void WillForwardToRightHandler(string url, int firstTimes, int secondTimes)
    {
        var firstHandler = new Mock<IProtocolHandler>();
        firstHandler.Setup(_ => _.Protocol).Returns("first");
        var secondHandler = new Mock<IProtocolHandler>();
        secondHandler.Setup(_ => _.Protocol).Returns("second");

        var invok = new ProtocolInvocation(_logger, new List<IProtocolHandler> { firstHandler.Object, secondHandler.Object });
        var res = await invok.Run(url, CancellationToken.None);

        res.Should().Be(0);
        firstHandler.Verify(_ => _.Handle(url, CancellationToken.None), Times.Exactly(firstTimes));
        secondHandler.Verify(_ => _.Handle(url, CancellationToken.None), Times.Exactly(secondTimes));
    }

    [Fact()]
    public async void WillThrowOnUnsupportedProtocol()
    {
        var invok = new ProtocolInvocation(_logger, new List<IProtocolHandler>());
        Func<Task<int>> act = async () => await invok.Run("test://foobar", CancellationToken.None);
        await act.Should().ThrowAsync<Exception>();
    }

}
