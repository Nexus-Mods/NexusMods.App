using Moq;
using CliWrap;
using NexusMods.Common.OSInterop;

namespace NexusMods.Common.Tests;

// ReSharper disable once InconsistentNaming
public class OSInteropTests
{
    [Fact]
    public async Task UsesExplorerOnWindows()
    {
        const string url = "foobar://test";
        var mockFactory = new Mock<IProcessFactory>();

        var os = new OSInteropWindows(mockFactory.Object);
        await os.OpenUrl(url);

        mockFactory.Verify(f => f.ExecuteAsync(
            It.Is<Command>(command =>
                command.TargetFilePath == "cmd.exe" &&
                command.Arguments == $@"/c start """" ""{url}"""),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task UsesXDGOpenOnLinux()
    {
        const string url = "foobar://test";
        var mockFactory = new Mock<IProcessFactory>();

        var os = new OSInteropLinux(mockFactory.Object);
        await os.OpenUrl(url);

        mockFactory.Verify(f => f.ExecuteAsync(
            It.Is<Command>(command =>
                command.TargetFilePath == "xdg-open" &&
                command.Arguments == url),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task UsesOpenOnOSX()
    {
        const string url = "foobar://test";
        var mockFactory = new Mock<IProcessFactory>();

        var os = new OSInteropOSX(mockFactory.Object);
        await os.OpenUrl(url);

        mockFactory.Verify(f => f.ExecuteAsync(
            It.Is<Command>(command =>
                command.TargetFilePath == "open" &&
                command.Arguments == url),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }
}
