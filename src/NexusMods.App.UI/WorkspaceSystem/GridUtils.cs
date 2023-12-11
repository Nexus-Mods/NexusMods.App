using System.Collections.Immutable;
using System.Diagnostics;
using Avalonia;

namespace NexusMods.App.UI.WorkspaceSystem;

internal static class GridUtils
{
    /// <summary>
    /// Checks whether the given state is a perfect grid.
    /// </summary>
    /// <remarks>
    /// A perfect has no gaps, and no panel is out-of-bounds.
    /// </remarks>
    /// <exception cref="Exception">Thrown when the grid is not perfect.</exception>
    internal static bool IsPerfectGrid(WorkspaceGridState gridState)
    {
        var totalArea = 0.0;

        foreach (var panelState in gridState)
        {
            var (id, rect) = panelState;
            if (rect.Left < 0.0 || rect.Right > 1.0 || rect.Top < 0.0 || rect.Bottom > 1.0)
            {
                throw new Exception($"Panel {panelState} is out of bounds");
            }

            totalArea += rect.Height * rect.Width;

            foreach (var other in gridState)
            {
                if (id == other.Id) continue;

                if (rect.Intersects(other.Rect))
                {
                    throw new Exception($"{panelState.ToString()} intersects with {other.ToString()}");
                }
            }
        }

        if (!totalArea.IsCloseTo(1.0))
        {
            throw new Exception($"Area of {totalArea} doesn't match 1.0");
        }

        return true;
    }

    /// <summary>
    /// Returns all possible new states.
    /// </summary>
    internal static List<WorkspaceGridState> GetPossibleStates(
        WorkspaceGridState currentState,
        int maxColumns,
        int maxRows)
    {
        if (currentState.Count == maxColumns * maxRows) return [];

        return currentState.IsHorizontal
            ? GetPossibleStatesForHorizontal(currentState, maxColumns, maxRows)
            : GetPossibleStatesForVertical(currentState, maxColumns, maxRows);
    }

    private static List<WorkspaceGridState> GetPossibleStatesForHorizontal(
        WorkspaceGridState currentState,
        int maxColumns,
        int maxRows)
    {
        var res = new List<WorkspaceGridState>();

        Span<WorkspaceGridState.ColumnInfo> seenColumns = stackalloc WorkspaceGridState.ColumnInfo[maxColumns];
        using var columnEnumerator = new WorkspaceGridState.ColumnEnumerator(currentState, seenColumns);

        var columnCount = 0;
        var rowCount = 0;

        // Step 1: Iterate over all columns.
        // NOTE(erri120): this will fill up seenColumns
        Span<PanelGridState> rowBuffer = stackalloc PanelGridState[maxRows];
        while (columnEnumerator.MoveNext(rowBuffer))
        {
            columnCount += 1;

            var column = columnEnumerator.Current;
            var rows = column.Rows;
            rowCount = Math.Max(rowCount, rows.Length);

            // NOTE(erri120): In a horizontal layout, the rows can move independent of rows in other columns.
            if (rows.Length == maxRows) continue;
            foreach (var panelToSplit in rows)
            {
                res.Add(CreateResult(currentState, panelToSplit, splitVertically: false, inverse: false));
                res.Add(CreateResult(currentState, panelToSplit, splitVertically: false, inverse: true));
            }
        }

        var seenColumnSlice = seenColumns[..columnCount];

        Span<WorkspaceGridState.RowInfo> seenRows = stackalloc WorkspaceGridState.RowInfo[rowCount];
        using var rowEnumerator = new WorkspaceGridState.RowEnumerator(currentState, seenRows);

        // Step 2: Iterate over all rows.
        Span<PanelGridState> columnBuffer = stackalloc PanelGridState[columnCount];
        while (rowEnumerator.MoveNext(columnBuffer))
        {
            var row = rowEnumerator.Current;
            var columns = row.Columns;

            // NOTE(erri120): In a horizontal layout, the columns are linked together.
            if (columns.Length == maxColumns) continue;
            foreach (var panelToSplit in columns)
            {
                var rect = panelToSplit.Rect;

                if (columnCount == 1)
                {
                    res.Add(CreateResult(currentState, panelToSplit, splitVertically: true, inverse: false));
                    res.Add(CreateResult(currentState, panelToSplit, splitVertically: true, inverse: true));
                    continue;
                }

                foreach (var seenColumn in seenColumnSlice)
                {
                    if (seenColumn.X.IsCloseTo(rect.X) && seenColumn.Width.IsCloseTo(rect.Width)) continue;

                    if (seenColumn.X > rect.X && seenColumn.Right().IsLessThanOrCloseTo(rect.Right))
                    {
                        var updatedLogicalBounds = new Rect(rect.X, rect.Y, seenColumn.X, rect.Height);
                        var newPanelLogicalBounds = new Rect(seenColumn.X, rect.Y, seenColumn.Width, rect.Height);

                        res.Add(CreateResult(currentState, panelToSplit,updatedLogicalBounds,newPanelLogicalBounds, inverse: false));
                        res.Add(CreateResult(currentState, panelToSplit, updatedLogicalBounds, newPanelLogicalBounds, inverse: true));
                    }
                }
            }
        }

        return res;
    }

    private static List<WorkspaceGridState> GetPossibleStatesForVertical(
        WorkspaceGridState currentState,
        int maxColumns,
        int maxRows)
    {
        var res = new List<WorkspaceGridState>();

        Span<WorkspaceGridState.RowInfo> seenRows = stackalloc WorkspaceGridState.RowInfo[maxRows];
        using var rowEnumerator = new WorkspaceGridState.RowEnumerator(currentState, seenRows);

        var rowCount = 0;
        var columnCount = 0;

        // Step 1: Iterate over all rows.
        Span<PanelGridState> columnBuffer = stackalloc PanelGridState[maxColumns];
        while (rowEnumerator.MoveNext(columnBuffer))
        {
            rowCount += 1;

            var row = rowEnumerator.Current;
            var columns = row.Columns;
            columnCount = Math.Max(columnCount, columns.Length);

            // NOTE(erri120): In a vertical layout, the columns can move independent of columns in other columns.
            if (columns.Length == maxColumns) continue;
            foreach (var panelToSplit in columns)
            {
                res.Add(CreateResult(currentState, panelToSplit, splitVertically: true, inverse: false));
                res.Add(CreateResult(currentState, panelToSplit, splitVertically: true, inverse: true));
            }
        }

        var seenRowSlice = seenRows[..rowCount];

        Span<WorkspaceGridState.ColumnInfo> seenColumns = stackalloc WorkspaceGridState.ColumnInfo[columnCount];
        using var columnEnumerator = new WorkspaceGridState.ColumnEnumerator(currentState, seenColumns);

        // Step 2: Iterate over all columns.
        Span<PanelGridState> rowBuffer = stackalloc PanelGridState[rowCount];
        while (columnEnumerator.MoveNext(rowBuffer))
        {
            var column = columnEnumerator.Current;
            var rows = column.Rows;

            // NOTE(erri120): In a vertical layout, the rows are linked together.
            if (rows.Length == maxRows) continue;
            foreach (var panelToSplit in rows)
            {
                var rect = panelToSplit.Rect;

                if (rowCount == 1)
                {
                    res.Add(CreateResult(currentState, panelToSplit, splitVertically: false, inverse: false));
                    res.Add(CreateResult(currentState, panelToSplit, splitVertically: false, inverse: true));
                }

                foreach (var seenRow in seenRowSlice)
                {
                    if (seenRow.Y.IsCloseTo(rect.Y) && seenRow.Height.IsCloseTo(rect.Height)) continue;

                    if (seenRow.Y > rect.Y && seenRow.Bottom().IsLessThanOrCloseTo(rect.Bottom))
                    {
                        var updatedLogicalBounds = new Rect(rect.X, rect.Y, rect.Width, seenRow.Y);
                        var newPanelLogicalBounds = new Rect(rect.X, seenRow.Y, rect.Width, seenRow.Height);

                        res.Add(CreateResult(currentState, panelToSplit,updatedLogicalBounds,newPanelLogicalBounds, inverse: false));
                        res.Add(CreateResult(currentState, panelToSplit, updatedLogicalBounds, newPanelLogicalBounds, inverse: true));
                    }
                }
            }
        }

        return res;
    }

    private static WorkspaceGridState CreateResult(
        WorkspaceGridState currentState,
        PanelGridState panelToSplit,
        Rect updatedLogicalBounds,
        Rect newPanelLogicalBounds,
        bool inverse)
    {
        Span<PanelGridState> updatedValues = stackalloc PanelGridState[2];
        if (inverse)
        {
            updatedValues[0] = new PanelGridState(PanelId.DefaultValue, updatedLogicalBounds);
            updatedValues[1] = panelToSplit with { Rect = newPanelLogicalBounds };
        }
        else
        {
            updatedValues[0] = panelToSplit with { Rect = updatedLogicalBounds };
            updatedValues[1] = new PanelGridState(PanelId.DefaultValue, newPanelLogicalBounds);
        }

        var res = currentState.UnionById(updatedValues);
        return res;
    }

    private static WorkspaceGridState CreateResult(
        WorkspaceGridState workspaceState,
        PanelGridState panelToSplit,
        bool splitVertically,
        bool inverse)
    {
        var (updatedLogicalBounds, newPanelLogicalBounds) = MathUtils.Split(panelToSplit.Rect, splitVertically);

        Span<PanelGridState> updatedValues = stackalloc PanelGridState[2];
        if (inverse)
        {
            updatedValues[0] = new PanelGridState(PanelId.DefaultValue, updatedLogicalBounds);
            updatedValues[1] = panelToSplit with { Rect = newPanelLogicalBounds };
        }
        else
        {
            updatedValues[0] = panelToSplit with { Rect = updatedLogicalBounds };
            updatedValues[1] = new PanelGridState(PanelId.DefaultValue, newPanelLogicalBounds);
        }

        var res = workspaceState.UnionById(updatedValues);
        return res;
    }

    internal static WorkspaceGridState GetStateWithoutPanel(
        WorkspaceGridState gridState,
        PanelId panelToRemove)
    {
        if (gridState.Count == 1) return WorkspaceGridState.Empty(gridState.IsHorizontal);

        var res = gridState.Remove(gridState[panelToRemove]);
        if (res.Count == 1) return WorkspaceGridState.Empty(gridState.IsHorizontal).Add(new PanelGridState(res[0].Id, MathUtils.One));

        var panelState = gridState[panelToRemove];
        var currentRect = panelState.Rect;

        Span<PanelId> sameColumn = stackalloc PanelId[gridState.Count];
        var sameColumnCount = 0;

        Span<PanelId> sameRow = stackalloc PanelId[gridState.Count];
        var sameRowCount = 0;

        foreach (var adjacent in res.EnumerateAdjacentPanels(panelState, includeAnchor: true))
        {
            if ((adjacent.Kind & WorkspaceGridState.AdjacencyKind.SameColumn) == WorkspaceGridState.AdjacencyKind.SameColumn)
                sameColumn[sameColumnCount++] = adjacent.Panel.Id;
            if ((adjacent.Kind & WorkspaceGridState.AdjacencyKind.SameRow) == WorkspaceGridState.AdjacencyKind.SameRow)
                sameRow[sameRowCount++] = adjacent.Panel.Id;
        }

        Debug.Assert(sameColumnCount > 0 || sameRowCount > 0);

        if (gridState.IsHorizontal)
        {
            // prefer columns over rows when horizontal
            if (sameColumnCount > 0)
            {
                res = JoinSameColumn(res, currentRect, sameColumn, sameColumnCount);
            } else if (sameRowCount > 0)
            {
                res = JoinSameRow(res, currentRect, sameRow, sameRowCount);
            }
        }
        else
        {
            // prefer rows over columns when vertical
            if (sameRowCount > 0)
            {
                res = JoinSameRow(res, currentRect, sameRow, sameRowCount);
            } else if (sameColumnCount > 0)
            {
                res = JoinSameColumn(res, currentRect, sameColumn, sameColumnCount);
            }
        }

        return res;
    }

    private static WorkspaceGridState JoinSameColumn(
        WorkspaceGridState res,
        Rect currentRect,
        Span<PanelId> sameColumn,
        int sameColumnCount)
    {
        var updates = GC.AllocateUninitializedArray<PanelGridState>(sameColumnCount);

        for (var i = 0; i < sameColumnCount; i++)
        {
            var id = sameColumn[i];
            var panel = res[id];
            var rect = panel.Rect;

            var x = rect.X;
            var width = rect.Width;

            var y = Math.Min(rect.Y, currentRect.Y);
            var height = rect.Height + currentRect.Height;

            updates[i] = new PanelGridState(id, new Rect(x, y, width, height));
        }

        return res.UnionById(updates);
    }

    private static WorkspaceGridState JoinSameRow(
        WorkspaceGridState res,
        Rect currentRect,
        Span<PanelId> sameRow,
        int sameRowCount)
    {
        var updates = GC.AllocateUninitializedArray<PanelGridState>(sameRowCount);

        for (var i = 0; i < sameRowCount; i++)
        {
            var id = sameRow[i];
            var panel = res[id];
            var rect = panel.Rect;

            var y = rect.Y;
            var height = rect.Height;

            var x = Math.Min(rect.X, currentRect.X);
            var width = rect.Width + currentRect.Width;

            updates[i] = new PanelGridState(id, new Rect(x, y, width, height));
        }

        return res.UnionById(updates);
    }

    internal static IReadOnlyList<ResizerInfo> GetResizers(
        ImmutableDictionary<PanelId, Rect> currentState,
        bool isWorkspaceHorizontal = true)
    {
        var tmp = new List<ResizerInfo>(capacity: currentState.Count);
        var adjacentPanels = new List<ResizerInfo>(capacity: currentState.Count);

        // Step 1: fill the tmp List with all Resizers
        foreach (var current in currentState)
        {
            var currentRect = current.Value;

            // Step 1.1: go through all panels and find their adjacent panels
            foreach (var other in currentState)
            {
                if (other.Key == current.Key) continue;

                var rect = other.Value;

                // same column
                // | a | x |  | b | x |
                // | b | x |  | a | x |
                if (rect.Left >= currentRect.Left && rect.Right <= currentRect.Right)
                {
                    if (rect.Top.IsCloseTo(currentRect.Bottom) || rect.Bottom.IsCloseTo(currentRect.Top))
                    {
                        Add(adjacentPanels, current, other, isHorizontal: true);
                    }
                }

                // same row
                // | a | b |  | b | a |  | a | b |
                // | x | x |  | x | x |  | a | c |
                if (rect.Top >= currentRect.Top && rect.Bottom <= currentRect.Bottom)
                {
                    if (rect.Left.IsCloseTo(currentRect.Right) || rect.Right.IsCloseTo(currentRect.Left))
                    {
                        Add(adjacentPanels, current, other, isHorizontal: false);
                    }
                }
            }

            // Step 1.2: combine the resizers for the current panel
            foreach (var info in adjacentPanels)
            {
                var pos = info.LogicalPosition;

                var acc = new List<PanelId>();
                acc.AddRange(info.ConnectedPanels);

                foreach (var other in adjacentPanels)
                {
                    if (other.IsHorizontal != info.IsHorizontal) continue;

                    var otherPos = other.LogicalPosition;
                    if (pos == otherPos) continue;
                    if (!pos.X.IsCloseTo(otherPos.X) && !pos.Y.IsCloseTo(otherPos.Y)) continue;

                    acc.AddRange(other.ConnectedPanels.Where(id => !acc.Contains(id)));
                }

                tmp.Add(info with { ConnectedPanels = acc.ToArray() });
            }

            adjacentPanels.Clear();
        }

        // Step 2: combine all resizers of all panels
        var res = tmp
            .DistinctBy(info => info.LogicalPosition)
            .GroupBy(info => isWorkspaceHorizontal ? info.LogicalPosition.X : info.LogicalPosition.Y)
            .SelectMany(group =>
            {
                var connectedPanels = group.SelectMany(info => info.ConnectedPanels).Distinct().ToArray();
                return group.Select(info => info with { ConnectedPanels = connectedPanels });
            }).ToArray();

        return res;

        static void Add(ICollection<ResizerInfo> list, KeyValuePair<PanelId, Rect> current, KeyValuePair<PanelId, Rect> other, bool isHorizontal)
        {
            var (currentId, currentRect) = current;
            var (otherId, rect) = other;

            var pos = MathUtils.GetMidPoint(currentRect, rect, isHorizontal);
            list.Add(new ResizerInfo(IsHorizontal: isHorizontal, pos, new[] { currentId, otherId }));
        }
    }

    internal record struct ResizerInfo(bool IsHorizontal, Point LogicalPosition, PanelId[] ConnectedPanels);
}
