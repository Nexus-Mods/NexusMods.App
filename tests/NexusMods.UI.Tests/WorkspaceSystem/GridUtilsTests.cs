using Avalonia;
using FluentAssertions;
using NexusMods.App.UI.WorkspaceSystem;
using NSubstitute;

namespace NexusMods.UI.Tests.WorkspaceSystem;

public class GridUtilsTests
{
    [Fact]
    public void Test_GetAvailableCreationInfos()
    {
        const int columns = 2;
        const int rows = 2;
        var panels = new List<IPanelViewModel>(capacity: columns * rows);

        var res = GridUtils.GetAvailableCreationInfos(panels, columns, rows).ToArray();
        res.Should().BeEmpty();

        var mainPanel = CreatePanel(new Rect(0, 0, 1, 1));
        panels.Add(mainPanel);

        res = GridUtils.GetAvailableCreationInfos(panels, columns, rows).ToArray();
        res.Should().ContainInOrder(new[]
        {
            new PanelCreationInfo(mainPanel, new Rect(0, 0, 0.5, 1), new Rect(0.5, 0, 0.5, 1)),
            new PanelCreationInfo(mainPanel, new Rect(0, 0, 1, 0.5), new Rect(0, 0.5, 1, 0.5)),
        });

        var info = res.First();
        info.PanelToSplit.LogicalBounds = info.UpdatedLogicalBounds;

        var verticalPanel = CreatePanel(info.NewPanelLogicalBounds);
        panels.Add(verticalPanel);

        res = GridUtils.GetAvailableCreationInfos(panels, columns, rows).ToArray();
        res.Should().ContainInOrder(new[]
        {
            new PanelCreationInfo(mainPanel, new Rect(0, 0, 0.5, 0.5), new Rect(0, 0.5, 0.5, 0.5)),
            new PanelCreationInfo(mainPanel, new Rect(0, 0.5, 0.5, 0.5), new Rect(0, 0, 0.5, 0.5)),

            new PanelCreationInfo(verticalPanel, new Rect(0.5, 0, 0.5, 0.5), new Rect(0.5, 0.5, 0.5, 0.5)),
            new PanelCreationInfo(verticalPanel, new Rect(0.5, 0.5, 0.5, 0.5), new Rect(0.5, 0, 0.5, 0.5)),
        });

        info = res.First();
        info.PanelToSplit.LogicalBounds = info.UpdatedLogicalBounds;

        var bottomLeftPanel = CreatePanel(info.NewPanelLogicalBounds);
        panels.Add(bottomLeftPanel);

        res = GridUtils.GetAvailableCreationInfos(panels, columns, rows).ToArray();
        res.Should().ContainInOrder(new[]
        {
            new PanelCreationInfo(verticalPanel, new Rect(0.5, 0, 0.5, 0.5), new Rect(0.5, 0.5, 0.5, 0.5)),
            new PanelCreationInfo(verticalPanel, new Rect(0.5, 0.5, 0.5, 0.5), new Rect(0.5, 0, 0.5, 0.5)),
        });

        info = res.First();
        info.PanelToSplit.LogicalBounds = info.UpdatedLogicalBounds;

        var bottomRightPanel = CreatePanel(info.NewPanelLogicalBounds);
        panels.Add(bottomRightPanel);

        res = GridUtils.GetAvailableCreationInfos(panels, columns, rows).ToArray();
        res.Should().BeEmpty();
    }

    private static IPanelViewModel CreatePanel(Rect logicalBounds)
    {
        var panel = Substitute.For<IPanelViewModel>();
        panel.LogicalBounds.Returns(logicalBounds);
        return panel;
    }
}
