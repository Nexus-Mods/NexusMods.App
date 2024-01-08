using System.Diagnostics.CodeAnalysis;
using Avalonia;
using FluentAssertions;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.UI.Tests.WorkspaceSystem;

public partial class WorkspaceGridStateTests
{
    [Theory]
    [MemberData(nameof(TestData_RowEnumerator))]
    public void Test_RowEnumerator(
        WorkspaceGridState currentState,
        (WorkspaceGridState.RowInfo Info, PanelGridState[] Columns)[] expectedOutputs)
    {
        GridUtils.IsPerfectGrid(currentState).Should().BeTrue();

        Span<WorkspaceGridState.RowInfo> seenRows = stackalloc WorkspaceGridState.RowInfo[8];
        using var rowEnumerator = new WorkspaceGridState.RowEnumerator(currentState, seenRows);

        var actualOutputs = new List<(WorkspaceGridState.RowInfo Info, PanelGridState[] Columns)>();

        Span<PanelGridState> columnBuffer = stackalloc PanelGridState[2];
        while (rowEnumerator.MoveNext(columnBuffer))
        {
            var current = rowEnumerator.Current;
            actualOutputs.Add((current.Info, current.Columns.ToArray()));
        }

        actualOutputs.Should().HaveCount(expectedOutputs.Length);
        for (var i = 0; i < actualOutputs.Count; i++)
        {
            var actualOutput = actualOutputs[i];
            var expectedOutput = expectedOutputs[i];

            actualOutput.Info.Should().Be(expectedOutput.Info);
            actualOutput.Columns.Should().Equal(expectedOutput.Columns);
        }

        currentState.CountRows().rowCount.Should().Be(expectedOutputs.Length);
    }

    [SuppressMessage("ReSharper", "HeapView.ObjectAllocation.Evident")]
    [SuppressMessage("ReSharper", "HeapView.ObjectAllocation")]
    [SuppressMessage("ReSharper", "HeapView.BoxingAllocation")]
    public static IEnumerable<object[]> TestData_RowEnumerator()
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
                new PanelGridState(secondPanelId, new Rect(0, 0.5, 0.5, 0.5)),
                new PanelGridState(thirdPanelId, new Rect(0.5, 0, 0.5, 1))
            ),
            new[]
            {
                (
                    Info: new WorkspaceGridState.RowInfo(0, 0.5),
                    Columns: new[]
                    {
                        new PanelGridState(firstPanelId, new Rect(0, 0, 0.5, 0.5)),
                        new PanelGridState(thirdPanelId, new Rect(0.5, 0, 0.5, 1))
                    }
                ),
                (
                    Info: new WorkspaceGridState.RowInfo(0.5, 0.5),
                    Columns: new[]
                    {
                        new PanelGridState(secondPanelId, new Rect(0, 0.5, 0.5, 0.5)),
                        new PanelGridState(thirdPanelId, new Rect(0.5, 0, 0.5, 1))
                    }
                ),
            }
        };
    }
}
