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
        WorkspaceGridState currentState,
        PanelId panelToRemove,
        WorkspaceGridState expectedOutput,
        string because)
    {
        currentState.IsHorizontal.Should().Be(expectedOutput.IsHorizontal, because: "this shouldn't change");
        GridUtils.IsPerfectGrid(currentState).Should().BeTrue();
        GridUtils.IsPerfectGrid(expectedOutput).Should().BeTrue();

        var actualOutput = GridUtils.GetStateWithoutPanel(currentState, panelToRemove);
        actualOutput.Inner.Should().Equal(expectedOutput.Inner, because: because);
        actualOutput.IsHorizontal.Should().Be(expectedOutput.IsHorizontal);

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
                    isHorizontal: true,
                    new PanelGridState(firstPanelId, new Rect(0, 0, width, 1)),
                    new PanelGridState(secondPanelId, new Rect(width, 0, 1.0 - width, 1))
                ),
                secondPanelId,
                CreateState(isHorizontal: true, new PanelGridState(firstPanelId, new Rect(0, 0, 1, 1))),
                "the second panel should be removed and the first panel should take up the entire space of the workspace"
            };

            yield return new object[]
            {
                CreateState(
                    isHorizontal: true,
                    new PanelGridState(firstPanelId, new Rect(0, 0, width, 1)),
                    new PanelGridState(secondPanelId, new Rect(width, 0, 1.0 - width, 1))
                ),
                firstPanelId,
                CreateState(isHorizontal: true, new PanelGridState(secondPanelId, new Rect(0, 0, 1, 1))),
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
                    isHorizontal: true,
                    new PanelGridState(firstPanelId, new Rect(0, 0, 1, height)),
                    new PanelGridState(secondPanelId, new Rect(0, height, 1, 1.0 - height))
                ),
                secondPanelId,
                CreateState(isHorizontal: true, new PanelGridState(firstPanelId, new Rect(0, 0, 1, 1))),
                "the second panel should be removed and the first panel should take up the entire space of the workspace"
            };

            yield return new object[]
            {
                CreateState(
                    isHorizontal: true,
                    new PanelGridState(firstPanelId, new Rect(0, 0, 1, height)),
                    new PanelGridState(secondPanelId, new Rect(0, height, 1, 1.0 - height))
                ),
                firstPanelId,
                CreateState(isHorizontal: true, new PanelGridState(secondPanelId, new Rect(0, 0, 1, 1))),
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
                        isHorizontal: true,
                        new PanelGridState(firstPanelId, new Rect(0, 0, width, 1)),
                        new PanelGridState(secondPanelId, new Rect(width, 0, 1.0 - width, height)),
                        new PanelGridState(thirdPanelId, new Rect(width, height, 1.0 - width, 1.0 - height))
                    ),
                    thirdPanelId,
                    CreateState(
                        isHorizontal: true,
                        new PanelGridState(firstPanelId, new Rect(0, 0, width, 1)),
                        new PanelGridState(secondPanelId, new Rect(width, 0, 1.0 - width, 1))
                    ),
                    "the third panel should be removed and the second panel should take up the space"
                };

                yield return new object[]
                {
                    CreateState(
                        isHorizontal: true,
                        new PanelGridState(firstPanelId, new Rect(0, 0, width, 1)),
                        new PanelGridState(secondPanelId, new Rect(width, 0, 1.0 - width, height)),
                        new PanelGridState(thirdPanelId, new Rect(width, height, 1.0 - width, 1.0 - height))
                    ),
                    secondPanelId,
                    CreateState(
                        isHorizontal: true,
                        new PanelGridState(firstPanelId, new Rect(0, 0, width, 1)),
                        new PanelGridState(thirdPanelId, new Rect(width, 0, 1.0 - width, 1))
                    ),
                    "the second panel should be removed and the third panel should take up the space"
                };

                yield return new object[]
                {
                    CreateState(
                        isHorizontal: true,
                        new PanelGridState(firstPanelId, new Rect(0, 0, width, 1)),
                        new PanelGridState(secondPanelId, new Rect(width, 0, 1.0 - width, height)),
                        new PanelGridState(thirdPanelId, new Rect(width, height, 1.0 - width, 1.0 - height))
                    ),
                    firstPanelId,
                    CreateState(
                        isHorizontal: true,
                        new PanelGridState(secondPanelId, new Rect(0, 0, 1, height)),
                        new PanelGridState(thirdPanelId, new Rect(0, height, 1, 1.0 - height))
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
                        isHorizontal: true,
                        new PanelGridState(firstPanelId, new Rect(0, 0, 1, height)),
                        new PanelGridState(secondPanelId, new Rect(0, height, width, 1 - height)),
                        new PanelGridState(thirdPanelId, new Rect(width, height, 1 - width, 1 - height))
                    ),
                    thirdPanelId,
                    CreateState(
                        isHorizontal: true,
                        new PanelGridState(firstPanelId, new Rect(0, 0, 1, height)),
                        new PanelGridState(secondPanelId, new Rect(0, height, 1, 1 - height))
                    ),
                    "the third panel should be removed and the second panel should take up the space"
                };

                yield return new object[]
                {
                    CreateState(
                        isHorizontal: true,
                        new PanelGridState(firstPanelId, new Rect(0, 0, 1, height)),
                        new PanelGridState(secondPanelId, new Rect(0, height, width, 1 - height)),
                        new PanelGridState(thirdPanelId, new Rect(width, height, 1 - width, 1 - height))
                    ),
                    secondPanelId,
                    CreateState(
                        isHorizontal: true,
                        new PanelGridState(firstPanelId, new Rect(0, 0, 1, height)),
                        new PanelGridState(thirdPanelId, new Rect(0, height, 1, 1 - height))
                    ),
                    "the second panel should be removed and the third panel should take up the space"
                };

                yield return new object[]
                {
                    CreateState(
                        isHorizontal: true,
                        new PanelGridState(firstPanelId, new Rect(0, 0, 1, height)),
                        new PanelGridState(secondPanelId, new Rect(0, height, width, 1 - height)),
                        new PanelGridState(thirdPanelId, new Rect(width, height, 1 - width, 1 - height))
                    ),
                    firstPanelId,
                    CreateState(
                        isHorizontal: true,
                        new PanelGridState(secondPanelId, new Rect(0, 0, width, 1)),
                        new PanelGridState(thirdPanelId, new Rect(width, 0, 1 - width, 1))
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
                    isHorizontal: true,
                    new PanelGridState(firstPanelId, new Rect(0, 0, width, 0.5)),
                    new PanelGridState(secondPanelId, new Rect(width, 0, 1 - width, 0.5)),
                    new PanelGridState(thirdPanelId, new Rect(0, 0.5, width, 0.5)),
                    new PanelGridState(fourthPanelId, new Rect(width, 0.5, 1 - width, 0.5))
                ),
                firstPanelId,
                CreateState(
                    isHorizontal: true,
                    new PanelGridState(secondPanelId, new Rect(width, 0, 1 - width, 0.5)),
                    new PanelGridState(thirdPanelId, new Rect(0, 0, width, 1)),
                    new PanelGridState(fourthPanelId, new Rect(width, 0.5, 1 - width, 0.5))
                ),
                "the first panel should be removed and the third panel should take up the space due to the horizontal layout"
            };

            yield return new object[]
            {
                CreateState(
                    isHorizontal: true,
                    new PanelGridState(firstPanelId, new Rect(0, 0, width, 0.5)),
                    new PanelGridState(secondPanelId, new Rect(width, 0, 1 - width, 0.5)),
                    new PanelGridState(thirdPanelId, new Rect(0, 0.5, width, 0.5)),
                    new PanelGridState(fourthPanelId, new Rect(width, 0.5, 1 - width, 0.5))
                ),
                thirdPanelId,
                CreateState(
                    isHorizontal: true,
                    new PanelGridState(firstPanelId, new Rect(0, 0, width, 1)),
                    new PanelGridState(secondPanelId, new Rect(width, 0, 1 - width, 0.5)),
                    new PanelGridState(fourthPanelId, new Rect(width, 0.5, 1 - width, 0.5))
                ),
                "the third panel should be removed and the first panel should take up the space due to the horizontal layout"
            };

            yield return new object[]
            {
                CreateState(
                    isHorizontal: true,
                    new PanelGridState(firstPanelId, new Rect(0, 0, width, 0.5)),
                    new PanelGridState(secondPanelId, new Rect(width, 0, 1 - width, 0.5)),
                    new PanelGridState(thirdPanelId, new Rect(0, 0.5, width, 0.5)),
                    new PanelGridState(fourthPanelId, new Rect(width, 0.5, 1 - width, 0.5))
                ),
                secondPanelId,
                CreateState(
                    isHorizontal: true,
                    new PanelGridState(firstPanelId, new Rect(0, 0, width, 0.5)),
                    new PanelGridState(thirdPanelId, new Rect(0, 0.5, width, 0.5)),
                    new PanelGridState(fourthPanelId, new Rect(width, 0, 1 - width, 1))
                ),
                "the second panel should be removed and the fourth panel should take up the space due to the horizontal layout"
            };

            yield return new object[]
            {
                CreateState(
                    isHorizontal: true,
                    new PanelGridState(firstPanelId, new Rect(0, 0, width, 0.5)),
                    new PanelGridState(secondPanelId, new Rect(width, 0, 1 - width, 0.5)),
                    new PanelGridState(thirdPanelId, new Rect(0, 0.5, width, 0.5)),
                    new PanelGridState(fourthPanelId, new Rect(width, 0.5, 1 - width, 0.5))
                ),
                fourthPanelId,
                CreateState(
                    isHorizontal: true,
                    new PanelGridState(firstPanelId, new Rect(0, 0, width, 0.5)),
                    new PanelGridState(secondPanelId, new Rect(width, 0, 1 - width, 1)),
                    new PanelGridState(thirdPanelId, new Rect(0, 0.5, width, 0.5))
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
                    isHorizontal: false,
                    new PanelGridState(firstPanelId, new Rect(0, 0, 0.5, height)),
                    new PanelGridState(secondPanelId, new Rect(0.5, 0, 0.5, height)),
                    new PanelGridState(thirdPanelId, new Rect(0, height, 0.5, 1 - height)),
                    new PanelGridState(fourthPanelId, new Rect(0.5, height, 0.5, 1 - height))
                ),
                firstPanelId,
                CreateState(
                    isHorizontal: false,
                    new PanelGridState(secondPanelId, new Rect(0, 0, 1, height)),
                    new PanelGridState(thirdPanelId, new Rect(0, height, 0.5, 1 - height)),
                    new PanelGridState(fourthPanelId, new Rect(0.5, height, 0.5, 1 - height))
                ),
                "the first panel should be removed and the second panel should take up the space due to the vertical layout"
            };

            yield return new object[]
            {
                CreateState(
                    isHorizontal: false,
                    new PanelGridState(firstPanelId, new Rect(0, 0, 0.5, height)),
                    new PanelGridState(secondPanelId, new Rect(0.5, 0, 0.5, height)),
                    new PanelGridState(thirdPanelId, new Rect(0, height, 0.5, 1 - height)),
                    new PanelGridState(fourthPanelId, new Rect(0.5, height, 0.5, 1 - height))
                ),
                secondPanelId,
                CreateState(
                    isHorizontal: false,
                    new PanelGridState(firstPanelId, new Rect(0, 0, 1, height)),
                    new PanelGridState(thirdPanelId, new Rect(0, height, 0.5, 1 - height)),
                    new PanelGridState(fourthPanelId, new Rect(0.5, height, 0.5, 1 - height))
                ),
                "the second panel should be removed and the first panel should take up the space due to the vertical layout"
            };

            yield return new object[]
            {
                CreateState(
                    isHorizontal: false,
                    new PanelGridState(firstPanelId, new Rect(0, 0, 0.5, height)),
                    new PanelGridState(secondPanelId, new Rect(0.5, 0, 0.5, height)),
                    new PanelGridState(thirdPanelId, new Rect(0, height, 0.5, 1 - height)),
                    new PanelGridState(fourthPanelId, new Rect(0.5, height, 0.5, 1 - height))
                ),
                thirdPanelId,
                CreateState(
                    isHorizontal: false,
                    new PanelGridState(firstPanelId, new Rect(0, 0, 0.5, height)),
                    new PanelGridState(secondPanelId, new Rect(0.5, 0, 0.5, height)),
                    new PanelGridState(fourthPanelId, new Rect(0, height, 1, 1 - height))
                ),
                "the third panel should be removed and the fourth panel should take up the space due to the vertical layout"
            };

            yield return new object[]
            {
                CreateState(
                    isHorizontal: false,
                    new PanelGridState(firstPanelId, new Rect(0, 0, 0.5, height)),
                    new PanelGridState(secondPanelId, new Rect(0.5, 0, 0.5, height)),
                    new PanelGridState(thirdPanelId, new Rect(0, height, 0.5, 1 - height)),
                    new PanelGridState(fourthPanelId, new Rect(0.5, height, 0.5, 1 - height))
                ),
                fourthPanelId,
                CreateState(
                    isHorizontal: false,
                    new PanelGridState(firstPanelId, new Rect(0, 0, 0.5, height)),
                    new PanelGridState(secondPanelId, new Rect(0.5, 0, 0.5, height)),
                    new PanelGridState(thirdPanelId, new Rect(0, height, 1, 1 - height))
                ),
                "the fourth panel should be removed and the third panel should take up the space due to the vertical layout"
            };
        }
    }
}
