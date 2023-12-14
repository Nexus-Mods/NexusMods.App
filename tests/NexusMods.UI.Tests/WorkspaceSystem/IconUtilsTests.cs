using Avalonia;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.UI.Tests.WorkspaceSystem;

[UsesVerify]
public class IconUtilsTests(IServiceProvider provider) : AUiTest(provider)
{
    [Fact]
    public Task Test_StateToBitmap()
    {
        var state = new Dictionary<PanelId, Rect>
        {
            { PanelId.NewId(), new Rect(0, 0, 0.5, 1) },
            { PanelId.DefaultValue, new Rect(0.5, 0, 0.5, 1) }
        };

        using var stream = new MemoryStream();
        using (var bitmap = IconUtils.StateToBitmap(state))
        {
            bitmap.Save(stream);
            stream.Position = 0;
        }

        return Verify(stream, extension: "png").DisableDiff();
    }
}
