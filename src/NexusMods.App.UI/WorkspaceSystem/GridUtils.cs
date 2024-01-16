using System.Collections.Immutable;
using System.Diagnostics;
using Avalonia;

namespace NexusMods.App.UI.WorkspaceSystem;

public static class GridUtils
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

        var (columnCount, maxRowCount) = currentState.CountColumns();

        Span<WorkspaceGridState.ColumnInfo> seenColumns = stackalloc WorkspaceGridState.ColumnInfo[columnCount];
        using var columnEnumerator = new WorkspaceGridState.ColumnEnumerator(currentState, seenColumns);

        // Step 1: Iterate over all columns.
        // NOTE(erri120): this will fill up seenColumns
        Span<PanelGridState> rowBuffer = stackalloc PanelGridState[maxRows];
        while (columnEnumerator.MoveNext(rowBuffer))
        {
            var column = columnEnumerator.Current;
            var rows = column.Rows;

            // NOTE(erri120): In a horizontal layout, the rows can move independent of rows in other columns.
            if (rows.Length != maxRows)
            {
                foreach (var panelToSplit in rows)
                {
                    res.Add(SplitPanelInHalf(currentState, panelToSplit, splitVertically: false, inverse: false));
                    res.Add(SplitPanelInHalf(currentState, panelToSplit, splitVertically: false, inverse: true));
                }
            }

            // NOTE(erri120): If these conditions are met, we can split all rows of the current column in half
            // to create a new column. Note that this requires knowing the current amount of columns, so two
            // iterations are required (that's what CountColumns does).
            if (rows.Length > 1 && columnCount != maxColumns)
            {
                res.Add(AddColumn(currentState, column.Info, rows, inverse: false));
                res.Add(AddColumn(currentState, column.Info, rows, inverse: true));
            }
        }

        var seenColumnSlice = seenColumns[..columnCount];

        Span<WorkspaceGridState.RowInfo> seenRows = stackalloc WorkspaceGridState.RowInfo[maxRowCount];
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
                    res.Add(SplitPanelInHalf(currentState, panelToSplit, splitVertically: true, inverse: false));
                    res.Add(SplitPanelInHalf(currentState, panelToSplit, splitVertically: true, inverse: true));
                    continue;
                }

                foreach (var seenColumn in seenColumnSlice)
                {
                    if (seenColumn.X.IsCloseTo(rect.X) && seenColumn.Width.IsCloseTo(rect.Width)) continue;

                    if (seenColumn.X > rect.X && seenColumn.Right().IsLessThanOrCloseTo(rect.Right))
                    {
                        var updatedLogicalBounds = new Rect(rect.X, rect.Y, seenColumn.X, rect.Height);
                        var newPanelLogicalBounds = new Rect(seenColumn.X, rect.Y, seenColumn.Width, rect.Height);

                        res.Add(SplitPanelWithBounds(currentState, panelToSplit,updatedLogicalBounds,newPanelLogicalBounds, inverse: false));
                        res.Add(SplitPanelWithBounds(currentState, panelToSplit, updatedLogicalBounds, newPanelLogicalBounds, inverse: true));
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

        var (rowCount, maxColumnCount) = currentState.CountRows();

        Span<WorkspaceGridState.RowInfo> seenRows = stackalloc WorkspaceGridState.RowInfo[rowCount];
        using var rowEnumerator = new WorkspaceGridState.RowEnumerator(currentState, seenRows);

        // Step 1: Iterate over all rows.
        Span<PanelGridState> columnBuffer = stackalloc PanelGridState[maxColumns];
        while (rowEnumerator.MoveNext(columnBuffer))
        {
            var row = rowEnumerator.Current;
            var columns = row.Columns;

            // NOTE(erri120): In a vertical layout, the columns can move independent of columns in other columns.
            if (columns.Length != maxColumns)
            {
                foreach (var panelToSplit in columns)
                {
                    res.Add(SplitPanelInHalf(currentState, panelToSplit, splitVertically: true, inverse: false));
                    res.Add(SplitPanelInHalf(currentState, panelToSplit, splitVertically: true, inverse: true));
                }
            }

            // NOTE(erri120): If these conditions are met, we can split all columns of the current ro win half
            // to create a new row. Note that this requires knowing the current amount of rows, so two
            // iterations are required (that's what CountRows does).
            if (columns.Length > 1 && rowCount != maxRows)
            {
                res.Add(AddRow(currentState, row.Info, columns, inverse: false));
                res.Add(AddRow(currentState, row.Info, columns, inverse: true));
            }
        }

        var seenRowSlice = seenRows[..rowCount];

        Span<WorkspaceGridState.ColumnInfo> seenColumns = stackalloc WorkspaceGridState.ColumnInfo[maxColumnCount];
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
                    res.Add(SplitPanelInHalf(currentState, panelToSplit, splitVertically: false, inverse: false));
                    res.Add(SplitPanelInHalf(currentState, panelToSplit, splitVertically: false, inverse: true));
                }

                foreach (var seenRow in seenRowSlice)
                {
                    if (seenRow.Y.IsCloseTo(rect.Y) && seenRow.Height.IsCloseTo(rect.Height)) continue;

                    if (seenRow.Y > rect.Y && seenRow.Bottom().IsLessThanOrCloseTo(rect.Bottom))
                    {
                        var updatedLogicalBounds = new Rect(rect.X, rect.Y, rect.Width, seenRow.Y);
                        var newPanelLogicalBounds = new Rect(rect.X, seenRow.Y, rect.Width, seenRow.Height);

                        res.Add(SplitPanelWithBounds(currentState, panelToSplit,updatedLogicalBounds,newPanelLogicalBounds, inverse: false));
                        res.Add(SplitPanelWithBounds(currentState, panelToSplit, updatedLogicalBounds, newPanelLogicalBounds, inverse: true));
                    }
                }
            }
        }

        return res;
    }

    private static WorkspaceGridState AddRow(
        WorkspaceGridState currentState,
        WorkspaceGridState.RowInfo rowInfo,
        ReadOnlySpan<PanelGridState> panelsToSplit,
        bool inverse)
    {
        var newHeight = rowInfo.Height / 2;
        var currentY = inverse ? newHeight : rowInfo.Y;
        var newY = inverse ? rowInfo.Y : newHeight;

        Span<PanelGridState> updatedValues = stackalloc PanelGridState[panelsToSplit.Length + 1];
        for (var i = 0; i < panelsToSplit.Length; i++)
        {
            var panelToSplit = panelsToSplit[i];
            var rect = panelToSplit.Rect;
            panelToSplit.Rect = new Rect(rect.X, currentY, rect.Width, newHeight);

            updatedValues[i] = panelToSplit;
        }

        updatedValues[panelsToSplit.Length] = new PanelGridState(
            PanelId.DefaultValue,
            new Rect(0, newY, 1.0, newHeight)
        );

        var res = currentState.UnionById(updatedValues);
        return res;
    }

    private static WorkspaceGridState AddColumn(
        WorkspaceGridState currentState,
        WorkspaceGridState.ColumnInfo columnInfo,
        ReadOnlySpan<PanelGridState> panelsToSplit,
        bool inverse)
    {
        var newWidth = columnInfo.Width / 2;
        var currentX = inverse ? newWidth : columnInfo.X;
        var newX = inverse ? columnInfo.X : newWidth;

        Span<PanelGridState> updatedValues = stackalloc PanelGridState[panelsToSplit.Length + 1];
        for (var i = 0; i < panelsToSplit.Length; i++)
        {
            var panelToSplit = panelsToSplit[i];
            var rect = panelToSplit.Rect;
            panelToSplit.Rect = new Rect(currentX, rect.Y, newWidth, rect.Height);

            updatedValues[i] = panelToSplit;
        }

        updatedValues[panelsToSplit.Length] = new PanelGridState(
            PanelId.DefaultValue,
            new Rect(newX, 0, newWidth, 1.0)
        );

        var res = currentState.UnionById(updatedValues);
        return res;
    }

    private static WorkspaceGridState SplitPanelWithBounds(
        WorkspaceGridState currentState,
        PanelGridState panelToSplit,
        Rect updatedBounds,
        Rect newPanelBounds,
        bool inverse)
    {
        Span<PanelGridState> updatedValues = stackalloc PanelGridState[2];
        if (inverse)
        {
            updatedValues[0] = new PanelGridState(PanelId.DefaultValue, updatedBounds);
            updatedValues[1] = panelToSplit with { Rect = newPanelBounds };
        }
        else
        {
            updatedValues[0] = panelToSplit with { Rect = updatedBounds };
            updatedValues[1] = new PanelGridState(PanelId.DefaultValue, newPanelBounds);
        }

        var res = currentState.UnionById(updatedValues);
        return res;
    }

    private static WorkspaceGridState SplitPanelInHalf(
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

    internal static List<ResizerInfo2> GetResizers2(WorkspaceGridState workspaceState)
    {
        return workspaceState.IsHorizontal
            ? GetResizersForHorizontal(workspaceState)
            : GetResizersForVertical(workspaceState);
    }

    private static List<ResizerInfo2> GetResizersForVertical(WorkspaceGridState currentState)
    {
        var res = new List<ResizerInfo2>();

        var (rowCount, maxColumnCount) = currentState.CountRows();

        Span<WorkspaceGridState.RowInfo> seenRows = stackalloc WorkspaceGridState.RowInfo[rowCount];
        using var rowEnumerator = new WorkspaceGridState.RowEnumerator(currentState, seenRows);

        Span<PanelGridState> columnBuffer = stackalloc PanelGridState[maxColumnCount];
        Span<PanelGridState> columnsToAdd = stackalloc PanelGridState[maxColumnCount];

        while (rowEnumerator.MoveNext(columnBuffer))
        {
            var numColumnsToAdd = 0;

            var row = rowEnumerator.Current;
            var info = row.Info;
            var columns = row.Columns;

            for (var i = 0; i < columns.Length - 1; i++)
            {
                var (curId, curRect) = columns[i];
                var (nextId, nextRect) = columns[i + 1];

                var (columnStart, columnEnd) = MathUtils.GetResizerPoints(curRect, nextRect, WorkspaceGridState.AdjacencyKind.SameRow);
                if (!HasOther(columnStart, columnEnd, columns))
                {
                    res.Add(new ResizerInfo2(columnStart, columnEnd, IsHorizontal: false, [curId, nextId]));
                }
            }

            if (rowCount == 1) break;

            var (minX, maxX) = (1.0, 0.0);
            foreach (var column in columns)
            {
                var rect = column.Rect;

                // panels that go across rows are skipped
                if (column.IsCrossRow(info)) continue;

                minX = Math.Min(rect.Left, minX);
                maxX = Math.Max(rect.Right, maxX);

                columnsToAdd[numColumnsToAdd++] = column;
            }

            if (minX > maxX)
            {
                Debugger.Break();
                break;
            }

            ValueTuple<Point, Point> tuple;
            var isDoubleSided = false;

            if (info.Y.IsCloseTo(0))
            {
                tuple = (new Point(minX, info.Bottom()), new Point(maxX, info.Bottom()));
            } else if (info.Bottom().IsCloseTo(1))
            {
                tuple = (new Point(minX, info.Y), new Point(maxX, info.Y));
            }
            else
            {
                tuple = (new Point(minX, info.Y), new Point(maxX, info.Y));
                isDoubleSided = true;
            }

            var (start, end) = tuple;

            var slice = columnsToAdd[..numColumnsToAdd];
            var hasOther = HasOther(start, end, slice);

            if (isDoubleSided || !hasOther)
            {
                var l = new List<PanelId>(capacity: slice.Length);
                foreach (var item in slice)
                {
                    l.Add(item.Id);
                }

                if (!isDoubleSided)
                {
                    res.Add(new ResizerInfo2(start, end, IsHorizontal: true, l));
                }
                else
                {
                    var (bottomStart, bottomEnd) = (new Point(start.X, info.Bottom()), new Point(end.X, info.Bottom()));
                    res.Add(new ResizerInfo2(bottomStart, bottomEnd, IsHorizontal: true, l));
                }
            }
        }

        return res;

        bool HasOther(Point start, Point end, ReadOnlySpan<PanelGridState> columns)
        {
            var hasOther = false;
            foreach (var other in res)
            {
                if (!other.Start.IsCloseTo(start) || !other.End.IsCloseTo(end)) continue;
                hasOther = true;

                foreach (var panel in columns)
                {
                    if (!other.ConnectedPanels.Contains(panel.Id))
                        other.ConnectedPanels.Add(panel.Id);
                }
            }

            return hasOther;
        }
    }

    private static List<ResizerInfo2> GetResizersForHorizontal(WorkspaceGridState currentState)
    {
        var res = new List<ResizerInfo2>();

        var (columnCount, maxRowCount) = currentState.CountColumns();

        Span<WorkspaceGridState.ColumnInfo> seenColumns = stackalloc WorkspaceGridState.ColumnInfo[columnCount];
        using var columnEnumerator = new WorkspaceGridState.ColumnEnumerator(currentState, seenColumns);

        Span<PanelGridState> rowBuffer = stackalloc PanelGridState[maxRowCount];
        Span<PanelGridState> rowsToAdd = stackalloc PanelGridState[maxRowCount];
        while (columnEnumerator.MoveNext(rowBuffer))
        {
            var numRowsToAdd = 0;

            var column = columnEnumerator.Current;
            var info = column.Info;
            var rows = column.Rows;

            for (var i = 0; i < rows.Length - 1; i++)
            {
                var (curId, curRect) = rows[i];
                var (nextId, nextRect) = rows[i + 1];

                // if (!curRect.X.IsCloseTo(nextRect.X) || !curRect.Right.IsCloseTo(nextRect.Right)) continue;

                var (rowStart, rowEnd) = MathUtils.GetResizerPoints(curRect, nextRect, WorkspaceGridState.AdjacencyKind.SameColumn);
                if (!HasOther(rowStart, rowEnd, rows))
                {
                    res.Add(new ResizerInfo2(rowStart, rowEnd, IsHorizontal: true, [curId, nextId]));
                }
            }

            if (columnCount == 1) break;

            var (minY, maxY) = (1.0, 0.0);
            foreach (var row in rows)
            {
                var rect = row.Rect;

                // panels that go across columns are skipped
                if (row.IsCrossColumn(info)) continue;

                minY = Math.Min(rect.Top, minY);
                maxY = Math.Max(rect.Bottom, maxY);

                rowsToAdd[numRowsToAdd++] = row;
            }

            if (minY > maxY)
            {
                Debugger.Break();
                break;
            }

            ValueTuple<Point, Point> tuple;
            var isDoubleSided = false;

            if (info.X.IsCloseTo(0))
            {
                tuple = (new Point(info.Right(), minY), new Point(info.Right(), maxY));
            } else if (info.Right().IsCloseTo(1))
            {
                tuple = (new Point(info.X, minY), new Point(info.X, maxY));
            }
            else
            {
                tuple = (new Point(info.X, minY), new Point(info.X, maxY));
                isDoubleSided = true;
            }

            var (start, end) = tuple;

            var slice = rowsToAdd[..numRowsToAdd];
            var hasOther = HasOther(start, end, slice);

            if (isDoubleSided || !hasOther)
            {
                var l = new List<PanelId>(capacity: slice.Length);
                foreach (var item in slice)
                {
                    l.Add(item.Id);
                }

                if (!isDoubleSided)
                {
                    res.Add(new ResizerInfo2(start, end, IsHorizontal: false, l));
                }
                else
                {
                    var (rightStart, rightEnd) = (new Point(info.Right(), start.Y), new Point(info.Right(), end.Y));
                    res.Add(new ResizerInfo2(rightStart, rightEnd, IsHorizontal: false, l));
                }
            }
        }

        return res;

        bool HasOther(Point start, Point end, ReadOnlySpan<PanelGridState> rows)
        {
            var hasOther = false;
            foreach (var other in res)
            {
                if (!other.Start.IsCloseTo(start) || !other.End.IsCloseTo(end)) continue;
                hasOther = true;

                foreach (var panel in rows)
                {
                    if (!other.ConnectedPanels.Contains(panel.Id))
                        other.ConnectedPanels.Add(panel.Id);
                }
            }

            return hasOther;
        }
    }

    public record struct ResizerInfo2(Point Start, Point End, bool IsHorizontal, List<PanelId> ConnectedPanels);
}
