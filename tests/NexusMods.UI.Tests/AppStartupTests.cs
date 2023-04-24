using FluentAssertions;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace NexusMods.UI.Tests;

public class AppStartupTests
{

    [Fact]
    public void CanBuildHost()
    {
        var host = App.Program.BuildHost();
        host.Should().NotBeNull();
    }
}
