using CliWrap;
using NexusMods.Common.OSInterop;
using NSubstitute;

namespace NexusMods.Common.Tests;

// ReSharper disable once InconsistentNaming
public class OSInteropTests
{
    [Fact]
    public async Task UsesExplorerOnWindows()
    {
        var url = new Uri("foobar://test");

        const string targetFilePath = "cmd.exe";
        var arguments = $@"/c start """" ""{url}""";

        await TestWithCommand(factory => new OSInteropWindows(factory), url, targetFilePath, arguments);
    }

    [Fact]
    public async Task UsesXDGOpenOnLinux()
    {
        var url = new Uri("foobar://test");

        const string targetFilePath = "xdg-open";
        var arguments = url.ToString();

        await TestWithCommand(factory => new OSInteropLinux(factory), url, targetFilePath, arguments);
    }

    [Fact]
    public async Task UsesOpenOnOSX()
    {
        var url = new Uri("foobar://test");

        const string targetFilePath = "open";
        var arguments = url.ToString();

        await TestWithCommand(factory => new OSInteropOSX(factory), url, targetFilePath, arguments);
    }

    private static async Task TestWithCommand(Func<IProcessFactory, IOSInterop> osFactory, Uri url, string targetFilePath, string arguments)
    {
        var factory = Substitute.For<IProcessFactory>();
        factory
            .ExecuteAsync(Arg.Is<Command>(command =>
                    command.TargetFilePath == targetFilePath &&
                    command.Arguments == arguments),
                Arg.Any<CancellationToken>()
            )
            .Returns(Task.FromResult(new CommandResult(0, DateTimeOffset.Now, DateTimeOffset.Now)));

        var os = osFactory(factory);
        await os.OpenUrl(url);

        await factory
            .Received(1)
            .ExecuteAsync(
                Arg.Is<Command>(command =>
                    command.TargetFilePath == targetFilePath &&
                    command.Arguments == arguments),
                Arg.Any<CancellationToken>()
            );
    }
}
