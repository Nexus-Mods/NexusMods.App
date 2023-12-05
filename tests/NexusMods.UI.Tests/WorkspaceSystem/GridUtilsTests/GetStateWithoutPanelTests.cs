using System.Collections.Immutable;
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
    [MemberData(nameof(TestData_GetStateWithoutPanel_Generated))]
    public void Test_GetStateWithoutPanel(
        ImmutableDictionary<PanelId, Rect> currentState,
        PanelId panelToRemove,
        bool isHorizontal,
        ImmutableDictionary<PanelId, Rect> expectedOutput,
        string because)
    {
        GridUtils.IsPerfectGrid(currentState).Should().BeTrue();
        GridUtils.IsPerfectGrid(expectedOutput).Should().BeTrue();

        var actualOutput = GridUtils.GetStateWithoutPanel(currentState, panelToRemove, isHorizontal);
        actualOutput.Should().Equal(expectedOutput, because: because);

        GridUtils.IsPerfectGrid(actualOutput).Should().BeTrue();
    }

    public static IEnumerable<object[]> TestData_GetStateWithoutPanel_Generated()
    {
        const double step = 0.1;
        const double min = 0.2;
        const double max = 1.0 - min;

        var firstPanelId = PanelId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var secondPanelId = PanelId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        var thirdPanelId = PanelId.From(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        var fourthPanelId = PanelId.From(Guid.Parse("44444444-4444-4444-4444-444444444444"));

        // two panels: two columns
        // | 1 | 2 |
        // | 1 | 2 |
        for (var width = min; width < max; width += step)
        {
            yield return new object[]
            {
                CreateState(
                    new KeyValuePair<PanelId, Rect>(firstPanelId, new Rect(0, 0, width, 1)),
                    new KeyValuePair<PanelId, Rect>(secondPanelId, new Rect(width, 0, 1.0 - width, 1))
                ),
                secondPanelId,
                true,
                CreateState(new KeyValuePair<PanelId, Rect>(firstPanelId, new Rect(0, 0, 1, 1))),
                "the second panel should be removed and the first panel should take up the entire space of the workspace"
            };

            yield return new object[]
            {
                CreateState(
                    new KeyValuePair<PanelId, Rect>(firstPanelId, new Rect(0, 0, width, 1)),
                    new KeyValuePair<PanelId, Rect>(secondPanelId, new Rect(width, 0, 1.0 - width, 1))
                ),
                firstPanelId,
                true,
                CreateState(new KeyValuePair<PanelId, Rect>(secondPanelId, new Rect(0, 0, 1, 1))),
                "the first panel should be removed and the second panel should take up the entire space of the workspace"
            };
        }

        // two panels: two rows
        // | 1 | 1 |
        // | 2 | 2 |
        for (var height = min; height < max; height += step)
        {
            yield return new object[]
            {
                CreateState(
                    new KeyValuePair<PanelId, Rect>(firstPanelId, new Rect(0, 0, 1, height)),
                    new KeyValuePair<PanelId, Rect>(secondPanelId, new Rect(0, height, 1, 1.0 - height))
                ),
                secondPanelId,
                true,
                CreateState(new KeyValuePair<PanelId, Rect>(firstPanelId, new Rect(0, 0, 1, 1))),
                "the second panel should be removed and the first panel should take up the entire space of the workspace"
            };

            yield return new object[]
            {
                CreateState(
                    new KeyValuePair<PanelId, Rect>(firstPanelId, new Rect(0, 0, 1, height)),
                    new KeyValuePair<PanelId, Rect>(secondPanelId, new Rect(0, height, 1, 1.0 - height))
                ),
                firstPanelId,
                true,
                CreateState(new KeyValuePair<PanelId, Rect>(secondPanelId, new Rect(0, 0, 1, 1))),
                "the first panel should be removed and the second panel should take up the entire space of the workspace"
            };
        }

        // three panels: one large column
        // | 1 | 2 |
        // | 1 | 3 |
        for (var width = min; width < max; width += step)
        {
            for (var height = min; height < max; height += step)
            {
                yield return new object[]
                {
                    CreateState(
                        new KeyValuePair<PanelId, Rect>(firstPanelId, new Rect(0, 0, width, 1)),
                        new KeyValuePair<PanelId, Rect>(secondPanelId, new Rect(width, 0, 1.0 - width, height)),
                        new KeyValuePair<PanelId, Rect>(thirdPanelId, new Rect(width, height, 1.0 - width, 1.0 - height))
                    ),
                    thirdPanelId,
                    true,
                    CreateState(
                        new KeyValuePair<PanelId, Rect>(firstPanelId, new Rect(0, 0, width, 1)),
                        new KeyValuePair<PanelId, Rect>(secondPanelId, new Rect(width, 0, 1.0 - width, 1))
                    ),
                    "the third panel should be removed and the second panel should take up the space"
                };

                yield return new object[]
                {
                    CreateState(
                        new KeyValuePair<PanelId, Rect>(firstPanelId, new Rect(0, 0, width, 1)),
                        new KeyValuePair<PanelId, Rect>(secondPanelId, new Rect(width, 0, 1.0 - width, height)),
                        new KeyValuePair<PanelId, Rect>(thirdPanelId, new Rect(width, height, 1.0 - width, 1.0 - height))
                    ),
                    secondPanelId,
                    true,
                    CreateState(
                        new KeyValuePair<PanelId, Rect>(firstPanelId, new Rect(0, 0, width, 1)),
                        new KeyValuePair<PanelId, Rect>(thirdPanelId, new Rect(width, 0, 1.0 - width, 1))
                    ),
                    "the second panel should be removed and the third panel should take up the space"
                };

                yield return new object[]
                {
                    CreateState(
                        new KeyValuePair<PanelId, Rect>(firstPanelId, new Rect(0, 0, width, 1)),
                        new KeyValuePair<PanelId, Rect>(secondPanelId, new Rect(width, 0, 1.0 - width, height)),
                        new KeyValuePair<PanelId, Rect>(thirdPanelId, new Rect(width, height, 1.0 - width, 1.0 - height))
                    ),
                    firstPanelId,
                    true,
                    CreateState(
                        new KeyValuePair<PanelId, Rect>(secondPanelId, new Rect(0, 0, 1, height)),
                        new KeyValuePair<PanelId, Rect>(thirdPanelId, new Rect(0, height, 1, 1.0 - height))
                    ),
                    "the first panel should be removed and the second and third panels should take up the space"
                };
            }
        }

        // three panels: one large row
        // | 1 | 1 |
        // | 2 | 3 |
        for (var height = min; height < max; height += step)
        {
            for (var width = min; width < max; width += step)
            {
                yield return new object[]
                {
                    CreateState(
                        new KeyValuePair<PanelId, Rect>(firstPanelId, new Rect(0, 0, 1, height)),
                        new KeyValuePair<PanelId, Rect>(secondPanelId, new Rect(0, height, width, 1 - height)),
                        new KeyValuePair<PanelId, Rect>(thirdPanelId, new Rect(width, height, 1 - width, 1 - height))
                    ),
                    thirdPanelId,
                    true,
                    CreateState(
                        new KeyValuePair<PanelId, Rect>(firstPanelId, new Rect(0, 0, 1, height)),
                        new KeyValuePair<PanelId, Rect>(secondPanelId, new Rect(0, height, 1, 1 - height))
                    ),
                    "the third panel should be removed and the second panel should take up the space"
                };

                yield return new object[]
                {
                    CreateState(
                        new KeyValuePair<PanelId, Rect>(firstPanelId, new Rect(0, 0, 1, height)),
                        new KeyValuePair<PanelId, Rect>(secondPanelId, new Rect(0, height, width, 1 - height)),
                        new KeyValuePair<PanelId, Rect>(thirdPanelId, new Rect(width, height, 1 - width, 1 - height))
                    ),
                    secondPanelId,
                    true,
                    CreateState(
                        new KeyValuePair<PanelId, Rect>(firstPanelId, new Rect(0, 0, 1, height)),
                        new KeyValuePair<PanelId, Rect>(thirdPanelId, new Rect(0, height, 1, 1 - height))
                    ),
                    "the second panel should be removed and the third panel should take up the space"
                };

                yield return new object[]
                {
                    CreateState(
                        new KeyValuePair<PanelId, Rect>(firstPanelId, new Rect(0, 0, 1, height)),
                        new KeyValuePair<PanelId, Rect>(secondPanelId, new Rect(0, height, width, 1 - height)),
                        new KeyValuePair<PanelId, Rect>(thirdPanelId, new Rect(width, height, 1 - width, 1 - height))
                    ),
                    firstPanelId,
                    true,
                    CreateState(
                        new KeyValuePair<PanelId, Rect>(secondPanelId, new Rect(0, 0, width, 1)),
                        new KeyValuePair<PanelId, Rect>(thirdPanelId, new Rect(width, 0, 1 - width, 1))
                    ),
                    "the first panel should be removed and the second and third panels should take up the space"
                };
            }
        }

        // four panels: horizontal (prefer columns over rows)
        // | 1 | 2 |
        // | 3 | 4 |
        for (var width = min; width < max; width += step)
        {
            yield return new object[]
            {
                CreateState(
                    new KeyValuePair<PanelId, Rect>(firstPanelId, new Rect(0, 0, width, 0.5)),
                    new KeyValuePair<PanelId, Rect>(secondPanelId, new Rect(width, 0, 1 - width, 0.5)),
                    new KeyValuePair<PanelId, Rect>(thirdPanelId, new Rect(0, 0.5, width, 0.5)),
                    new KeyValuePair<PanelId, Rect>(fourthPanelId, new Rect(width, 0.5, 1 - width, 0.5))
                ),
                firstPanelId,
                true,
                CreateState(
                    new KeyValuePair<PanelId, Rect>(secondPanelId, new Rect(width, 0, 1 - width, 0.5)),
                    new KeyValuePair<PanelId, Rect>(thirdPanelId, new Rect(0, 0, width, 1)),
                    new KeyValuePair<PanelId, Rect>(fourthPanelId, new Rect(width, 0.5, 1 - width, 0.5))
                ),
                "the first panel should be removed and the third panel should take up the space due to the horizontal layout"
            };

            yield return new object[]
            {
                CreateState(
                    new KeyValuePair<PanelId, Rect>(firstPanelId, new Rect(0, 0, width, 0.5)),
                    new KeyValuePair<PanelId, Rect>(secondPanelId, new Rect(width, 0, 1 - width, 0.5)),
                    new KeyValuePair<PanelId, Rect>(thirdPanelId, new Rect(0, 0.5, width, 0.5)),
                    new KeyValuePair<PanelId, Rect>(fourthPanelId, new Rect(width, 0.5, 1 - width, 0.5))
                ),
                thirdPanelId,
                true,
                CreateState(
                    new KeyValuePair<PanelId, Rect>(firstPanelId, new Rect(0, 0, width, 1)),
                    new KeyValuePair<PanelId, Rect>(secondPanelId, new Rect(width, 0, 1 - width, 0.5)),
                    new KeyValuePair<PanelId, Rect>(fourthPanelId, new Rect(width, 0.5, 1 - width, 0.5))
                ),
                "the third panel should be removed and the first panel should take up the space due to the horizontal layout"
            };

            yield return new object[]
            {
                CreateState(
                    new KeyValuePair<PanelId, Rect>(firstPanelId, new Rect(0, 0, width, 0.5)),
                    new KeyValuePair<PanelId, Rect>(secondPanelId, new Rect(width, 0, 1 - width, 0.5)),
                    new KeyValuePair<PanelId, Rect>(thirdPanelId, new Rect(0, 0.5, width, 0.5)),
                    new KeyValuePair<PanelId, Rect>(fourthPanelId, new Rect(width, 0.5, 1 - width, 0.5))
                ),
                secondPanelId,
                true,
                CreateState(
                    new KeyValuePair<PanelId, Rect>(firstPanelId, new Rect(0, 0, width, 0.5)),
                    new KeyValuePair<PanelId, Rect>(thirdPanelId, new Rect(0, 0.5, width, 0.5)),
                    new KeyValuePair<PanelId, Rect>(fourthPanelId, new Rect(width, 0, 1 - width, 1))
                ),
                "the second panel should be removed and the fourth panel should take up the space due to the horizontal layout"
            };

            yield return new object[]
            {
                CreateState(
                    new KeyValuePair<PanelId, Rect>(firstPanelId, new Rect(0, 0, width, 0.5)),
                    new KeyValuePair<PanelId, Rect>(secondPanelId, new Rect(width, 0, 1 - width, 0.5)),
                    new KeyValuePair<PanelId, Rect>(thirdPanelId, new Rect(0, 0.5, width, 0.5)),
                    new KeyValuePair<PanelId, Rect>(fourthPanelId, new Rect(width, 0.5, 1 - width, 0.5))
                ),
                fourthPanelId,
                true,
                CreateState(
                    new KeyValuePair<PanelId, Rect>(firstPanelId, new Rect(0, 0, width, 0.5)),
                    new KeyValuePair<PanelId, Rect>(secondPanelId, new Rect(width, 0, 1 - width, 1)),
                    new KeyValuePair<PanelId, Rect>(thirdPanelId, new Rect(0, 0.5, width, 0.5))
                ),
                "the fourth panel should be removed and the second panel should take up the space due to the horizontal layout"
            };
        }

        // four panels: vertical (prefer rows over columns)
        // | 1 | 2 |
        // | 3 | 4 |
        for (var height = min; height < max; height += step)
        {
            yield return new object[]
            {
                CreateState(
                    new KeyValuePair<PanelId, Rect>(firstPanelId, new Rect(0, 0, 0.5, height)),
                    new KeyValuePair<PanelId, Rect>(secondPanelId, new Rect(0.5, 0, 0.5, height)),
                    new KeyValuePair<PanelId, Rect>(thirdPanelId, new Rect(0, height, 0.5, 1 - height)),
                    new KeyValuePair<PanelId, Rect>(fourthPanelId, new Rect(0.5, height, 0.5, 1 - height))
                ),
                firstPanelId,
                false,
                CreateState(
                    new KeyValuePair<PanelId, Rect>(secondPanelId, new Rect(0, 0, 1, height)),
                    new KeyValuePair<PanelId, Rect>(thirdPanelId, new Rect(0, height, 0.5, 1 - height)),
                    new KeyValuePair<PanelId, Rect>(fourthPanelId, new Rect(0.5, height, 0.5, 1 - height))
                ),
                "the first panel should be removed and the second panel should take up the space due to the vertical layout"
            };

            yield return new object[]
            {
                CreateState(
                    new KeyValuePair<PanelId, Rect>(firstPanelId, new Rect(0, 0, 0.5, height)),
                    new KeyValuePair<PanelId, Rect>(secondPanelId, new Rect(0.5, 0, 0.5, height)),
                    new KeyValuePair<PanelId, Rect>(thirdPanelId, new Rect(0, height, 0.5, 1 - height)),
                    new KeyValuePair<PanelId, Rect>(fourthPanelId, new Rect(0.5, height, 0.5, 1 - height))
                ),
                secondPanelId,
                false,
                CreateState(
                    new KeyValuePair<PanelId, Rect>(firstPanelId, new Rect(0, 0, 1, height)),
                    new KeyValuePair<PanelId, Rect>(thirdPanelId, new Rect(0, height, 0.5, 1 - height)),
                    new KeyValuePair<PanelId, Rect>(fourthPanelId, new Rect(0.5, height, 0.5, 1 - height))
                ),
                "the second panel should be removed and the first panel should take up the space due to the vertical layout"
            };

            yield return new object[]
            {
                CreateState(
                    new KeyValuePair<PanelId, Rect>(firstPanelId, new Rect(0, 0, 0.5, height)),
                    new KeyValuePair<PanelId, Rect>(secondPanelId, new Rect(0.5, 0, 0.5, height)),
                    new KeyValuePair<PanelId, Rect>(thirdPanelId, new Rect(0, height, 0.5, 1 - height)),
                    new KeyValuePair<PanelId, Rect>(fourthPanelId, new Rect(0.5, height, 0.5, 1 - height))
                ),
                thirdPanelId,
                false,
                CreateState(
                    new KeyValuePair<PanelId, Rect>(firstPanelId, new Rect(0, 0, 0.5, height)),
                    new KeyValuePair<PanelId, Rect>(secondPanelId, new Rect(0.5, 0, 0.5, height)),
                    new KeyValuePair<PanelId, Rect>(fourthPanelId, new Rect(0, height, 1, 1 - height))
                ),
                "the third panel should be removed and the fourth panel should take up the space due to the vertical layout"
            };

            yield return new object[]
            {
                CreateState(
                    new KeyValuePair<PanelId, Rect>(firstPanelId, new Rect(0, 0, 0.5, height)),
                    new KeyValuePair<PanelId, Rect>(secondPanelId, new Rect(0.5, 0, 0.5, height)),
                    new KeyValuePair<PanelId, Rect>(thirdPanelId, new Rect(0, height, 0.5, 1 - height)),
                    new KeyValuePair<PanelId, Rect>(fourthPanelId, new Rect(0.5, height, 0.5, 1 - height))
                ),
                fourthPanelId,
                false,
                CreateState(
                    new KeyValuePair<PanelId, Rect>(firstPanelId, new Rect(0, 0, 0.5, height)),
                    new KeyValuePair<PanelId, Rect>(secondPanelId, new Rect(0.5, 0, 0.5, height)),
                    new KeyValuePair<PanelId, Rect>(thirdPanelId, new Rect(0, height, 1, 1 - height))
                ),
                "the fourth panel should be removed and the third panel should take up the space due to the vertical layout"
            };
        }
    }
}
