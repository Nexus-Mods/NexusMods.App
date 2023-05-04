using Avalonia.Controls;
using Avalonia.Markup.Parsers;
using FluentAssertions;
using NexusMods.UI.Tests.Framework;

namespace NexusMods.UI.Tests;

public class EndToEndSanityTest : AEndToEndTest
{
    public EndToEndSanityTest(AvaloniaApp app) : base(app) { }

    [Fact]
    public async Task CanGetControlsInTheMainWindow()
    {
        await Host!.SnapShot();
        var found = await Host!.Select<Button>("Button#LoginButton");
        found.Count().Should().BeGreaterOrEqualTo(1);
    }
}
