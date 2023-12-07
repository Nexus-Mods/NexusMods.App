using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using FluentAssertions;
using JetBrains.Annotations;
using NexusMods.App.UI.WorkspaceSystem;
using NSubstitute;

namespace NexusMods.UI.Tests.WorkspaceSystem;

[UsedImplicitly]
[SuppressMessage("ReSharper", "HeapView.BoxingAllocation")]
public partial class GridUtilsTests
{
    [Fact]
    public void Test_GetResizers()
    {
        var firstPanelId = PanelId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var secondPanelId = PanelId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        var thirdPanelId = PanelId.From(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        var fourthPanelId = PanelId.From(Guid.Parse("44444444-4444-4444-4444-444444444444"));

        const int columns = 2;
        const int rows = 2;

        // NOTE(erri120): two columns
        // | a | b |
        // | a | b |
        var state = new List<IPanelViewModel>(capacity: columns * rows)
        {
            CreatePanel(firstPanelId, new Rect(0, 0, 0.5, 1)),
            CreatePanel(secondPanelId, new Rect(0.5, 0, 0.5, 1))
        }.ToImmutableDictionary(panel => panel.Id, panel => panel.LogicalBounds);

        var res = GridUtils.GetResizers(state);
        res.Should().ContainSingle().And.SatisfyRespectively(info =>
        {
            info.IsHorizontal.Should().BeFalse();
            info.LogicalPosition.Should().Be(new Point(0.5, 0.5));
            info.ConnectedPanels.Should().HaveCount(2).And.Contain(new[] { firstPanelId, secondPanelId });
        });


        // NOTE(erri120): two rows
        // | a | a |
        // | b | b |
        state = new List<IPanelViewModel>(capacity: columns * rows)
        {
            CreatePanel(firstPanelId, new Rect(0, 0, 1, 0.5)),
            CreatePanel(secondPanelId, new Rect(0, 0.5, 1, 0.5))
        }.ToImmutableDictionary(panel => panel.Id, panel => panel.LogicalBounds);

        res = GridUtils.GetResizers(state);
        res.Should().ContainSingle().And.SatisfyRespectively(info =>
        {
            info.IsHorizontal.Should().BeTrue();
            info.LogicalPosition.Should().Be(new Point(0.5, 0.5));
            info.ConnectedPanels.Should().HaveCount(2).And.Contain(new[] { firstPanelId, secondPanelId });
        });


        // NOTE(erri120):
        // | a | b |
        // | a | c |
        state = new List<IPanelViewModel>(capacity: columns * rows)
        {
            CreatePanel(firstPanelId, new Rect(0, 0, 0.5, 1)),
            CreatePanel(secondPanelId, new Rect(0.5, 0, 0.5, 0.5)),
            CreatePanel(thirdPanelId, new Rect(0.5, 0.5, 0.5, 0.5))
        }.ToImmutableDictionary(panel => panel.Id, panel => panel.LogicalBounds);

        res = GridUtils.GetResizers(state);
        res.Should().HaveCount(3).And.SatisfyRespectively(info =>
        {
            info.IsHorizontal.Should().BeFalse();
            info.LogicalPosition.Should().Be(new Point(0.5, 0.25));
            info.ConnectedPanels.Should().HaveCount(3).And.Contain(new[] { firstPanelId, secondPanelId, thirdPanelId });
        }, info =>
        {
            info.IsHorizontal.Should().BeFalse();
            info.LogicalPosition.Should().Be(new Point(0.5, 0.75));
            info.ConnectedPanels.Should().HaveCount(3).And.Contain(new[] { firstPanelId, secondPanelId, thirdPanelId });
        }, info =>
        {
            info.IsHorizontal.Should().BeTrue();
            info.LogicalPosition.Should().Be(new Point(0.75, 0.5));
            info.ConnectedPanels.Should().HaveCount(2).And.Contain(new[] { secondPanelId, thirdPanelId });
        });


        // NOTE(erri120):
        // | a | a |
        // | b | c |
        state = new List<IPanelViewModel>(capacity: columns * rows)
        {
            CreatePanel(firstPanelId, new Rect(0, 0, 1, 0.5)),
            CreatePanel(secondPanelId, new Rect(0, 0.5, 0.5, 0.5)),
            CreatePanel(thirdPanelId, new Rect(0.5, 0.5, 0.5, 0.5))
        }.ToImmutableDictionary(panel => panel.Id, panel => panel.LogicalBounds);

        res = GridUtils.GetResizers(state);
        res.Should().HaveCount(3).And.SatisfyRespectively(info =>
        {
            info.IsHorizontal.Should().BeTrue();
            info.LogicalPosition.Should().Be(new Point(0.25, 0.5));
            info.ConnectedPanels.Should().HaveCount(3).And.Contain(new[] { firstPanelId, secondPanelId, thirdPanelId });
        }, info =>
        {
            info.IsHorizontal.Should().BeTrue();
            info.LogicalPosition.Should().Be(new Point(0.75, 0.5));
            info.ConnectedPanels.Should().HaveCount(3).And.Contain(new[] { firstPanelId, secondPanelId, thirdPanelId });
        }, info =>
        {
            info.IsHorizontal.Should().BeFalse();
            info.LogicalPosition.Should().Be(new Point(0.5, 0.75));
            info.ConnectedPanels.Should().HaveCount(2).And.Contain(new[] { secondPanelId, thirdPanelId });
        });


        // NOTE(erri120):
        // workspace layout is horizontal (more width than height), columns have priority over rows
        // | a | b |
        // | c | d |
        state = new List<IPanelViewModel>(capacity: columns * rows)
        {
            CreatePanel(firstPanelId, new Rect(0, 0, 0.5, 0.5)),
            CreatePanel(secondPanelId, new Rect(0.5, 0, 0.5, 0.5)),
            CreatePanel(thirdPanelId, new Rect(0, 0.5, 0.5, 0.5)),
            CreatePanel(fourthPanelId, new Rect(0.5, 0.5, 0.5, 0.5))
        }.ToImmutableDictionary(panel => panel.Id, panel => panel.LogicalBounds);

        res = GridUtils.GetResizers(state, isWorkspaceHorizontal: true);
        res.Should().HaveCount(4).And.SatisfyRespectively(info =>
        {
            info.IsHorizontal.Should().BeFalse();
            info.LogicalPosition.Should().Be(new Point(0.5, 0.25));
            info.ConnectedPanels.Should().HaveCount(4).And.Contain(new[] { firstPanelId, secondPanelId, thirdPanelId, fourthPanelId });
        }, info =>
        {
            info.IsHorizontal.Should().BeFalse();
            info.LogicalPosition.Should().Be(new Point(0.5, 0.75));
            info.ConnectedPanels.Should().HaveCount(4).And.Contain(new[] { firstPanelId, secondPanelId, thirdPanelId, fourthPanelId });
        }, info =>
        {
            info.IsHorizontal.Should().BeTrue();
            info.LogicalPosition.Should().Be(new Point(0.25, 0.5));
            info.ConnectedPanels.Should().HaveCount(2).And.Contain(new[] { firstPanelId, thirdPanelId });
        }, info =>
        {
            info.IsHorizontal.Should().BeTrue();
            info.LogicalPosition.Should().Be(new Point(0.75, 0.5));
            info.ConnectedPanels.Should().HaveCount(2).And.Contain(new[] { secondPanelId, fourthPanelId });
        });

        // NOTE(erri120):
        // workspace layout is vertical (more height than width), rows have priority over columns
        // | a | b |
        // | c | d |
        res = GridUtils.GetResizers(state, isWorkspaceHorizontal: false);
        res.Should().HaveCount(4).And.SatisfyRespectively(info =>
        {
            info.IsHorizontal.Should().BeFalse();
            info.LogicalPosition.Should().Be(new Point(0.5, 0.25));
            info.ConnectedPanels.Should().HaveCount(2).And.Contain(new[] { firstPanelId, secondPanelId });
        }, info =>
        {
            info.IsHorizontal.Should().BeTrue();
            info.LogicalPosition.Should().Be(new Point(0.25, 0.5));
            info.ConnectedPanels.Should().HaveCount(4).And.Contain(new[] { firstPanelId, secondPanelId, thirdPanelId, fourthPanelId });
        }, info =>
        {
            info.IsHorizontal.Should().BeTrue();
            info.LogicalPosition.Should().Be(new Point(0.75, 0.5));
            info.ConnectedPanels.Should().HaveCount(4).And.Contain(new[] { firstPanelId, secondPanelId, thirdPanelId, fourthPanelId });
        }, info =>
        {
            info.IsHorizontal.Should().BeFalse();
            info.LogicalPosition.Should().Be(new Point(0.5, 0.75));
            info.ConnectedPanels.Should().HaveCount(2).And.Contain(new[] { thirdPanelId, fourthPanelId });
        });


        // NOTE(erri120): weird sizes
        state = new List<IPanelViewModel>(capacity: columns * rows)
        {
            CreatePanel(firstPanelId, new Rect(0, 0, 0.35, 0.5)),
            CreatePanel(secondPanelId, new Rect(0, 0.5, 0.35, 0.5)),
            CreatePanel(thirdPanelId, new Rect(0.35, 0, 0.65, 1)),
        }.ToImmutableDictionary(panel => panel.Id, panel => panel.LogicalBounds);

        res = GridUtils.GetResizers(state, isWorkspaceHorizontal: true);
        res.Should().HaveCount(3).And.SatisfyRespectively(info =>
        {
            info.IsHorizontal.Should().BeTrue();
            info.LogicalPosition.Should().Be(new Point(0.175, 0.5));
            info.ConnectedPanels.Should().HaveCount(2).And.Contain(new[] { firstPanelId, secondPanelId });
        }, info =>
        {
            info.IsHorizontal.Should().BeFalse();
            info.LogicalPosition.Should().Be(new Point(0.35, 0.25));
            info.ConnectedPanels.Should().HaveCount(3).And.Contain(new[] { firstPanelId, secondPanelId, thirdPanelId });
        }, info =>
        {
            info.IsHorizontal.Should().BeFalse();
            info.LogicalPosition.Should().Be(new Point(0.35, 0.75));
            info.ConnectedPanels.Should().HaveCount(3).And.Contain(new[] { firstPanelId, secondPanelId, thirdPanelId });
        });
    }

    private static IPanelViewModel CreatePanel(PanelId panelId, Rect logicalBounds)
    {
        var panel = Substitute.For<IPanelViewModel>();
        panel.LogicalBounds.Returns(logicalBounds);
        panel.Id.Returns(panelId);
        return panel;
    }

    private static WorkspaceGridState CreateState(bool isHorizontal, params PanelGridState[] panels)
    {
        return WorkspaceGridState.From(panels, isHorizontal);
    }
}
