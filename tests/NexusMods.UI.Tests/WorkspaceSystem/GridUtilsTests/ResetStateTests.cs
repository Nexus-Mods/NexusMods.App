using Avalonia;
using FluentAssertions;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.UI.Tests.WorkspaceSystem;

public partial class GridUtilsTests
{
    [Theory]
    [MemberData(nameof(TestData_ResetState))]
    public void Test_ResetState(WorkspaceGridState input, WorkspaceGridState expected)
    {
        GridUtils.IsPerfectGrid(expected).Should().BeTrue();

        var res = GridUtils.ResetState(input, maxColumns: 2, maxRows: 2);
        res.Should().HaveCount(expected.Count);
        res.Should().Equal(expected);
    }

    public static TheoryData<WorkspaceGridState, WorkspaceGridState> TestData_ResetState()
    {
        var firstPanelId = PanelId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var secondPanelId = PanelId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        var thirdPanelId = PanelId.From(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        var fourthPanelId = PanelId.From(Guid.Parse("44444444-4444-4444-4444-444444444444"));

        var res = new TheoryData<WorkspaceGridState, WorkspaceGridState>();

        res.Add(
            CreateState(isHorizontal: true,
                new PanelGridState(firstPanelId, MathUtils.One),
                new PanelGridState(secondPanelId, MathUtils.One),
                new PanelGridState(thirdPanelId, MathUtils.One),
                new PanelGridState(fourthPanelId, MathUtils.One)
            ),
            CreateState(isHorizontal: true,
                new PanelGridState(firstPanelId, new Rect(0, 0, 0.5, 0.5)),
                new PanelGridState(secondPanelId, new Rect(0, 0.5, 0.5, 0.5)),
                new PanelGridState(thirdPanelId, new Rect(0.5, 0, 0.5, 0.5)),
                new PanelGridState(fourthPanelId, new Rect(0.5, 0.5, 0.5, 0.5))
            )
        );

        res.Add(
            CreateState(isHorizontal: true,
                new PanelGridState(firstPanelId, new Rect(0, 0, 0.5, 0.5)),
                new PanelGridState(secondPanelId, new Rect(0, 0.5, 0.5, 0.5)),
                new PanelGridState(thirdPanelId, new Rect(0.5, 0, 0.5, 0.5)),
                new PanelGridState(fourthPanelId, new Rect(0.5, 0.5, 0.5, 0.5))
            ),
            CreateState(isHorizontal: true,
                new PanelGridState(firstPanelId, new Rect(0, 0, 0.5, 0.5)),
                new PanelGridState(secondPanelId, new Rect(0, 0.5, 0.5, 0.5)),
                new PanelGridState(thirdPanelId, new Rect(0.5, 0, 0.5, 0.5)),
                new PanelGridState(fourthPanelId, new Rect(0.5, 0.5, 0.5, 0.5))
            )
        );

        return res;
    }
}
