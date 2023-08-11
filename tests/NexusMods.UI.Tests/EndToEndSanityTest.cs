using Avalonia.Controls;
using FluentAssertions;
using NexusMods.UI.Tests.Framework;

namespace NexusMods.UI.Tests;

public class EndToEndSanityTest : AEndToEndTest
{
    public EndToEndSanityTest(AvaloniaApp app) : base(app) { }

    [Fact]
    public async Task CanGetControlsInTheMainWindow()
    {
        var found = await Host.Select<Button>("LoginButton");
        found.Should().HaveCountGreaterOrEqualTo(1);
    }
}
