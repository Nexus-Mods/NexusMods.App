using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NexusMods.CLI.Types;
using NexusMods.CLI.Verbs;

namespace NexusMods.CLI.Tests.VerbTests;

public class ProtocolInvocationTests
{
    private readonly ILogger<ProtocolInvoke> _logger;

    public ProtocolInvocationTests(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<ProtocolInvoke>>();
    }

    [Theory]
    [InlineData("first://path", 1, 0)]
    [InlineData("second://path", 0, 1)]
    public async void WillForwardToRightHandler(string url, int firstTimes, int secondTimes)
    {
        var firstHandler = new Mock<IProtocolHandler>();
        firstHandler.Setup(_ => _.Protocol).Returns("first");
        var secondHandler = new Mock<IProtocolHandler>();
        secondHandler.Setup(_ => _.Protocol).Returns("second");

        var invoke = new ProtocolInvoke(_logger,
            new List<IProtocolHandler> { firstHandler.Object, secondHandler.Object });
        var res = await invoke.Run(url, CancellationToken.None);

        res.Should().Be(0);
        firstHandler.Verify(_ => _.Handle(url, CancellationToken.None), Times.Exactly(firstTimes));
        secondHandler.Verify(_ => _.Handle(url, CancellationToken.None), Times.Exactly(secondTimes));
    }

    [Fact]
    public async void WillThrowOnUnsupportedProtocol()
    {
        var invok = new ProtocolInvoke(_logger, new List<IProtocolHandler>());
        Func<Task<int>> act = async () => await invok.Run("test://foobar", CancellationToken.None);
        await act.Should().ThrowAsync<Exception>();
    }

}
