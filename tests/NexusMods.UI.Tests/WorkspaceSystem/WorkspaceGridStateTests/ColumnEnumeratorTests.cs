using System.Diagnostics.CodeAnalysis;
using Avalonia;
using FluentAssertions;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.UI.Tests.WorkspaceSystem;

public partial class WorkspaceGridStateTests
{
    [Theory]
    [MemberData(nameof(TestData_ColumnEnumerator))]
    public void Test_ColumnEnumerator(
        WorkspaceGridState currentState,
        (WorkspaceGridState.ColumnInfo Info, PanelGridState[] Rows)[] expectedOutputs)
    {
        GridUtils.IsPerfectGrid(currentState).Should().BeTrue();

        Span<WorkspaceGridState.ColumnInfo> seenColumns = stackalloc WorkspaceGridState.ColumnInfo[8];
        using var columnEnumerator = new WorkspaceGridState.ColumnEnumerator(currentState, seenColumns);

        var actualOutputs = new List<(WorkspaceGridState.ColumnInfo Info, PanelGridState[] Rows)>();

        Span<PanelGridState> rowBuffer = stackalloc PanelGridState[2];
        while (columnEnumerator.MoveNext(rowBuffer))
        {
            var current = columnEnumerator.Current;
            actualOutputs.Add((current.Info, current.Rows.ToArray()));
        }

        actualOutputs.Should().HaveCount(expectedOutputs.Length);
        for (var i = 0; i < actualOutputs.Count; i++)
        {
            var actualOutput = actualOutputs[i];
            var expectedOutput = expectedOutputs[i];

            actualOutput.Info.Should().Be(expectedOutput.Info);
            actualOutput.Rows.Should().Equal(expectedOutput.Rows);
        }

        currentState.CountColumns().columnCount.Should().Be(expectedOutputs.Length);
    }

    [SuppressMessage("ReSharper", "HeapView.ObjectAllocation.Evident")]
    [SuppressMessage("ReSharper", "HeapView.ObjectAllocation")]
    [SuppressMessage("ReSharper", "HeapView.BoxingAllocation")]
    public static IEnumerable<object[]> TestData_ColumnEnumerator()
    {
        var firstPanelId = PanelId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var secondPanelId = PanelId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        var thirdPanelId = PanelId.From(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        var fourthPanelId = PanelId.From(Guid.Parse("44444444-4444-4444-4444-444444444444"));

        yield return new object[]
        {
            CreateState(
                isHorizontal: true,
                new PanelGridState(firstPanelId, new Rect(0, 0, 0.5, 0.5)),
                new PanelGridState(secondPanelId, new Rect(0.5, 0, 0.5, 0.5)),
                new PanelGridState(thirdPanelId, new Rect(0, 0.5, 1, 0.5))
            ),
            new[]
            {
                (
                    Info: new WorkspaceGridState.ColumnInfo(0, 0.5),
                    Rows: new[]
                    {
                        new PanelGridState(firstPanelId, new Rect(0, 0, 0.5, 0.5)),
                        new PanelGridState(thirdPanelId, new Rect(0, 0.5, 1, 0.5))
                    }
                ),
                (
                    Info: new WorkspaceGridState.ColumnInfo(0.5, 0.5),
                    Rows: new[]
                    {
                        new PanelGridState(secondPanelId, new Rect(0.5, 0, 0.5, 0.5)),
                        new PanelGridState(thirdPanelId, new Rect(0, 0.5, 1, 0.5))
                    }
                ),
            }
        };
    }
}
