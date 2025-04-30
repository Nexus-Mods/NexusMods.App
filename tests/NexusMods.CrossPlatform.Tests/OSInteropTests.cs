using CliWrap;
using Microsoft.Extensions.Logging;
using NexusMods.CrossPlatform.Process;
using NexusMods.Paths;
using NSubstitute;

namespace NexusMods.CrossPlatform.Tests;

// ReSharper disable once InconsistentNaming
public class OSInteropTests
{
    [Fact]
    public async Task UsesExplorerOnWindows()
    {
        var url = new Uri("foobar://test");

        const string targetFilePath = "cmd.exe";
        var arguments = $@"/c start """" ""{url}""";

        await TestWithCommand((loggerFactory, processFactory) => new OSInteropWindows(loggerFactory, processFactory, FileSystem.Shared), url, targetFilePath, arguments);
    }

    [Fact]
    public async Task UsesXDGOpenOnLinux()
    {
        var url = new Uri("foobar://test");

        const string targetFilePath = "xdg-open";
        var arguments = url.ToString();

        await TestWithCommand((loggerFactory, processFactory) => new OSInteropWindows(loggerFactory, processFactory, FileSystem.Shared), url, targetFilePath, arguments);
    }

    [Fact]
    public async Task UsesOpenOnOSX()
    {
        var url = new Uri("foobar://test");

        const string targetFilePath = "open";
        var arguments = url.ToString();

        await TestWithCommand((loggerFactory, processFactory) => new OSInteropWindows(loggerFactory, processFactory, FileSystem.Shared), url, targetFilePath, arguments);
    }

    private static async Task TestWithCommand(
        Func<ILoggerFactory, IProcessFactory, IOSInterop> osFactory,
        Uri url,
        string targetFilePath,
        string arguments)
    {
        var loggerFactory = LoggerFactory.Create(_ => { });

        var processFactory = Substitute.For<IProcessFactory>();
        processFactory
            .ExecuteAsync(Arg.Is<Command>(command =>
                    command.TargetFilePath == targetFilePath &&
                    command.Arguments == arguments),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(Task.FromResult(new CommandResult(0, DateTimeOffset.Now, DateTimeOffset.Now)));

        var os = osFactory(loggerFactory, processFactory);
        await os.OpenUrl(url);

        await processFactory
            .Received(1)
            .ExecuteAsync(
                Arg.Any<Command>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>()
            );
    }
}
