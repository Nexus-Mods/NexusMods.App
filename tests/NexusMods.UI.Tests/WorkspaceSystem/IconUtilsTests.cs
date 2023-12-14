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
        var state = WorkspaceGridState.From(
            isHorizontal: true,
            new PanelGridState(PanelId.NewId(),new Rect(0, 0, 0.5, 1)),
            new PanelGridState(PanelId.DefaultValue, new Rect(0.5, 0, 0.5, 1))
        );

        return RunVerify(state);
    }

    [Fact]
    public Task Test_StateToBitmap_TwoRows()
    {
        var state = WorkspaceGridState.From(
            isHorizontal: true,
            new PanelGridState(PanelId.NewId(), new Rect(0, 0, 1, 0.5)),
            new PanelGridState(PanelId.DefaultValue,  new Rect(0, 0.5, 1, 0.5))
        );

        return RunVerify(state);
    }

    [Fact]
    public Task Test_StateToBitmap_ThreePanels_OneLargeColumn()
    {
        var state = WorkspaceGridState.From(
            isHorizontal: true,
            new PanelGridState(PanelId.NewId(),new Rect(0, 0, 0.5, 0.5)),
            new PanelGridState(PanelId.DefaultValue, new Rect(0.5, 0, 0.5, 1)),
            new PanelGridState(PanelId.NewId(), new Rect(0, 0.5, 0.5, 0.5))
        );

        return RunVerify(state);
    }

    [Fact]
    public Task Test_StateToBitmap_ThreePanels_OneLargeRow()
    {
        var state = WorkspaceGridState.From(
            isHorizontal: true,
            new PanelGridState(PanelId.NewId(), new Rect(0, 0, 0.5, 0.5)),
            new PanelGridState(PanelId.NewId(), new Rect(0.5, 0, 0.5, 0.5)),
            new PanelGridState(PanelId.DefaultValue,new Rect(0, 0.5, 1, 0.5))
        );

        return RunVerify(state);
    }

    [Fact]
    public Task Test_StateToBitmap_FourPanels()
    {
        var state = WorkspaceGridState.From(
            isHorizontal: true,
            new PanelGridState(PanelId.NewId(), new Rect(0, 0, 0.5, 0.5)),
            new PanelGridState(PanelId.NewId(), new Rect(0, 0.5, 0.5, 0.5)),
            new PanelGridState(PanelId.NewId(), new Rect(0.5, 0, 0.5, 0.5)),
            new PanelGridState(PanelId.DefaultValue, new Rect(0.5, 0.5, 0.5, 0.5))
        );

        return RunVerify(state);
    }

    private static Task RunVerify(WorkspaceGridState state, [CallerFilePath] string sourceFile = "")
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
