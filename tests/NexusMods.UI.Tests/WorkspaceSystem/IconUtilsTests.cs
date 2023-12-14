using System.Runtime.CompilerServices;
using Avalonia;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.UI.Tests.WorkspaceSystem;

[UsesVerify]
public class IconUtilsTests(IServiceProvider provider) : AUiTest(provider)
{
    [Fact]
    public Task Test_StateToBitmap_TwoColumns()
    {
        var state = new Dictionary<PanelId, Rect>
        {
            { PanelId.NewId(), new Rect(0, 0, 0.5, 1) },
            { PanelId.DefaultValue, new Rect(0.5, 0, 0.5, 1) }
        };

        return RunVerify(state);
    }

    [Fact]
    public Task Test_StateToBitmap_TwoRows()
    {
        var state = new Dictionary<PanelId, Rect>
        {
            { PanelId.NewId(), new Rect(0, 0, 1, 0.5) },
            { PanelId.DefaultValue, new Rect(0, 0.5, 1, 0.5) }
        };

        return RunVerify(state);
    }

    [Fact]
    public Task Test_StateToBitmap_ThreePanels_OneLargeColumn()
    {
        var state = new Dictionary<PanelId, Rect>
        {
            { PanelId.NewId(), new Rect(0, 0, 0.5, 0.5) },
            { PanelId.DefaultValue, new Rect(0.5, 0, 0.5, 1) },
            { PanelId.NewId(), new Rect(0, 0.5, 0.5, 0.5) }
        };

        return RunVerify(state);
    }

    [Fact]
    public Task Test_StateToBitmap_ThreePanels_OneLargeRow()
    {
        var state = new Dictionary<PanelId, Rect>
        {
            { PanelId.NewId(), new Rect(0, 0, 0.5, 0.5) },
            { PanelId.NewId(), new Rect(0.5, 0, 0.5, 0.5) },
            { PanelId.DefaultValue, new Rect(0, 0.5, 1, 0.5) },
        };

        return RunVerify(state);
    }

    [Fact]
    public Task Test_StateToBitmap_FourPanels()
    {
        var state = new Dictionary<PanelId, Rect>
        {
            { PanelId.NewId(), new Rect(0, 0, 0.5, 0.5) },
            { PanelId.NewId(), new Rect(0, 0.5, 0.5, 0.5) },
            { PanelId.NewId(), new Rect(0.5, 0, 0.5, 0.5) },
            { PanelId.DefaultValue, new Rect(0.5, 0.5, 0.5, 0.5) },
        };

        return RunVerify(state);
    }

    private static Task RunVerify(Dictionary<PanelId, Rect> state, [CallerFilePath] string sourceFile = "")
    {
        using var stream = new MemoryStream();
        using (var bitmap = IconUtils.StateToBitmap(state))
        {
            bitmap.Save(stream);
            stream.Position = 0;
        }

        // ReSharper disable once ExplicitCallerInfoArgument
        return Verify(stream, extension: "png", sourceFile: sourceFile).DisableDiff();
    }
}
