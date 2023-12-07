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
    [MemberData(nameof(TestData_GetPossibleStates_Generated))]
    public void Test_GetPossibleStates(
        WorkspaceGridState currentState,
        WorkspaceGridState[] expectedOutputs)
    {
        GridUtils.IsPerfectGrid(currentState).Should().BeTrue();
        if (expectedOutputs.Length != 0)
        {
            expectedOutputs.Should().AllSatisfy(expectedOutput =>
            {
                expectedOutput.IsHorizontal.Should().Be(currentState.IsHorizontal);
                GridUtils.IsPerfectGrid(expectedOutput).Should().BeTrue();
            });
        }

        var actualOutputs = GridUtils.GetPossibleStates(
            currentState,
            columns: 2,
            rows: 2
        ).ToArray();

        if (actualOutputs.Length != 0)
        {
            actualOutputs.Should().AllSatisfy(output =>
            {
                GridUtils.IsPerfectGrid(output).Should().BeTrue();
            });
        }

        actualOutputs.Should().HaveCount(expectedOutputs.Length);
        for (var i = 0; i < actualOutputs.Length; i++)
        {
            actualOutputs[i].Should().Equal(expectedOutputs[i]);
        }
    }

    public static IEnumerable<object[]> TestData_GetPossibleStates_Generated()
    {
        var newPanelId = PanelId.DefaultValue;
        var firstPanelId = PanelId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var secondPanelId = PanelId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        var thirdPanelId = PanelId.From(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        var fourthPanelId = PanelId.From(Guid.Parse("44444444-4444-4444-4444-444444444444"));

        // TODO: test with variable sized panels

        // Input: one panel
        // Possible States:
        // 1) split vertically, new panel is in the second column
        // 2) split vertically, new panel is in the first column
        // 3) split horizontally, new panel is in the second row
        // 4) split horizontally, new panel is in the first row
        yield return new object[]
        {
            CreateState(
                isHorizontal: true,
                new PanelGridState(firstPanelId, MathUtils.One)
            ),
            new[]
            {
                CreateState(
                    isHorizontal: true,
                    new PanelGridState(firstPanelId, new Rect(0, 0, 0.5, 1)),
                    new PanelGridState(newPanelId, new Rect(0.5, 0, 0.5, 1))
                ),
                CreateState(
                    isHorizontal: true,
                    new PanelGridState(newPanelId, new Rect(0, 0, 0.5, 1)),
                    new PanelGridState(firstPanelId, new Rect(0.5, 0, 0.5, 1))
                ),
                CreateState(
                    isHorizontal: true,
                    new PanelGridState(firstPanelId, new Rect(0, 0, 1, 0.5)),
                    new PanelGridState(newPanelId, new Rect(0, 0.5, 1, 0.5))
                ),
                CreateState(
                    isHorizontal: true,
                    new PanelGridState(newPanelId, new Rect(0, 0, 1, 0.5)),
                    new PanelGridState(firstPanelId, new Rect(0, 0.5, 1, 0.5))
                ),
            }
        };

        // Input: two columns
        // | 1 | 2 |
        // | 1 | 2 |
        // Possible States:
        // 1) split the first panel horizontally, the new panel is in the second row
        // 2) split the first panel horizontally, the new panel is in the first row
        // 3) split the second panel horizontally, the new panel is in the second row
        // 4) split the second panel horizontally, the new panel is in the first row
        // TODO: 5) split both the first and second panel horizontally, the new panel will take up the entirety of the second row
        // TODO: 6) split both the first and second panel horizontally, the new panel will take up the entirety of the first row
        yield return new object[]
        {
            CreateState(
                isHorizontal: true,
                new PanelGridState(firstPanelId, new Rect(0, 0, 0.5, 1)),
                new PanelGridState(secondPanelId, new Rect(0.5, 0, 0.5, 1))
            ),
            new[]
            {
                CreateState(
                    isHorizontal: true,
                    new PanelGridState(firstPanelId, new Rect(0, 0, 0.5, 0.5)),
                    new PanelGridState(newPanelId, new Rect(0, 0.5, 0.5, 0.5)),
                    new PanelGridState(secondPanelId, new Rect(0.5, 0, 0.5, 1))
                ),
                CreateState(
                    isHorizontal: true,
                    new PanelGridState(newPanelId, new Rect(0, 0, 0.5, 0.5)),
                    new PanelGridState(firstPanelId, new Rect(0, 0.5, 0.5, 0.5)),
                    new PanelGridState(secondPanelId, new Rect(0.5, 0, 0.5, 1))
                ),
                CreateState(
                    isHorizontal: true,
                    new PanelGridState(firstPanelId, new Rect(0, 0, 0.5, 1)),
                    new PanelGridState(secondPanelId, new Rect(0.5, 0, 0.5, 0.5)),
                    new PanelGridState(newPanelId, new Rect(0.5, 0.5, 0.5, 0.5))
                ),
                CreateState(
                    isHorizontal: true,
                    new PanelGridState(firstPanelId, new Rect(0, 0, 0.5, 1)),
                    new PanelGridState(newPanelId, new Rect(0.5, 0, 0.5, 0.5)),
                    new PanelGridState(secondPanelId, new Rect(0.5, 0.5, 0.5, 0.5))
                ),
            }
        };

        // Input: two rows
        // | 1 | 1 |
        // | 2 | 2 |
        // Possible States:
        // 1) split the first panel vertically, the new panel is in the second column
        // 2) split the first panel vertically, the new panel is in the first column
        // 3) split the second panel vertically, the new panel is in the second column
        // 4) split the second panel vertically, the new panel is in the first column
        // TODO: 5) split both the first and second panel vertically, the new panel will take up the entirety of the second column
        // TODO: 6) split both the first and second panel vertically, the new panel will take up the entirety of the first column
        yield return new object[]
        {
            CreateState(
                isHorizontal: true,
                new PanelGridState(firstPanelId, new Rect(0, 0, 1, 0.5)),
                new PanelGridState(secondPanelId, new Rect(0, 0.5, 1, 0.5))
            ),
            new[]
            {
                CreateState(
                    isHorizontal: true,
                    new PanelGridState(firstPanelId, new Rect(0, 0, 0.5, 0.5)),
                    new PanelGridState(secondPanelId, new Rect(0, 0.5, 1, 0.5)),
                    new PanelGridState(newPanelId, new Rect(0.5, 0, 0.5, 0.5))
                ),
                CreateState(
                    isHorizontal: true,
                    new PanelGridState(newPanelId, new Rect(0, 0, 0.5, 0.5)),
                    new PanelGridState(secondPanelId, new Rect(0, 0.5, 1, 0.5)),
                    new PanelGridState(firstPanelId, new Rect(0.5, 0, 0.5, 0.5))
                ),
                CreateState(
                    isHorizontal: true,
                    new PanelGridState(firstPanelId, new Rect(0, 0, 1, 0.5)),
                    new PanelGridState(secondPanelId, new Rect(0, 0.5, 0.5, 0.5)),
                    new PanelGridState(newPanelId, new Rect(0.5, 0.5, 0.5, 0.5))
                ),
                CreateState(
                    isHorizontal: true,
                    new PanelGridState(firstPanelId, new Rect(0, 0, 1, 0.5)),
                    new PanelGridState(newPanelId, new Rect(0, 0.5, 0.5, 0.5)),
                    new PanelGridState(secondPanelId, new Rect(0.5, 0.5, 0.5, 0.5))
                ),
            }
        };

        // Input: three panels with one large row
        // | 1 | 1 |
        // | 2 | 3 |
        // Possible States:
        // 1) split the first panel vertically, the new panel is in the second column
        // 2) split the first panel vertically, the new panel is in the first column
        yield return new object[]
        {
            CreateState(
                isHorizontal: true,
                new PanelGridState(firstPanelId, new Rect(0, 0, 1, 0.5)),
                new PanelGridState(secondPanelId, new Rect(0, 0.5, 0.5, 0.5)),
                new PanelGridState(thirdPanelId, new Rect(0.5, 0.5, 0.5, 0.5))
            ),
            new[]
            {
                CreateState(
                    isHorizontal: true,
                    new PanelGridState(firstPanelId, new Rect(0, 0, 0.5, 0.5)),
                    new PanelGridState(newPanelId, new Rect(0.5, 0, 0.5, 0.5)),
                    new PanelGridState(secondPanelId, new Rect(0, 0.5, 0.5, 0.5)),
                    new PanelGridState(thirdPanelId, new Rect(0.5, 0.5, 0.5, 0.5))
                ),
                CreateState(
                    isHorizontal: true,
                    new PanelGridState(newPanelId, new Rect(0, 0, 0.5, 0.5)),
                    new PanelGridState(firstPanelId, new Rect(0.5, 0, 0.5, 0.5)),
                    new PanelGridState(secondPanelId, new Rect(0, 0.5, 0.5, 0.5)),
                    new PanelGridState(thirdPanelId, new Rect(0.5, 0.5, 0.5, 0.5))
                )
            }
        };

        // Input: three panels with one large column
        // | 1 | 2 |
        // | 1 | 3 |
        // Possible States:
        // 1) split the first panel horizontally, the new panel is in the second row
        // 2) split the first panel horizontally, the new panel is in the first row
        yield return new object[]
        {
            CreateState(
                isHorizontal: true,
                new PanelGridState(firstPanelId, new Rect(0, 0, 0.5, 1)),
                new PanelGridState(secondPanelId, new Rect(0.5, 0, 0.5, 0.5)),
                new PanelGridState(thirdPanelId, new Rect(0.5, 0.5, 0.5, 0.5))
            ),
            new[]
            {
                CreateState(
                    isHorizontal: true,
                    new PanelGridState(firstPanelId, new Rect(0, 0, 0.5, 0.5)),
                    new PanelGridState(newPanelId, new Rect(0, 0.5, 0.5, 0.5)),
                    new PanelGridState(secondPanelId, new Rect(0.5, 0, 0.5, 0.5)),
                    new PanelGridState(thirdPanelId, new Rect(0.5, 0.5, 0.5, 0.5))
                ),
                CreateState(
                    isHorizontal: true,
                    new PanelGridState(newPanelId, new Rect(0, 0, 0.5, 0.5)),
                    new PanelGridState(firstPanelId, new Rect(0, 0.5, 0.5, 0.5)),
                    new PanelGridState(secondPanelId, new Rect(0.5, 0, 0.5, 0.5)),
                    new PanelGridState(thirdPanelId, new Rect(0.5, 0.5, 0.5, 0.5))
                ),
            }
        };

        // Input: four panels
        // Possible States: none
        yield return new object[]
        {
            CreateState(
                isHorizontal: true,
                new PanelGridState(firstPanelId, new Rect(0, 0, 0.5, 0.5)),
                new PanelGridState(secondPanelId, new Rect(0, 0.5, 0.5, 0.5)),
                new PanelGridState(thirdPanelId, new Rect(0.5, 0, 0.5, 0.5)),
                new PanelGridState(fourthPanelId, new Rect(0.5, 0.5, 0.5, 0.5))
            ),
            Array.Empty<WorkspaceGridState>()
        };
    }
}
