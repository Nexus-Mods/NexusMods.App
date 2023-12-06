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
    public static void Test_GetPossibleStates()
    {
        var firstPanelId = PanelId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var secondPanelId = PanelId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        var thirdPanelId = PanelId.From(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        var fourthPanelId = PanelId.From(Guid.Parse("44444444-4444-4444-4444-444444444444"));

        const int columns = 2;
        const int rows = 2;
        var panels = new List<IPanelViewModel>(capacity: columns * rows);

        var res = GridUtils.GetPossibleStates(ToInput(), columns, rows).ToArray();
        res.Should().BeEmpty();

        // NOTE(erri120): Start with a single "main" panel that takes up the entire space.
        var firstPanel = CreatePanel(firstPanelId, new Rect(0, 0, 1, 1));
        panels.Add(firstPanel);

        // NOTE(erri120): Testing with a single panel in the grid. There are four possible
        // new grid states in this scenario since the single panel can be split vertically
        // or horizontally and the new panel can be placed below/right or above/left of the
        // main panel (1 = existing, 2 = new):
        // | 1 | 2 |  | 2 | 1 |  | 1 | 1 |  | 2 | 2 |
        // | 1 | 2 |  | 2 | 1 |  | 2 | 2 |  | 1 | 1 |

        res = GridUtils.GetPossibleStates(ToInput(), columns, rows).ToArray();
        res.Should().HaveCount(4).And.SatisfyRespectively(dict =>
        {
            dict.Should().HaveCount(2).And.Equal(new KeyValuePair<PanelId, Rect>[]
            {
                new(firstPanel.Id, new Rect(0, 0, 0.5, 1)),
                new(PanelId.DefaultValue, new Rect(0.5, 0, 0.5, 1)),
            });
        }, dict => {
            dict.Should().HaveCount(2).And.Equal(new KeyValuePair<PanelId, Rect>[]
            {
                new(firstPanel.Id, new Rect(0.5, 0, 0.5, 1)),
                new(PanelId.DefaultValue, new Rect(0, 0, 0.5, 1)),
            });
        }, dict =>
        {
            dict.Should().HaveCount(2).And.Equal(new KeyValuePair<PanelId, Rect>[]
            {
                new(firstPanel.Id, new Rect(0, 0, 1, 0.5)),
                new(PanelId.DefaultValue, new Rect(0, 0.5, 1, 0.5)),
            });
        }, dict => {
            dict.Should().HaveCount(2).And.Equal(new KeyValuePair<PanelId, Rect>[]
            {
                new(firstPanel.Id, new Rect(0, 0.5, 1, 0.5)),
                new(PanelId.DefaultValue, new Rect(0, 0, 1, 0.5)),
            });
        });

        // NOTE(erri120): Add a vertical panel that takes up half the space:
        // | 1 | 2 |
        // | 1 | 2 |
        firstPanel.LogicalBounds = new Rect(0, 0, 0.5, 1);
        var secondPanel = CreatePanel(secondPanelId, new Rect(0.5, 0, 0.5, 1));
        panels.Add(secondPanel);

        // NOTE(erri120): A third panel can now only appear in the corners, which is why
        // we have 4 possible states (1 = first panel, 2 = second panel, 3 = new panel):
        // | 1 | 2 |  | 3 | 2 |  | 1 | 2 |  | 1 | 3 |
        // | 3 | 2 |  | 1 | 2 |  | 1 | 3 |  | 1 | 2 |
        res = GridUtils.GetPossibleStates(ToInput(), columns, rows).ToArray();
        res.Should().HaveCount(4).And.SatisfyRespectively(dict =>
        {
            dict.Should().HaveCount(3).And.Equal(new KeyValuePair<PanelId, Rect>[]
            {
                new(firstPanel.Id, new Rect(0, 0, 0.5, 0.5)),
                new(PanelId.DefaultValue, new Rect(0, 0.5, 0.5, 0.5)),
                new(secondPanel.Id, new Rect(0.5, 0, 0.5, 1)),
            });
        }, dict =>
        {
            dict.Should().HaveCount(3).And.Equal(new KeyValuePair<PanelId, Rect>[]
            {
                new(PanelId.DefaultValue, new Rect(0, 0, 0.5, 0.5)),
                new(firstPanel.Id, new Rect(0, 0.5, 0.5, 0.5)),
                new(secondPanel.Id, new Rect(0.5, 0, 0.5, 1)),
            });
        }, dict =>
        {
            dict.Should().HaveCount(3).And.Equal(new KeyValuePair<PanelId, Rect>[]
            {
                new(firstPanel.Id, new Rect(0, 0, 0.5, 1)),
                new(secondPanel.Id, new Rect(0.5, 0, 0.5, 0.5)),
                new(PanelId.DefaultValue, new Rect(0.5, 0.5, 0.5, 0.5)),
            });
        }, dict =>
        {
            dict.Should().HaveCount(3).And.Equal(new KeyValuePair<PanelId, Rect>[]
            {
                new(firstPanel.Id, new Rect(0, 0, 0.5, 1)),
                new(secondPanel.Id, new Rect(0.5, 0.5, 0.5, 0.5)),
                new(PanelId.DefaultValue, new Rect(0.5, 0, 0.5, 0.5)),
            });
        });

        // NOTE(erri120): add the third panel in the bottom left corner
        // | 1 | 2 |
        // | 3 | 2 |
        firstPanel.LogicalBounds = new Rect(0, 0, 0.5, 0.5);
        var thirdPanel = CreatePanel(thirdPanelId, new Rect(0, 0.5, 0.5, 0.5));
        panels.Add(thirdPanel);

        // NOTE(erri120): The final fourth panel can now only appear in one of the corners
        // in the right column (1 = first panel, 2 = second panel, 3 = third panel, 4 = new panel):
        // | 1 | 2 |  | 1 | 4 |
        // | 3 | 4 |  | 3 | 2 |
        res = GridUtils.GetPossibleStates(ToInput(), columns, rows).ToArray();
        res.Should().HaveCount(2).And.SatisfyRespectively(dict =>
        {
            dict.Should().HaveCount(4).And.Equal(new KeyValuePair<PanelId, Rect>[]
            {
                new(firstPanel.Id, new Rect(0, 0, 0.5, 0.5)),
                new(secondPanel.Id, new Rect(0.5, 0, 0.5, 0.5)),
                new(thirdPanel.Id, new Rect(0, 0.5, 0.5, 0.5)),
                new(PanelId.DefaultValue, new Rect(0.5, 0.5, 0.5, 0.5)),
            });
        }, dict =>
        {
            dict.Should().HaveCount(4).And.Equal(new KeyValuePair<PanelId, Rect>[]
            {
                new(firstPanel.Id, new Rect(0, 0, 0.5, 0.5)),
                new(secondPanel.Id, new Rect(0.5, 0.5, 0.5, 0.5)),
                new(thirdPanel.Id, new Rect(0, 0.5, 0.5, 0.5)),
                new(PanelId.DefaultValue, new Rect(0.5, 0, 0.5, 0.5)),
            });
        });

        // NOTE(erri120): add the fourth panel in the bottom right corner
        // | 1 | 2 |
        // | 3 | 4 |
        secondPanel.LogicalBounds = new Rect(0, 0, 0.5, 0.5);
        var fourthPanel = CreatePanel(fourthPanelId, new Rect(0.5, 0.5, 0.5, 0.5));
        panels.Add(fourthPanel);

        // NOTE(erri120): final state reached, no more panels possible.
        res = GridUtils.GetPossibleStates(ToInput(), columns, rows).ToArray();
        res.Should().BeEmpty();
        return;

        ImmutableDictionary<PanelId, Rect> ToInput()
        {
            return panels.ToImmutableDictionary(x => x.Id, x => x.LogicalBounds);
        }
    }

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

    private static WorkspaceGridState CreateState(params KeyValuePair<PanelId, Rect>[] panels)
    {
        return new WorkspaceGridState(panels.ToImmutableDictionary());
    }
}
