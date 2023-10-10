using Avalonia;
using FluentAssertions;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.UI.Tests.WorkspaceSystem;

public class IconUtilsTests : AUiTest
{
    public IconUtilsTests(IServiceProvider provider) : base(provider) { }

    [Fact]
    public void Test_StateToBitmap()
    {
        var state = new Dictionary<PanelId, Rect>
        {
            { PanelId.New(), new Rect(0, 0, 0.5, 1) },
            { PanelId.Empty, new Rect(0.5, 0, 0.5, 1) }
        };

        var bitmap = IconUtils.StateToBitmap(state);
        bitmap.Size.Should().Be(new Size(180, 180));
    }
}
