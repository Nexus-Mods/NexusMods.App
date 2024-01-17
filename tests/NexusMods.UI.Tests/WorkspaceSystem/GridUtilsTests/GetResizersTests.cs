using System.Diagnostics.CodeAnalysis;
using Avalonia;
using FluentAssertions;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.UI.Tests.WorkspaceSystem;

[SuppressMessage("ReSharper", "HeapView.BoxingAllocation")]
[SuppressMessage("ReSharper", "HeapView.ObjectAllocation.Evident")]
[SuppressMessage("ReSharper", "HeapView.ObjectAllocation")]
public partial class GridUtilsTests
{
    [Theory]
    [MemberData(nameof(TestData_GetResizers2_Generated))]
    [SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters")]
    public void Test_GetResizers2(
        string name,
        WorkspaceGridState workspaceState,
        List<GridUtils.ResizerInfo> expectedRes)
    {
        GridUtils.IsPerfectGrid(workspaceState).Should().BeTrue();

        var res = GridUtils.GetResizers(workspaceState);
        res.Should().HaveCount(expectedRes.Count);

        for (var i = 0; i < res.Count; i++)
        {
            var actual = res[i];
            var expected = expectedRes[i];

            actual.IsHorizontal.Should().Be(expected.IsHorizontal);
            actual.Start.Should().Be(expected.Start);
            actual.End.Should().Be(expected.End);
            actual.ConnectedPanels.Order().Should().Equal(expected.ConnectedPanels.Order());
        }
    }

    public static TheoryData<string, WorkspaceGridState, List<GridUtils.ResizerInfo>> TestData_GetResizers2_Generated()
    {
        var firstPanelId = PanelId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var secondPanelId = PanelId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        var thirdPanelId = PanelId.From(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        var fourthPanelId = PanelId.From(Guid.Parse("44444444-4444-4444-4444-444444444444"));

        const double step = 0.1;
        const double min = 0.2;
        const double max = 1.0 - min;

        var res = new TheoryData<string, WorkspaceGridState, List<GridUtils.ResizerInfo>>();

        // two columns
        // | 1 | 2 |
        // | 1 | 2 |
        AddTwoColumns(isHorizontal: true);
        AddTwoColumns(isHorizontal: false);

        // two rows
        // | 1 | 1 |
        // | 2 | 2 |
        AddTwoRows(isHorizontal: true);
        AddTwoRows(isHorizontal: false);

        // three panels: one large column
        // | 1 | 2 |
        // | 1 | 3 |
        AddThreePanelsOneLargeColumn(isHorizontal: true);
        AddThreePanelsOneLargeColumn(isHorizontal: false);

        // three panels: one large row
        // | 1 | 1 |
        // | 2 | 3 |
        AddThreePanelsOneLargeRow(isHorizontal: true);
        AddThreePanelsOneLargeRow(isHorizontal: false);

        // four panels: horizontal
        // | 1 | 2 |
        // | 3 | 4 |
        res.Add("four panels | horizontal",
            CreateState(
                isHorizontal: true,
                new PanelGridState(firstPanelId, new Rect(0, 0, 0.5, 0.5)),
                new PanelGridState(secondPanelId, new Rect(0.5, 0, 0.5, 0.5)),
                new PanelGridState(thirdPanelId, new Rect(0, 0.5, 0.5, 0.5)),
                new PanelGridState(fourthPanelId, new Rect(0.5, 0.5, 0.5, 0.5))
            ),
            [
                new GridUtils.ResizerInfo(new Point(0, 0.5), new Point(0.5, 0.5), IsHorizontal: true, [firstPanelId, thirdPanelId]),
                new GridUtils.ResizerInfo(new Point(0.5, 0), new Point(0.5, 1), IsHorizontal: false, [firstPanelId, secondPanelId, thirdPanelId, fourthPanelId]),
                new GridUtils.ResizerInfo(new Point(0.5, 0.5), new Point(1, 0.5), IsHorizontal: true, [secondPanelId, fourthPanelId])
            ]
        );

        // four panels: vertical
        // | 1 | 2 |
        // | 3 | 4 |
        res.Add("four panels | vertical",
            CreateState(
                isHorizontal: false,
                new PanelGridState(firstPanelId, new Rect(0, 0, 0.5, 0.5)),
                new PanelGridState(secondPanelId, new Rect(0.5, 0, 0.5, 0.5)),
                new PanelGridState(thirdPanelId, new Rect(0, 0.5, 0.5, 0.5)),
                new PanelGridState(fourthPanelId, new Rect(0.5, 0.5, 0.5, 0.5))
            ),
            [
                new GridUtils.ResizerInfo(new Point(0.5, 0), new Point(0.5, 0.5), IsHorizontal: false, [firstPanelId, secondPanelId]),
                new GridUtils.ResizerInfo(new Point(0, 0.5), new Point(1, 0.5), IsHorizontal: true, [firstPanelId, secondPanelId, thirdPanelId, fourthPanelId]),
                new GridUtils.ResizerInfo(new Point(0.5, 0.5), new Point(0.5, 1), IsHorizontal: false, [thirdPanelId, fourthPanelId]),
            ]
        );

        return res;

        string ToText(bool isHorizontal) => isHorizontal ? "horizontal" : "vertical";

        void AddTwoColumns(bool isHorizontal)
        {
            res.Add($"two columns | {ToText(isHorizontal)}",
                CreateState(
                    isHorizontal: isHorizontal,
                    new PanelGridState(firstPanelId, new Rect(0, 0, 0.7, 1)),
                    new PanelGridState(secondPanelId, new Rect(0.7, 0, 0.3, 1))
                ), [
                    new GridUtils.ResizerInfo(new Point(0.7, 0), new Point(0.7, 1), IsHorizontal: false, [firstPanelId, secondPanelId]),
                ]);
        }

        void AddTwoRows(bool isHorizontal)
        {
            res.Add($"two rows | {ToText(isHorizontal)}",
                CreateState(
                    isHorizontal: isHorizontal,
                    new PanelGridState(firstPanelId, new Rect(0, 0, 1, 0.7)),
                    new PanelGridState(secondPanelId, new Rect(0, 0.7, 1, 0.3))
                ), [
                    new GridUtils.ResizerInfo(new Point(0, 0.7), new Point(1, 0.7), IsHorizontal: true, [firstPanelId, secondPanelId]),
                ]);
        }

        void AddThreePanelsOneLargeColumn(bool isHorizontal)
        {
            res.Add($"three panels with one large column | {ToText(isHorizontal)}",
                CreateState(
                    isHorizontal: isHorizontal,
                    new PanelGridState(firstPanelId, new Rect(0, 0, 0.7, 1)),
                    new PanelGridState(secondPanelId, new Rect(0.7, 0, 0.3, 0.4)),
                    new PanelGridState(thirdPanelId, new Rect(0.7, 0.4, 0.3, 0.6))
                ),
                [
                    new GridUtils.ResizerInfo(new Point(0.7, 0), new Point(0.7, 1), IsHorizontal: false, [firstPanelId, secondPanelId, thirdPanelId]),
                    new GridUtils.ResizerInfo(new Point(0.7, 0.4), new Point(1, 0.4), IsHorizontal: true, [secondPanelId, thirdPanelId])
                ]
            );
        }

        void AddThreePanelsOneLargeRow(bool isHorizontal)
        {
            res.Add($"three panels with one large row | {ToText(isHorizontal)}",
                CreateState(
                    isHorizontal: isHorizontal,
                    new PanelGridState(firstPanelId, new Rect(0, 0, 1, 0.4)),
                    new PanelGridState(secondPanelId, new Rect(0, 0.4, 0.7, 0.6)),
                    new PanelGridState(thirdPanelId, new Rect(0.7, 0.4, 0.3, 0.6))
                ),
                [
                    new GridUtils.ResizerInfo(new Point(0, 0.4), new Point(1, 0.4), IsHorizontal: true, [firstPanelId, secondPanelId, thirdPanelId]),
                    new GridUtils.ResizerInfo(new Point(0.7, 0.4), new Point(0.7, 1), IsHorizontal: false, [secondPanelId, thirdPanelId]),
                ]
            );
        }
    }
}
