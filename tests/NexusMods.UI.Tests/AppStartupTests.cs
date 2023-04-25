using FluentAssertions;

namespace NexusMods.UI.Tests;

public class AppStartupTests
{

    [Fact]
    public void CanBuildHost()
    {
        var host = App.Program.BuildHost();
        App.UI.Startup.BuildAvaloniaApp(host.Services).Should().NotBeNull();
    }
}
